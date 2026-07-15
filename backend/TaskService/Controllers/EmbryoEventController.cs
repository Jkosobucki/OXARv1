using NLog;
using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using OXAR.DataTransferObjects.EmbryoEvent;
using OXAR.Services.EmbryoEvent;

namespace OXAR.Controllers
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Embryology Event API (worklist / diary / cycle timeline).
    /// Full CRUD. Fully independent of OX.gp.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [Authorize]
    [RoutePrefix("api/EmbryoEvent")]
    public class EmbryoEventController : ApiController
    {
        #region Properties
        private readonly IEmbryoEventService _service;
        static Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Constructor
        private EmbryoEventController()
        {
            _service = new EmbryoEventService();
        }
        #endregion

        #region Reads

        [HttpGet, Route("GetByCycleId")]
        public IHttpActionResult GetByCycleId(string id)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventController, Method: GetByCycleId, Action: Starting..");
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _))
                    return BadRequest("id must be a valid guid.");
                var result = _service.GetByCycleId(id);
                logger.Info("Type: Info, Location: EmbryoEventController, Method: GetByCycleId, Action: Completed.");
                return Ok(result);
            }
            catch (Exception ex) { return Handle(ex, "GetByCycleId"); }
        }

        [HttpGet, Route("GetWorklist")]
        public IHttpActionResult GetWorklist(string siteId = null, DateTime? from = null, DateTime? to = null,
                                             string status = null, string eventType = null)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventController, Method: GetWorklist, Action: Starting..");
                var f = from ?? DateTime.UtcNow.Date;
                var t = to ?? f.AddDays(1);
                var result = _service.GetWorklist(siteId, f, t, status, eventType);
                logger.Info("Type: Info, Location: EmbryoEventController, Method: GetWorklist, Action: Completed.");
                return Ok(result);
            }
            catch (Exception ex) { return Handle(ex, "GetWorklist"); }
        }

        [HttpGet, Route("GetById")]
        public IHttpActionResult GetById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _))
                    return BadRequest("id must be a valid guid.");
                return Ok(_service.GetById(id));
            }
            catch (Exception ex) { return Handle(ex, "GetById"); }
        }

        #endregion

        #region Writes

        [HttpPost, Route("Create")]
        public IHttpActionResult Post(EmbryoEventDTO dto)
        {
            try
            {
                if (dto == null) return BadRequest("Body is required.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Create(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Create"); }
        }

        [HttpPut, Route("Update")]
        public IHttpActionResult Put(EmbryoEventDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Id is required.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Update(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Update"); }
        }

        /// <summary>Soft-retire with a reason (preferred over hard delete for reportable data).</summary>
        [HttpPut, Route("Cancel")]
        public IHttpActionResult Cancel(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason)) return BadRequest("reason is required.");
                return Ok(_service.Cancel(id, reason));
            }
            catch (Exception ex) { return Handle(ex, "Cancel"); }
        }

        /// <summary>Hard delete — team-manager only (M18-038).</summary>
        [HttpDelete, Route("Delete")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                if (!IsManager())
                    return Content(HttpStatusCode.Forbidden, "Only a team manager may delete an event.");
                return Ok(_service.Delete(id));
            }
            catch (Exception ex) { return Handle(ex, "Delete"); }
        }

        #endregion

        #region Infrastructure

        /// <summary>Role gate — reads the caller's lab role from the X-User-Role header (case-insensitive contains 'manager').</summary>
        private bool IsManager()
        {
            if (Request.Headers.TryGetValues("X-User-Role", out var vals))
            {
                var role = vals.FirstOrDefault() ?? string.Empty;
                return role.IndexOf("manager", StringComparison.OrdinalIgnoreCase) >= 0
                    || role.IndexOf("admin", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        private IHttpActionResult Handle(Exception ex, string method)
        {
            string inner = (ex.InnerException != null)
                ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message)
                : ex.Message;
            logger.Error("Type: Error, Location: EmbryoEventController, Method: " + method + ", Error: " + inner);
            if (!string.IsNullOrEmpty(inner) && inner.StartsWith("CONFLICT", StringComparison.OrdinalIgnoreCase))
                return Content(HttpStatusCode.Conflict, inner);   // 409 → UI reloads
            return BadRequest(inner);
        }

        #endregion
    }
}
