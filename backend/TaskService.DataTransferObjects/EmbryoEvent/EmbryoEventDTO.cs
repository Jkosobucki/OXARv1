using System;

namespace OXAR.DataTransferObjects.EmbryoEvent
{
    /// <summary>
    /// DTO for the Embryology Event (worklist / diary spine) — Dataverse table 'bcrm_embryoevent'.
    /// Part of the OX.ar Embryo Module (M-18). See docs/embryo-module/01-DATA-MODEL.md §3.1.
    /// Fully independent of OX.gp.
    /// </summary>
    public class EmbryoEventDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }

        // Option sets: label is returned on read (FormattedValues); integer value is sent on write.
        public string EventType { get; set; }
        public int? EventTypeValue { get; set; }
        public string Status { get; set; }
        public int? StatusValue { get; set; }
        public string Priority { get; set; }
        public int? PriorityValue { get; set; }

        public DateTime? ScheduledDate { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }

        // Lookups surfaced as Id (guid) + Name.
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string PartnerId { get; set; }
        public string TreatmentCycleId { get; set; }
        public string TreatmentCycleName { get; set; }
        public string EmbryologistId { get; set; }
        public string EmbryologistName { get; set; }
        public string WitnessId { get; set; }
        public string WitnessName { get; set; }

        public string MaterialType { get; set; }
        public int? MaterialTypeValue { get; set; }
        public string MaterialRecordId { get; set; }   // bcrm_egg guid

        public string Outcome { get; set; }
        public string OutcomeNotes { get; set; }
        public string Comments { get; set; }

        // Diary
        public string Room { get; set; }
        public string AssignedUserId { get; set; }

        // Editing / audit / concurrency (see 04-CRUD-AND-EDITING.md §3)
        public string CancelReason { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }       // echo back on Update for optimistic concurrency
        public DateTime? CreatedOn { get; set; }
    }
}
