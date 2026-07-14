using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OXAR.DataTransferObjects.Opu;
using OXAR.Repository.UnitOfWork;
using TaskService.Services.Helper;

namespace OXAR.Services.Opu
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Egg Collection (OPU) service. Full CRUD.
    /// Target Dataverse table: bcrm_opu (see 01-DATA-MODEL.md §3.4).
    /// </summary>
    public class OpuService : IOpuService
    {
        #region Properties
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Constructor
        public OpuService() { _unitOfWork = new UnitOfWork(); }
        public OpuService(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        #endregion

        #region Reads

        public object GetByCycleId(string cycleId)
        {
            try
            {
                logger.Info("Type: Info, Location: OpuService, Method: GetByCycleId, Action: Starting..");
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_opu'>
                        <attribute name='bcrm_opuid' />
                        <attribute name='bcrm_name' />
                        <attribute name='bcrm_proceduredate' />
                        <attribute name='bcrm_doctor' />
                        <attribute name='bcrm_embryologist' />
                        <attribute name='bcrm_witness' />
                        <attribute name='bcrm_folliclesaspirated' />
                        <attribute name='bcrm_eggsretrieved' />
                        <attribute name='bcrm_mii' />
                        <attribute name='bcrm_mi' />
                        <attribute name='bcrm_gv' />
                        <attribute name='modifiedon' />
                        <order attribute='bcrm_proceduredate' descending='true' />
                        <filter type='and'>
                          <condition attribute='bcrm_treatment_cycle' operator='eq' value='{0}' />
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>", cycleId);

                dynamic result = new List<ExpandoObject>();
                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                if (rows != null && rows.Entities != null)
                {
                    foreach (var x in rows.Entities)
                    {
                        dynamic a = new ExpandoObject();
                        a.Id = x.Id.ToString();
                        a.Name = Str(x, "bcrm_name");
                        a.ProcedureDate = Date(x, "bcrm_proceduredate");
                        a.Doctor = RefName(x, "bcrm_doctor");
                        a.Embryologist = RefName(x, "bcrm_embryologist");
                        a.Witness = RefName(x, "bcrm_witness");
                        a.FolliclesAspirated = Int(x, "bcrm_folliclesaspirated");
                        a.EggsRetrieved = Int(x, "bcrm_eggsretrieved");
                        a.Mii = Int(x, "bcrm_mii");
                        a.Mi = Int(x, "bcrm_mi");
                        a.Gv = Int(x, "bcrm_gv");
                        a.ModifiedOn = Date(x, "modifiedon");
                        ((List<ExpandoObject>)result).Add(a);
                    }
                }
                logger.Info("Type: Info, Location: OpuService, Method: GetByCycleId, Action: Completed.");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: OpuService, Method: GetByCycleId, Error: " + ex.Message);
                throw;
            }
        }

        public OpuDTO GetById(string id)
        {
            try
            {
                logger.Info("Type: Info, Location: OpuService, Method: GetById, Action: Starting..");
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_opu'>
                        <all-attributes />
                        <filter type='and'>
                          <condition attribute='bcrm_opuid' operator='eq' value='{0}' />
                        </filter>
                      </entity>
                    </fetch>", id);

                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                OpuDTO dto = null;
                if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
                {
                    var x = rows.Entities.First();
                    dto = new OpuDTO
                    {
                        Id = x.Id.ToString(),
                        Name = Str(x, "bcrm_name"),
                        TreatmentCycleId = RefId(x, "bcrm_treatment_cycle"),
                        TreatmentCycleName = RefName(x, "bcrm_treatment_cycle"),
                        ProcedureDate = Date(x, "bcrm_proceduredate"),
                        DoctorId = RefId(x, "bcrm_doctor"),
                        DoctorName = RefName(x, "bcrm_doctor"),
                        EmbryologistId = RefId(x, "bcrm_embryologist"),
                        EmbryologistName = RefName(x, "bcrm_embryologist"),
                        WitnessId = RefId(x, "bcrm_witness"),
                        WitnessName = RefName(x, "bcrm_witness"),
                        FolliclesAspirated = Int(x, "bcrm_folliclesaspirated"),
                        EggsRetrieved = Int(x, "bcrm_eggsretrieved"),
                        Mii = Int(x, "bcrm_mii"),
                        Mi = Int(x, "bcrm_mi"),
                        Gv = Int(x, "bcrm_gv"),
                        Complications = Str(x, "bcrm_complications"),
                        Notes = Str(x, "bcrm_notes"),
                        ModifiedBy = RefName(x, "modifiedby"),
                        ModifiedOn = Date(x, "modifiedon"),
                        CreatedOn = Date(x, "createdon")
                    };
                }
                logger.Info("Type: Info, Location: OpuService, Method: GetById, Action: Completed.");
                return dto;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: OpuService, Method: GetById, Error: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Writes

        public Guid Create(OpuDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: OpuService, Method: Create, Action: Starting..");
                Guard(dto);
                ValidateWitness(dto);
                ValidateCounts(dto);

                var e = new Entity("bcrm_opu");
                Apply(e, dto);
                var id = _unitOfWork.CRMCoreRepository.Create(e);
                logger.Info("Type: Info, Location: OpuService, Method: Create, Action: Completed.");
                return id;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: OpuService, Method: Create, Error: " + ex.Message);
                throw;
            }
        }

        public bool Update(OpuDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: OpuService, Method: Update, Action: Starting..");
                if (string.IsNullOrWhiteSpace(dto.Id)) throw new Exception("Id is required for update.");
                Guard(dto);
                ValidateWitness(dto);
                ValidateCounts(dto);
                CheckConcurrency(dto.Id, dto.ModifiedOn);

                var e = new Entity("bcrm_opu") { Id = Guid.Parse(dto.Id) };
                Apply(e, dto);
                _unitOfWork.CRMCoreRepository.Update(e);
                logger.Info("Type: Info, Location: OpuService, Method: Update, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: OpuService, Method: Update, Error: " + ex.Message);
                throw;
            }
        }

        public bool Cancel(Guid id, string reason)
        {
            try
            {
                logger.Info("Type: Info, Location: OpuService, Method: Cancel, Action: Starting..");
                if (string.IsNullOrWhiteSpace(reason)) throw new Exception("A cancel reason is required.");
                var e = new Entity("bcrm_opu") { Id = id };
                e["bcrm_cancelreason"] = reason;
                e["statecode"] = new OptionSetValue(1);
                e["statuscode"] = new OptionSetValue(2);
                _unitOfWork.CRMCoreRepository.Update(e);
                logger.Info("Type: Info, Location: OpuService, Method: Cancel, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: OpuService, Method: Cancel, Error: " + ex.Message);
                throw;
            }
        }

        public bool Delete(Guid id)
        {
            try
            {
                logger.Info("Type: Info, Location: OpuService, Method: Delete, Action: Starting..");
                _unitOfWork.CRMCoreRepository.Delete(new Entity("bcrm_opu", id));
                logger.Info("Type: Info, Location: OpuService, Method: Delete, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: OpuService, Method: Delete, Error: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Helpers

        private static void Apply(Entity e, OpuDTO dto)
        {
            SetRef(e, "bcrm_treatment_cycle", "bcrm_treatment_cycle", dto.TreatmentCycleId);
            SetRef(e, "bcrm_doctor", "systemuser", dto.DoctorId);
            SetRef(e, "bcrm_embryologist", "systemuser", dto.EmbryologistId);
            SetRef(e, "bcrm_witness", "systemuser", dto.WitnessId);
            SetDate(e, "bcrm_proceduredate", dto.ProcedureDate);
            if (dto.FolliclesAspirated.HasValue) e["bcrm_folliclesaspirated"] = dto.FolliclesAspirated.Value;
            if (dto.EggsRetrieved.HasValue) e["bcrm_eggsretrieved"] = dto.EggsRetrieved.Value;
            if (dto.Mii.HasValue) e["bcrm_mii"] = dto.Mii.Value;
            if (dto.Mi.HasValue) e["bcrm_mi"] = dto.Mi.Value;
            if (dto.Gv.HasValue) e["bcrm_gv"] = dto.Gv.Value;
            if (!string.IsNullOrWhiteSpace(dto.Complications)) e["bcrm_complications"] = dto.Complications;
            if (!string.IsNullOrWhiteSpace(dto.Notes)) e["bcrm_notes"] = dto.Notes;
        }

        private static void ValidateWitness(OpuDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.WitnessId) &&
                !string.IsNullOrWhiteSpace(dto.EmbryologistId) &&
                string.Equals(dto.WitnessId, dto.EmbryologistId, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Witness must be a different person from the performing embryologist.");
        }

        /// <summary>Maturity split cannot exceed the eggs retrieved.</summary>
        private static void ValidateCounts(OpuDTO dto)
        {
            var split = (dto.Mii ?? 0) + (dto.Mi ?? 0) + (dto.Gv ?? 0);
            if (dto.EggsRetrieved.HasValue && split > dto.EggsRetrieved.Value)
                throw new Exception("MII + MI + GV cannot exceed the number of eggs retrieved.");
        }

        private void CheckConcurrency(string id, DateTime? clientModifiedOn)
        {
            if (!clientModifiedOn.HasValue) return;
            var fetch = string.Format(@"
                <fetch top='1' mapping='logical'>
                  <entity name='bcrm_opu'>
                    <attribute name='modifiedon' />
                    <filter type='and'><condition attribute='bcrm_opuid' operator='eq' value='{0}' /></filter>
                  </entity>
                </fetch>", id);
            var rows = _unitOfWork.CRMCoreRepository.Get(fetch);
            if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
            {
                var current = Date(rows.Entities.First(), "modifiedon");
                if (current.HasValue && Math.Abs((current.Value - clientModifiedOn.Value).TotalSeconds) > 1)
                    throw new Exception("CONFLICT: this record was changed by someone else. Reload and re-apply your edit.");
            }
        }

        private static void Guard(object dto)
        {
            var obj = JObject.Parse(JsonConvert.SerializeObject(dto));
            foreach (var prop in obj.Properties())
                if (new ACSHelper().ContainsSpecialCharacters(prop.Value.ToString()))
                    throw new Exception("Invalid json");
        }

        private static string Str(Entity x, string f) => x.Attributes.Contains(f) && x[f] != null ? x[f].ToString() : string.Empty;
        private static int? Int(Entity x, string f) => x.Attributes.Contains(f) && x[f] is int i ? i : (int?)null;
        private static DateTime? Date(Entity x, string f) => x.Attributes.Contains(f) && x[f] is DateTime dt ? dt : (DateTime?)null;
        private static string RefId(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Id.ToString() : string.Empty;
        private static string RefName(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Name : string.Empty;
        private static void SetDate(Entity e, string f, DateTime? v) { if (v.HasValue && v.Value != DateTime.MinValue) e[f] = DateTime.SpecifyKind(v.Value, DateTimeKind.Utc); }
        private static void SetRef(Entity e, string f, string logicalName, string id) { if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var g)) e[f] = new EntityReference(logicalName, g); }

        #endregion
    }
}
