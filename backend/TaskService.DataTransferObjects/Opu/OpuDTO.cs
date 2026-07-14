using System;

namespace OXAR.DataTransferObjects.Opu
{
    /// <summary>
    /// DTO for Egg Collection (Oocyte Pick-Up) — Dataverse table 'bcrm_opu'.
    /// OX.ar Embryo Module (M-18). See docs/embryo-module/01-DATA-MODEL.md §3.4.
    /// </summary>
    public class OpuDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }                // auto-number OPU-{seq}

        public string TreatmentCycleId { get; set; }
        public string TreatmentCycleName { get; set; }
        public DateTime? ProcedureDate { get; set; }

        public string DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string EmbryologistId { get; set; }
        public string EmbryologistName { get; set; }
        public string WitnessId { get; set; }
        public string WitnessName { get; set; }

        public int? FolliclesAspirated { get; set; }
        public int? EggsRetrieved { get; set; }
        public int? Mii { get; set; }                   // mature — ICSI eligible
        public int? Mi { get; set; }
        public int? Gv { get; set; }

        public string Complications { get; set; }
        public string Notes { get; set; }

        // Editing / audit / concurrency
        public string CancelReason { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
