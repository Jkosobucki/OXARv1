using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OXAR.DataTransferObjects.CryoLocation;
using OXAR.Repository.UnitOfWork;
using TaskService.Services.Helper;

namespace OXAR.Services.CryoLocation
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Cryo storage-position service. Target table: bcrm_cryolocation.
    /// </summary>
    public class CryoLocationService : ICryoLocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public CryoLocationService() { _unitOfWork = new UnitOfWork(); }
        public CryoLocationService(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

        public object GetByTank(string tankId) { return Query(tankId, null); }

        public object GetAvailableByTank(string tankId)
        {
            // storage status "Available" — confirm the option-set value for your org (2 used here).
            return Query(tankId, 2);
        }

        private object Query(string tankId, int? statusValue)
        {
            try
            {
                logger.Info("Type: Info, Location: CryoLocationService, Method: Query, Action: Starting..");
                var statusFilter = statusValue.HasValue
                    ? string.Format("<condition attribute='bcrm_storagestatus' operator='eq' value='{0}' />", statusValue.Value)
                    : string.Empty;
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_cryolocation'>
                        <attribute name='bcrm_cryolocationid' />
                        <attribute name='bcrm_name' />
                        <attribute name='bcrm_tank' />
                        <attribute name='bcrm_canisternumber' />
                        <attribute name='bcrm_gobletnumber' />
                        <attribute name='bcrm_strawvialnumber' />
                        <attribute name='bcrm_storagetype' />
                        <attribute name='bcrm_storagestatus' />
                        <attribute name='modifiedon' />
                        <order attribute='bcrm_canisternumber' descending='false' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                          <condition attribute='bcrm_tank' operator='eq' value='{0}' />
                          {1}
                        </filter>
                      </entity>
                    </fetch>", tankId, statusFilter);

                dynamic result = new List<ExpandoObject>();
                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                if (rows != null && rows.Entities != null)
                {
                    foreach (var x in rows.Entities)
                    {
                        dynamic a = new ExpandoObject();
                        a.Id = x.Id.ToString();
                        a.Name = Str(x, "bcrm_name");
                        a.CanisterNumber = Str(x, "bcrm_canisternumber");
                        a.GobletNumber = Str(x, "bcrm_gobletnumber");
                        a.StrawVialNumber = Str(x, "bcrm_strawvialnumber");
                        a.StorageType = Label(x, "bcrm_storagetype");
                        a.StorageStatus = Label(x, "bcrm_storagestatus");
                        a.ModifiedOn = Date(x, "modifiedon");
                        ((List<ExpandoObject>)result).Add(a);
                    }
                }
                logger.Info("Type: Info, Location: CryoLocationService, Method: Query, Action: Completed.");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoLocationService, Method: Query, Error: " + ex.Message);
                throw;
            }
        }

        public CryoLocationDTO GetById(string id)
        {
            try
            {
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_cryolocation'>
                        <all-attributes />
                        <filter type='and'><condition attribute='bcrm_cryolocationid' operator='eq' value='{0}' /></filter>
                      </entity>
                    </fetch>", id);
                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                CryoLocationDTO dto = null;
                if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
                {
                    var x = rows.Entities.First();
                    dto = new CryoLocationDTO
                    {
                        Id = x.Id.ToString(),
                        Name = Str(x, "bcrm_name"),
                        TankId = RefId(x, "bcrm_tank"),
                        TankName = RefName(x, "bcrm_tank"),
                        CanisterNumber = Str(x, "bcrm_canisternumber"),
                        GobletNumber = Str(x, "bcrm_gobletnumber"),
                        StrawVialNumber = Str(x, "bcrm_strawvialnumber"),
                        StorageType = Label(x, "bcrm_storagetype"),
                        StorageStatus = Label(x, "bcrm_storagestatus"),
                        SiteId = RefId(x, "bcrm_site"),
                        SiteName = RefName(x, "bcrm_site"),
                        Notes = Str(x, "bcrm_notes"),
                        ModifiedBy = RefName(x, "modifiedby"),
                        ModifiedOn = Date(x, "modifiedon"),
                        CreatedOn = Date(x, "createdon")
                    };
                }
                return dto;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: CryoLocationService, Method: GetById, Error: " + ex.Message);
                throw;
            }
        }

        public Guid Create(CryoLocationDTO dto, string userId)
        {
            try
            {
                Guard(dto);
                var e = new Entity("bcrm_cryolocation");
                Apply(e, dto);
                return _unitOfWork.CRMCoreRepository.Create(e);
            }
            catch (Exception ex) { logger.Error("CryoLocationService.Create: " + ex.Message); throw; }
        }

        public bool Update(CryoLocationDTO dto, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Id)) throw new Exception("Id is required for update.");
                Guard(dto);
                CheckConcurrency(dto.Id, dto.ModifiedOn);
                var e = new Entity("bcrm_cryolocation") { Id = Guid.Parse(dto.Id) };
                Apply(e, dto);
                _unitOfWork.CRMCoreRepository.Update(e);
                return true;
            }
            catch (Exception ex) { logger.Error("CryoLocationService.Update: " + ex.Message); throw; }
        }

        public bool SetStatus(Guid id, int statusValue)
        {
            try
            {
                var e = new Entity("bcrm_cryolocation") { Id = id };
                e["bcrm_storagestatus"] = new OptionSetValue(statusValue);
                _unitOfWork.CRMCoreRepository.Update(e);
                return true;
            }
            catch (Exception ex) { logger.Error("CryoLocationService.SetStatus: " + ex.Message); throw; }
        }

        public bool Delete(Guid id)
        {
            try
            {
                var e = new Entity("bcrm_cryolocation") { Id = id };
                e["statecode"] = new OptionSetValue(1);
                e["statuscode"] = new OptionSetValue(2);
                _unitOfWork.CRMCoreRepository.Update(e);   // soft-retire; hard delete blocked if occupied
                return true;
            }
            catch (Exception ex) { logger.Error("CryoLocationService.Delete: " + ex.Message); throw; }
        }

        private static void Apply(Entity e, CryoLocationDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Name)) e["bcrm_name"] = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.TankId) && Guid.TryParse(dto.TankId, out var g))
                e["bcrm_tank"] = new EntityReference("bcrm_cryotank", g);
            if (!string.IsNullOrWhiteSpace(dto.CanisterNumber)) e["bcrm_canisternumber"] = dto.CanisterNumber;
            if (!string.IsNullOrWhiteSpace(dto.GobletNumber)) e["bcrm_gobletnumber"] = dto.GobletNumber;
            if (!string.IsNullOrWhiteSpace(dto.StrawVialNumber)) e["bcrm_strawvialnumber"] = dto.StrawVialNumber;
            if (dto.StorageTypeValue.HasValue) e["bcrm_storagetype"] = new OptionSetValue(dto.StorageTypeValue.Value);
            if (dto.StorageStatusValue.HasValue) e["bcrm_storagestatus"] = new OptionSetValue(dto.StorageStatusValue.Value);
            if (!string.IsNullOrWhiteSpace(dto.SiteId) && Guid.TryParse(dto.SiteId, out var gs))
                e["bcrm_site"] = new EntityReference("bcrm_site", gs);
            if (!string.IsNullOrWhiteSpace(dto.Notes)) e["bcrm_notes"] = dto.Notes;
        }

        private void CheckConcurrency(string id, DateTime? clientModifiedOn)
        {
            if (!clientModifiedOn.HasValue) return;
            var fetch = string.Format(@"
                <fetch top='1' mapping='logical'>
                  <entity name='bcrm_cryolocation'>
                    <attribute name='modifiedon' />
                    <filter type='and'><condition attribute='bcrm_cryolocationid' operator='eq' value='{0}' /></filter>
                  </entity>
                </fetch>", id);
            var rows = _unitOfWork.CRMCoreRepository.Get(fetch);
            if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
            {
                var current = Date(rows.Entities.First(), "modifiedon");
                if (current.HasValue && Math.Abs((current.Value - clientModifiedOn.Value).TotalSeconds) > 1)
                    throw new Exception("CONFLICT: this location was changed by someone else. Reload and re-apply your edit.");
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
        private static string Label(Entity x, string f) => x.Attributes.Contains(f) && x.FormattedValues.Contains(f) ? x.FormattedValues[f] : string.Empty;
        private static DateTime? Date(Entity x, string f) => x.Attributes.Contains(f) && x[f] is DateTime dt ? dt : (DateTime?)null;
        private static string RefId(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Id.ToString() : string.Empty;
        private static string RefName(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Name : string.Empty;
    }
}
