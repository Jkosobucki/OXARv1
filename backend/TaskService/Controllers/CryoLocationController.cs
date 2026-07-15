using NLog;
using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using OXAR.DataTransferObjects.CryoLocation;
using OXAR.Services.CryoLocation;

namespace OXAR.Controllers
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Cryo storage-position API (canister/goblet/straw).
    /// Full CRUD (manager/admin gated for writes) so the lab can lay out & manage tank contents.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [Authorize]
    [RoutePrefix("api/CryoLocation")]
    public class CryoLocationController : ApiController
    {
        private readonly ICryoLocationService _service;
        static Logger logger = LogManager.GetCurrentClassLogger();

        private CryoLocationController()
        {
            _service = new CryoLocationService();
        }

        [HttpGet, Route("GetByTank")]
        public IHttpActionResult GetByTank(string tankId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tankId) || !Guid.TryParse(tankId, out _))
                    return BadRequest("tankId must be a valid guid.");
                return Ok(_service.GetByTank(tankId));
            }
            catch (Exception ex) { return Handle(ex, "GetByTank"); }
        }

        [HttpGet, Route("GetAvailableByTank")]
        public IHttpActionResult GetAvailableByTank(string tankId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tankId) || !Guid.TryParse(tankId, out _))
                    return BadRequest("tankId must be a valid guid.");
                return Ok(_service.GetAvailableByTank(tankId));
            }
            catch (Exception ex) { return Handle(ex, "GetAvailableByTank"); }
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

        [HttpPost, Route("Create")]
        public IHttpActionResult Post(CryoLocationDTO dto)
        {
            try
            {
                if (dto == null) return BadRequest("Body is required.");
                if (!IsManager()) return Content(HttpStatusCode.Forbidden, "Only a manager/admin may add a location.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Create(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Create"); }
        }

        [HttpPut, Route("Update")]
        public IHttpActionResult Put(CryoLocationDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Id is required.");
                if (!IsManager()) return Content(HttpStatusCode.Forbidden, "Only a manager/admin may edit a location.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Update(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Update"); }
        }

        /// <summary>Set Occupied/Available/Reserved (used by freeze/thaw allocation).</summary>
        [HttpPut, Route("SetStatus")]
        public IHttpActionResult SetStatus(Guid id, int statusValue)
        {
            try { return Ok(_service.SetStatus(id, statusValue)); }
            catch (Exception ex) { return Handle(ex, "SetStatus"); }
        }

        [HttpDelete, Route("Delete")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                if (!IsManager()) return Content(HttpStatusCode.Forbidden, "Only a manager/admin may deactivate a location.");
                return Ok(_service.Delete(id));
            }
            catch (Exception ex) { return Handle(ex, "Delete"); }
        }

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
            logger.Error("Type: Error, Location: CryoLocationController, Method: " + method + ", Error: " + inner);
            if (!string.IsNullOrEmpty(inner) && inner.StartsWith("CONFLICT", StringComparison.OrdinalIgnoreCase))
                return Content(HttpStatusCode.Conflict, inner);
            return BadRequest(inner);
        }
    }
}
