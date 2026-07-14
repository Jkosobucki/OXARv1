using System;
using OXAR.DataTransferObjects.EmbryoEvent;

namespace OXAR.Services.EmbryoEvent
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Embryology Event service (worklist / diary / cycle timeline).
    /// Full CRUD. Fully independent of OX.gp.
    /// </summary>
    public interface IEmbryoEventService
    {
        object GetByCycleId(string cycleId);
        object GetWorklist(string siteId, DateTime from, DateTime to, string status, string eventType);
        EmbryoEventDTO GetById(string id);
        Guid Create(EmbryoEventDTO dto, string userId);
        bool Update(EmbryoEventDTO dto, string userId);
        bool Cancel(Guid id, string reason);
        bool Delete(Guid id);
    }
}
