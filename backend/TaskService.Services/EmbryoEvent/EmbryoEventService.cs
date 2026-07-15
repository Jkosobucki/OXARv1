using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OXAR.DataTransferObjects.EmbryoEvent;
using OXAR.Repository.UnitOfWork;
using TaskService.Services.Helper;

namespace OXAR.Services.EmbryoEvent
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Embryology Event service.
    /// Reads via FetchXML, writes via Entity + CRMCoreRepository (mirrors StoredSampleService /
    /// PreviousProcedureService). Fully independent of OX.gp.
    /// Target Dataverse table: bcrm_embryoevent (create it first — see 01-DATA-MODEL.md §3.1).
    /// </summary>
    public class EmbryoEventService : IEmbryoEventService
    {
        #region Properties
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Constructor
        public EmbryoEventService()
        {
            _unitOfWork = new UnitOfWork();
        }

        public EmbryoEventService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Reads

        /// <summary>Timeline of events for one treatment cycle.</summary>
        public object GetByCycleId(string cycleId)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetByCycleId, Action: Starting..");
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_embryoevent'>
                        <attribute name='bcrm_embryoeventid' />
                        <attribute name='bcrm_name' />
                        <attribute name='bcrm_eventtype' />
                        <attribute name='bcrm_status' />
                        <attribute name='bcrm_priority' />
                        <attribute name='bcrm_scheduleddate' />
                        <attribute name='bcrm_scheduledstarttime' />
                        <attribute name='bcrm_scheduledendtime' />
                        <attribute name='bcrm_patient' />
                        <attribute name='bcrm_partner' />
                        <attribute name='bcrm_embryologist' />
                        <attribute name='bcrm_witness' />
                        <attribute name='bcrm_materialrecord' />
                        <attribute name='bcrm_outcome' />
                        <attribute name='modifiedon' />
                        <attribute name='modifiedby' />
                        <order attribute='bcrm_scheduleddate' descending='false' />
                        <filter type='and'>
                          <condition attribute='bcrm_treatment_cycle' operator='eq' value='{0}' />
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>", cycleId);

                var result = MapList(_unitOfWork.CRMCoreRepository.Get(fetchXML));
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetByCycleId, Action: Completed.");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: GetByCycleId, Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>Site worklist for a date range, with optional status / event-type filters.</summary>
        public object GetWorklist(string siteId, DateTime from, DateTime to, string status, string eventType)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetWorklist, Action: Starting..");

                var conditions = new System.Text.StringBuilder();
                conditions.AppendFormat("<condition attribute='bcrm_scheduleddate' operator='on-or-after' value='{0}' />",
                    from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                conditions.AppendFormat("<condition attribute='bcrm_scheduleddate' operator='on-or-before' value='{0}' />",
                    to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                conditions.Append("<condition attribute='statecode' operator='eq' value='0' />");
                if (!string.IsNullOrWhiteSpace(siteId) && Guid.TryParse(siteId, out _))
                    conditions.AppendFormat("<condition attribute='bcrm_site' operator='eq' value='{0}' />", siteId);
                if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out _))
                    conditions.AppendFormat("<condition attribute='bcrm_status' operator='eq' value='{0}' />", status);
                if (!string.IsNullOrWhiteSpace(eventType) && int.TryParse(eventType, out _))
                    conditions.AppendFormat("<condition attribute='bcrm_eventtype' operator='eq' value='{0}' />", eventType);

                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_embryoevent'>
                        <attribute name='bcrm_embryoeventid' />
                        <attribute name='bcrm_name' />
                        <attribute name='bcrm_eventtype' />
                        <attribute name='bcrm_status' />
                        <attribute name='bcrm_priority' />
                        <attribute name='bcrm_scheduleddate' />
                        <attribute name='bcrm_patient' />
                        <attribute name='bcrm_treatment_cycle' />
                        <attribute name='bcrm_materialrecord' />
                        <attribute name='modifiedon' />
                        <order attribute='bcrm_scheduleddate' descending='false' />
                        <filter type='and'>{0}</filter>
                      </entity>
                    </fetch>", conditions.ToString());

                var result = MapList(_unitOfWork.CRMCoreRepository.Get(fetchXML));
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetWorklist, Action: Completed.");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: GetWorklist, Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>Load a single event to populate the edit form.</summary>
        public EmbryoEventDTO GetById(string id)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetById, Action: Starting..");
                var fetchXML = string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bcrm_embryoevent'>
                        <all-attributes />
                        <filter type='and'>
                          <condition attribute='bcrm_embryoeventid' operator='eq' value='{0}' />
                        </filter>
                      </entity>
                    </fetch>", id);

                var rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                EmbryoEventDTO dto = null;
                if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
                {
                    var x = rows.Entities.First();
                    dto = new EmbryoEventDTO
                    {
                        Id = x.Id.ToString(),
                        Name = Str(x, "bcrm_name"),
                        EventType = Label(x, "bcrm_eventtype"),
                        Status = Label(x, "bcrm_status"),
                        Priority = Label(x, "bcrm_priority"),
                        ScheduledDate = Date(x, "bcrm_scheduleddate"),
                        ScheduledStartTime = Date(x, "bcrm_scheduledstarttime"),
                        ScheduledEndTime = Date(x, "bcrm_scheduledendtime"),
                        ActualStartTime = Date(x, "bcrm_actualstarttime"),
                        ActualEndTime = Date(x, "bcrm_actualendtime"),
                        PatientId = RefId(x, "bcrm_patient"),
                        PatientName = RefName(x, "bcrm_patient"),
                        PartnerId = RefId(x, "bcrm_partner"),
                        TreatmentCycleId = RefId(x, "bcrm_treatment_cycle"),
                        TreatmentCycleName = RefName(x, "bcrm_treatment_cycle"),
                        EmbryologistId = RefId(x, "bcrm_embryologist"),
                        EmbryologistName = RefName(x, "bcrm_embryologist"),
                        WitnessId = RefId(x, "bcrm_witness"),
                        WitnessName = RefName(x, "bcrm_witness"),
                        MaterialType = Label(x, "bcrm_materialtype"),
                        MaterialRecordId = RefId(x, "bcrm_materialrecord"),
                        Outcome = Str(x, "bcrm_outcome"),
                        OutcomeNotes = Str(x, "bcrm_outcomenotes"),
                        Comments = Str(x, "bcrm_comments"),
                        ModifiedBy = RefName(x, "modifiedby"),
                        ModifiedOn = Date(x, "modifiedon"),
                        CreatedOn = Date(x, "createdon")
                    };
                }
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetById, Action: Completed.");
                return dto;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: GetById, Error: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Writes

        /// <summary>Add a new event.</summary>
        public Guid Create(EmbryoEventDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Create, Action: Starting..");
                Guard(dto);
                ValidateWitness(dto);

                var e = new Entity("bcrm_embryoevent");
                ApplyEditableFields(e, dto);
                var id = _unitOfWork.CRMCoreRepository.Create(e);
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Create, Action: Completed.");
                return id;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: Create, Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>Edit an existing event (partial merge — only set attributes are written).</summary>
        public bool Update(EmbryoEventDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Update, Action: Starting..");
                if (string.IsNullOrWhiteSpace(dto.Id))
                    throw new Exception("Id is required for update.");
                Guard(dto);
                ValidateWitness(dto);
                CheckConcurrency("bcrm_embryoevent", "bcrm_embryoeventid", dto.Id, dto.ModifiedOn);

                var e = new Entity("bcrm_embryoevent") { Id = Guid.Parse(dto.Id) };
                ApplyEditableFields(e, dto);
                _unitOfWork.CRMCoreRepository.Update(e);
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Update, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: Update, Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>Soft-retire: set state Inactive + record a reason (preferred over hard delete).</summary>
        public bool Cancel(Guid id, string reason)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Cancel, Action: Starting..");
                if (string.IsNullOrWhiteSpace(reason))
                    throw new Exception("A cancel reason is required.");

                var e = new Entity("bcrm_embryoevent") { Id = id };
                e["bcrm_cancelreason"] = reason;
                e["statecode"] = new OptionSetValue(1);   // Inactive
                e["statuscode"] = new OptionSetValue(2);  // Inactive (default)
                _unitOfWork.CRMCoreRepository.Update(e);
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Cancel, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: Cancel, Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>Hard delete — manager only (gate in the controller). Prefer Cancel for reportable data.</summary>
        public bool Delete(Guid id)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Delete, Action: Starting..");
                _unitOfWork.CRMCoreRepository.Delete(new Entity("bcrm_embryoevent", id));
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Delete, Action: Completed.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Type: Error, Location: EmbryoEventService, Method: Delete, Error: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Helpers

        private static void ApplyEditableFields(Entity e, EmbryoEventDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Name)) e["bcrm_name"] = dto.Name;
            if (dto.EventTypeValue.HasValue) e["bcrm_eventtype"] = new OptionSetValue(dto.EventTypeValue.Value);
            if (dto.StatusValue.HasValue) e["bcrm_status"] = new OptionSetValue(dto.StatusValue.Value);
            if (dto.PriorityValue.HasValue) e["bcrm_priority"] = new OptionSetValue(dto.PriorityValue.Value);
            if (dto.MaterialTypeValue.HasValue) e["bcrm_materialtype"] = new OptionSetValue(dto.MaterialTypeValue.Value);

            SetDate(e, "bcrm_scheduleddate", dto.ScheduledDate);
            SetDate(e, "bcrm_scheduledstarttime", dto.ScheduledStartTime);
            SetDate(e, "bcrm_scheduledendtime", dto.ScheduledEndTime);
            SetDate(e, "bcrm_actualstarttime", dto.ActualStartTime);
            SetDate(e, "bcrm_actualendtime", dto.ActualEndTime);

            SetRef(e, "bcrm_patient", "contact", dto.PatientId);
            SetRef(e, "bcrm_partner", "contact", dto.PartnerId);
            SetRef(e, "bcrm_treatment_cycle", "bcrm_treatment_cycle", dto.TreatmentCycleId);
            SetRef(e, "bcrm_embryologist", "systemuser", dto.EmbryologistId);
            SetRef(e, "bcrm_witness", "systemuser", dto.WitnessId);
            SetRef(e, "bcrm_materialrecord", "bcrm_egg", dto.MaterialRecordId);
            SetRef(e, "bcrm_assigneduser", "systemuser", dto.AssignedUserId);

            if (!string.IsNullOrWhiteSpace(dto.Outcome)) e["bcrm_outcome"] = dto.Outcome;
            if (!string.IsNullOrWhiteSpace(dto.OutcomeNotes)) e["bcrm_outcomenotes"] = dto.OutcomeNotes;
            if (!string.IsNullOrWhiteSpace(dto.Comments)) e["bcrm_comments"] = dto.Comments;
            if (!string.IsNullOrWhiteSpace(dto.Room)) e["bcrm_room"] = dto.Room;
        }

        /// <summary>Independent-witness rule (HFEA): witness must differ from the performer.</summary>
        private static void ValidateWitness(EmbryoEventDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.WitnessId) &&
                !string.IsNullOrWhiteSpace(dto.EmbryologistId) &&
                string.Equals(dto.WitnessId, dto.EmbryologistId, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Witness must be a different person from the performing embryologist.");
            }
        }

        /// <summary>Optimistic concurrency: reject if the row moved since the client loaded it.</summary>
        private void CheckConcurrency(string entity, string idAttr, string id, DateTime? clientModifiedOn)
        {
            if (!clientModifiedOn.HasValue) return;   // client didn't supply a token — skip
            var fetch = string.Format(@"
                <fetch top='1' mapping='logical'>
                  <entity name='{0}'>
                    <attribute name='modifiedon' />
                    <filter type='and'><condition attribute='{1}' operator='eq' value='{2}' /></filter>
                  </entity>
                </fetch>", entity, idAttr, id);
            var rows = _unitOfWork.CRMCoreRepository.Get(fetch);
            if (rows != null && rows.Entities != null && rows.Entities.Count > 0)
            {
                var current = Date(rows.Entities.First(), "modifiedon");
                if (current.HasValue &&
                    Math.Abs((current.Value - clientModifiedOn.Value).TotalSeconds) > 1)
                {
                    // Controller maps a message starting with CONFLICT to HTTP 409.
                    throw new Exception("CONFLICT: this record was changed by someone else. Reload and re-apply your edit.");
                }
            }
        }

        private static void Guard(object dto)
        {
            var obj = JObject.Parse(JsonConvert.SerializeObject(dto));
            foreach (var prop in obj.Properties())
            {
                var value = prop.Value.ToString();
                if (new ACSHelper().ContainsSpecialCharacters(value))
                    throw new Exception("Invalid json");
            }
        }

        private static dynamic MapList(EntityCollection rows)
        {
            dynamic result = new List<ExpandoObject>();
            if (rows == null || rows.Entities == null) return result;
            foreach (var x in rows.Entities)
            {
                dynamic a = new ExpandoObject();
                a.Id = x.Id.ToString();
                a.Name = Str(x, "bcrm_name");
                a.EventType = Label(x, "bcrm_eventtype");
                a.Status = Label(x, "bcrm_status");
                a.Priority = Label(x, "bcrm_priority");
                a.ScheduledDate = Date(x, "bcrm_scheduleddate");
                a.PatientId = RefId(x, "bcrm_patient");
                a.PatientName = RefName(x, "bcrm_patient");
                a.TreatmentCycleId = RefId(x, "bcrm_treatment_cycle");
                a.MaterialRecordId = RefId(x, "bcrm_materialrecord");
                a.ModifiedOn = Date(x, "modifiedon");
                ((List<ExpandoObject>)result).Add(a);
            }
            return result;
        }

        // ---- attribute readers (null-safe) ----
        private static string Str(Entity x, string f) => x.Attributes.Contains(f) && x[f] != null ? x[f].ToString() : string.Empty;
        private static string Label(Entity x, string f) => x.Attributes.Contains(f) && x.FormattedValues.Contains(f) ? x.FormattedValues[f] : string.Empty;
        private static DateTime? Date(Entity x, string f) => x.Attributes.Contains(f) && x[f] is DateTime dt ? dt : (DateTime?)null;
        private static string RefId(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Id.ToString() : string.Empty;
        private static string RefName(Entity x, string f) => x.Attributes.Contains(f) && x[f] is EntityReference r ? r.Name : string.Empty;

        // ---- attribute writers ----
        private static void SetDate(Entity e, string f, DateTime? v)
        {
            if (v.HasValue && v.Value != DateTime.MinValue)
                e[f] = DateTime.SpecifyKind(v.Value, DateTimeKind.Utc);
        }
        private static void SetRef(Entity e, string f, string logicalName, string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var g))
                e[f] = new EntityReference(logicalName, g);
        }

        #endregion
    }
}
