using System;
using OXAR.DataTransferObjects.CryoLocation;

namespace OXAR.Services.CryoLocation
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Cryo storage-position (canister/goblet/straw) service.
    /// Full CRUD so the lab can lay out and manage tank contents.
    /// </summary>
    public interface ICryoLocationService
    {
        object GetByTank(string tankId);            // all positions in a tank (graphical view)
        object GetAvailableByTank(string tankId);   // free slots for allocation
        CryoLocationDTO GetById(string id);
        Guid Create(CryoLocationDTO dto, string userId);
        bool Update(CryoLocationDTO dto, string userId);
        bool SetStatus(Guid id, int statusValue);   // Occupied / Available / Reserved
        bool Delete(Guid id);
    }
}
