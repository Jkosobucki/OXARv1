using System;
using OXAR.DataTransferObjects.Opu;

namespace OXAR.Services.Opu
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Egg Collection (OPU) service. Full CRUD.
    /// </summary>
    public interface IOpuService
    {
        object GetByCycleId(string cycleId);
        OpuDTO GetById(string id);
        Guid Create(OpuDTO dto, string userId);
        bool Update(OpuDTO dto, string userId);
        bool Cancel(Guid id, string reason);
        bool Delete(Guid id);
    }
}
