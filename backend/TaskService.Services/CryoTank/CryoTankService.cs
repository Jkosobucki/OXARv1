using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OXAR.DataTransferObjects.CryoTank;
using OXAR.Repository.UnitOfWork;
using TaskService.Services.Helper;

namespace OXAR.Services.CryoTank
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Cryo Tank reference-data service. Full CRUD so a lab manager
    /// can add/edit tanks from the admin UI. Target Dataverse table: bcrm_cryotank
    /// (see 01-DATA-MODEL.md §3.11 and 04-CRUD-AND-EDITING.md §6).
    /// </summary>
    public class CryoTankService : ICryoTankService
    {
        #region Properties
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Constructor
        public CryoTankService() { _unitOfWork = new UnitOfWork(); }
        public CryoTankService(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }
        #endregion

        #region Reads

        public object GetBySite(string siteId)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoTankService, Method: GetBySite, Action: Starting..");
                var siteFilter = (!string.IsNullOrWhiteSpace(siteId) && Guid.TryParse(siteId, out _))
                    ? string.Format("<condition attribute='bcrm_site' operator='eq' value='{0}' />", siteId)
                    : string.Empty;

                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_cryotank'>
                        <attribute name='bcrm_cryotankid' />
                        <attribute name='bcrm_name' />
                        <attribute name='bcrm_site' />
                        <attribute name='bcrm_capacity' />
                        <attribute name='bcrm_currentusage' />
                        <attribute name='bcrm_active' />
                        <attribute name='bcrm_status' />
                        <attribute name='modifiedon' />
                        <order attribute='bcrm_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                          {0}
                        </filter>
                      </entity>
                    </fetch>", siteFilter);

                dynamic result = new List<ExpandoObject>();
                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                if (rows != null && rows.Entities != null)
                {
                    foreach (var x in rows.Entities)
                    {
                        dynamic a = new ExpandoObject();
                        a.Id = x.Id.ToString();
                        a.Name = Str(x, "bcrm_name");
                        a.SiteName = RefName(x, "bcrm_site");
                        a.Capacity = Int(x, "bcrm_capacity");
                        a.CurrentUsage = Int(x, "bcrm_currentusage");
                        a.Active = Bool(x, "bcrm_active");
                        a.Status = Label(x, "bcrm_status");
                        a.ModifiedOn = Date(x, "modifiedon");
                        ((List<ExpandoObject>)result).Add(a);
                    }
                }
                logger.Info("Type: Info, Location: CryoTankService, Method: GetBySite, Action: Completed.");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoTankService, Method: GetBySite, Error: " + ex.Message);
                throw;
            }
        }

        public CryoTankDTO GetById(string id)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoTankService, Method: GetById, Action: Starting..");
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_cryotank'>
                        <all-attributes />
                        <filter type='and'>
                          <condition attribute='bcrm_cryotankid' operator='eq' value='{0}' />
                        </filter>
                      </entity>
                    </fetch>", id);

                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                CryoTankDTO dto = null;
                if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
                {
                    var x = rows.Entities.First();
                    dto = new CryoTankDTO
                    {
                        Id = x.Id.ToString(),
                        Name = Str(x, "bcrm_name"),
                        SiteId = RefId(x, "bcrm_site"),
                        SiteName = RefName(x, "bcrm_site"),
                        Capacity = Int(x, "bcrm_capacity"),
                        CurrentUsage = Int(x, "bcrm_currentusage"),
                        Active = Bool(x, "bcrm_active"),
                        Status = Label(x, "bcrm_status"),
                        Notes = Str(x, "bcrm_notes"),
                        ModifiedBy = RefName(x, "modifiedby"),
                        ModifiedOn = Date(x, "modifiedon"),
                        CreatedOn = Date(x, "createdon")
                    };
                }
                logger.Info("Type: Info, Location: CryoTankService, Method: GetById, Action: Completed.");
                return dto;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoTankService, Method: GetById, Error: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Writes

        public Guid Create(CryoTankDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoTankService, Method: Create, Action: Starting..");
                Guard(dto);
                var e = new Entity("bcrm_cryotank");
                Apply(e, dto);
                var id = _unitOfWork.CRMCoreRepository.Create(e);
                logger.Info("Type: Info, Location: CryoTankService, Method: Create, Action: Completed.");
                return id;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoTankService, Method: Create, Error: " + ex.Message);
                throw;
            }
        }

        public bool Update(CryoTankDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoTankService, Method: Update, Action: Starting..");
                if (string.IsNullOrWhiteSpace(dto.Id)) throw new Exception("Id is required for update.");
                Guard(dto);
                CheckConcurrency(dto.Id, dto.ModifiedOn);
                var e = new Entity("bcrm_cryotank") { Id = Guid.Parse(dto.Id) };
                Apply(e, dto);
                _unitOfWork.CRMCoreRepository.Update(e);
                logger.Info("Type: Info, Location: CryoTankService, Method: Update, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoTankService, Method: Update, Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>Deactivate a tank (soft). Hard delete is blocked while specimens reference it.</summary>
        public bool Delete(Guid id)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoTankService, Method: Delete, Action: Starting..");
                var e = new Entity("bcrm_cryotank") { Id = id };
                e["bcrm_active"] = false;
                e["statecode"] = new OptionSetValue(1);
                e["statuscode"] = new OptionSetValue(2);
                _unitOfWork.CRMCoreRepository.Update(e);
                logger.Info("Type: Info, Location: CryoTankService, Method: Delete, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoTankService, Method: Delete, Error: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Helpers

        private static void Apply(Entity e, CryoTankDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Name)) e["bcrm_name"] = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.SiteId) && Guid.TryParse(dto.SiteId, out var g))
                e["bcrm_site"] = new EntityReference("bcrm_site", g);
            if (dto.Capacity.HasValue) e["bcrm_capacity"] = dto.Capacity.Value;
            if (dto.CurrentUsage.HasValue) e["bcrm_currentusage"] = dto.CurrentUsage.Value;
            e["bcrm_active"] = dto.Active;
            if (dto.StatusValue.HasValue) e["bcrm_status"] = new OptionSetValue(dto.StatusValue.Value);
            if (!string.IsNullOrWhiteSpace(dto.Notes)) e["bcrm_notes"] = dto.Notes;
        }

        private void CheckConcurrency(string id, DateTime? clientModifiedOn)
        {
            if (!clientModifiedOn.HasValue) return;
            var fetch = string.Format(@"
                <fetch top='1' mapping='logical'>
                  <entity name='bcrm_cryotank'>
                    <attribute name='modifiedon' />
                    <filter type='and'><condition attribute='bcrm_cryotankid' operator='eq' value='{0}' /></filter>
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

        /// <summary>
        /// Inventory = the frozen specimens (bcrm_egg) stored in this tank. Derived, not a table
        /// (01-DATA-MODEL.md sheet 20). Scoped by the tank reference on the specimen
        /// (bcrm_nitrogen_tank). Confirm that column's type/name against the live schema — if it is
        /// a text field rather than a lookup, filter by tank name instead of the guid.
        /// </summary>
        public object GetInventory(string tankId)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoTankService, Method: GetInventory, Action: Starting..");
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_egg'>
                        <attribute name='bcrm_eggid' />
                        <attribute name='bcrm_eggs_id' />
                        <attribute name='bcrm_type' />
                        <attribute name='bcrm_patient' />
                        <attribute name='bcrm_treatment_cycle' />
                        <attribute name='bcrm_freeze_date' />
                        <attribute name='bcrm_location' />
                        <attribute name='bcrm_goblet' />
                        <attribute name='bcrm_straw' />
                        <attribute name='bcrm_egg_status' />
                        <order attribute='bcrm_freeze_date' descending='true' />
                        <filter type='and'>
                          <condition attribute='bcrm_frozen' operator='eq' value='1' />
                          <condition attribute='bcrm_nitrogen_tank' operator='eq' value='{0}' />
                        </filter>
                      </entity>
                    </fetch>", tankId);

                dynamic result = new List<ExpandoObject>();
                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                if (rows != null && rows.Entities != null)
                {
                    foreach (var x in rows.Entities)
                    {
                        dynamic a = new ExpandoObject();
                        a.Id = x.Id.ToString();
                        a.SpecimenId = Str(x, "bcrm_eggs_id");
                        a.Type = Label(x, "bcrm_type");
                        a.Patient = RefName(x, "bcrm_patient");
                        a.TreatmentCycle = RefName(x, "bcrm_treatment_cycle");
                        a.FreezeDate = Date(x, "bcrm_freeze_date");
                        a.Location = Str(x, "bcrm_location");
                        a.Goblet = Str(x, "bcrm_goblet");
                        a.Straw = Str(x, "bcrm_straw");
                        a.Status = Str(x, "bcrm_egg_status");
                        ((List<ExpandoObject>)result).Add(a);
                    }
                }
                logger.Info("Type: Info, Location: CryoTankService, Method: GetInventory, Action: Completed.");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoTankService, Method: GetInventory, Error: " + ex.Message);
                throw;
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
        private static bool Bool(Entity x, string f) => x.Attributes.Contains(f) && x[f] is bool b && b;
        private static string Label(Entity x, string f) => x.Attributes.Contains(f) && x.FormattedValues.Contains(f) ? x.FormattedValues[f] : string.Empty;
        private static DateTime? Date(Entity x, string f) => x.Attributes.Contains(f) && x[f] is DateTime dt ? dt : (DateTime?)null;
        private static string RefId(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Id.ToString() : string.Empty;
        private static string RefName(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Name : string.Empty;

        #endregion
    }
}
