using System;

namespace OXAR.DataTransferObjects.CryoLocation
{
    /// <summary>
    /// DTO for a cryo storage position within a tank (canister -> goblet -> straw/vial) —
    /// Dataverse table 'bcrm_cryolocation'. Runtime-editable reference data (full CRUD).
    /// OX.ar Embryo Module (M-18). See 01-DATA-MODEL.md 3.11 and 04-CRUD-AND-EDITING.md 6.
    /// </summary>
    public class CryoLocationDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }              // auto-number LOC-{seq}
        public string TankId { get; set; }
        public string TankName { get; set; }
        public string CanisterNumber { get; set; }
        public string GobletNumber { get; set; }
        public string StrawVialNumber { get; set; }
        public string StorageType { get; set; }       // Embryo / Sperm / Oocyte
        public int? StorageTypeValue { get; set; }
        public string StorageStatus { get; set; }      // Occupied / Available / Reserved
        public int? StorageStatusValue { get; set; }
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public string Notes { get; set; }

        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
