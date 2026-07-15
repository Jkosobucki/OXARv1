using NLog;
using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using OXAR.DataTransferObjects.Opu;
using OXAR.Services.Opu;

namespace OXAR.Controllers
{
    /// <summary>
    /// OX.ar Embryo Module (M-18) — Egg Collection (OPU) API. Full CRUD.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [Authorize]
    [RoutePrefix("api/Opu")]
    public class OpuController : ApiController
    {
        private readonly IOpuService _service;
        static Logger logger = LogManager.GetCurrentClassLogger();

        private OpuController()
        {
            _service = new OpuService();
        }

        [HttpGet, Route("GetByCycleId")]
        public IHttpActionResult GetByCycleId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _))
                    return BadRequest("id must be a valid guid.");
                return Ok(_service.GetByCycleId(id));
            }
            catch (Exception ex) { return Handle(ex, "GetByCycleId"); }
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
        public IHttpActionResult Post(OpuDTO dto)
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
        public IHttpActionResult Put(OpuDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Id is required.");
                var userId = HttpContext.Current.Items["ContactId"] as string;
                return Ok(_service.Update(dto, userId));
            }
            catch (Exception ex) { return Handle(ex, "Update"); }
        }

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

        [HttpDelete, Route("Delete")]
        public IHttpActionResult Delete(Guid id)
        {
            try
            {
                if (!IsManager())
                    return Content(HttpStatusCode.Forbidden, "Only a team manager may delete an OPU record.");
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
            logger.Error("Type: Error, Location: OpuController, Method: " + method + ", Error: " + inner);
            if (!string.IsNullOrEmpty(inner) && inner.StartsWith("CONFLICT", StringComparison.OrdinalIgnoreCase))
                return Content(HttpStatusCode.Conflict, inner);
            return BadRequest(inner);
        }
    }
}
