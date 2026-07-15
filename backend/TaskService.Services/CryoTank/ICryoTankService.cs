using System;
using OXAR.DataTransferObjects.CryoTank;

namespace OXAR.Services.CryoTank
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Cryo Tank reference-data service. Full CRUD so lab managers
    /// can add/edit tanks at runtime.
    /// </summary>
    public interface ICryoTankService
    {
        object GetBySite(string siteId);
        CryoTankDTO GetById(string id);
        Guid Create(CryoTankDTO dto, string userId);
        bool Update(CryoTankDTO dto, string userId);
        bool Delete(Guid id);
    }
}
