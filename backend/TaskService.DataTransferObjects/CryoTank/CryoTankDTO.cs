using System;

namespace OXAR.DataTransferObjects.CryoTank
{
    /// <summary>
    /// DTO for a cryo storage tank — Dataverse table 'bcrm_cryotank'. This is runtime-editable
    /// reference data (a lab manager can add/edit tanks): full CRUD, not an option set.
    /// OX.ar Embryo Module (M-18). See docs/embryo-module/01-DATA-MODEL.md §3.11 and
    /// 04-CRUD-AND-EDITING.md §6.
    /// </summary>
    public class CryoTankDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }                // auto-number TANK-{seq}
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public int? Capacity { get; set; }
        public int? CurrentUsage { get; set; }
        public bool Active { get; set; }
        public string Status { get; set; }              // OK / Low N2 / Maintenance / Alarm
        public int? StatusValue { get; set; }
        public string Notes { get; set; }

        // Editing / audit / concurrency
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
