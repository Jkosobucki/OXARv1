using NLog;
using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using OXAR.DataTransferObjects.CryoTank;
using OXAR.Services.CryoTank;

namespace OXAR.Controllers
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Cryo Tank reference-data API. Full CRUD (manager/admin gated
    /// for writes) so the lab can add/edit tanks at runtime.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [Authorize]
    [RoutePrefix("api/CryoTank")]
    public class CryoTankController : ApiController
    {
        private readonly ICryoTankService _service;
        static Logger logger = LogManager.GetCurrentClassLogger();

        private CryoTankController()
        {
            _service = new CryoTankService();
        }

        [HttpGet, Route("GetBySite")]
        public IHttpActionResult GetBySite(string siteId = null)
        {
            try { return Ok(_service.GetBySite(siteId)); }
            catch (Exception ex) { return Handle(ex, "GetBySite"); }
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
        public IHttpActionResult Post(CryoTankDTO dto)
        {
            try
            {
                if (dto == null) return BadRequest("Body is required.");
                if (!IsManager())
                    return Content(HttpStatusCode.Forbidden, "Only a manager/admin may add a tank.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Create(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Create"); }
        }

        [HttpPut, Route("Update")]
        public IHttpActionResult Put(CryoTankDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Id is required.");
                if (!IsManager())
                    return Content(HttpStatusCode.Forbidden, "Only a manager/admin may edit a tank.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Update(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Update"); }
        }

        [HttpDelete, Route("Delete")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                if (!IsManager())
                    return Content(HttpStatusCode.Forbidden, "Only a manager/admin may deactivate a tank.");
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
            logger.Error("Type: Error, Location: CryoTankController, Method: " + method + ", Error: " + inner);
            if (!string.IsNullOrEmpty(inner) && inner.StartsWith("CONFLICT", StringComparison.OrdinalIgnoreCase))
                return Content(HttpStatusCode.Conflict, inner);
            return BadRequest(inner);
        }
    }
}
