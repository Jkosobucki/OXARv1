# OX.ar Embryo Module — API, Services & Scaffolding Guide (Backend)

> **Independent of OX.gp.** OX.ar (`OXAR` Web API) only. No OX.gp code, endpoints, or contracts.

This guide shows **exactly how to add each Embryo Module endpoint** to `OxArBackendReact`, using
the repo's own conventions (verified against `StoredSampleController` / `StoredSampleService` and
`PreviousProcedureController` / `PreviousProcedureService`). Follow the data model in
`01-DATA-MODEL.md` first.

> **Every entity is fully read-write.** The tables below show the read + primary write routes;
> the *complete* Create / **Update (edit)** / Delete contract, plus optimistic concurrency,
> audit, soft-delete, role gating, and **managing the reference/lookup data** (templates, tanks,
> sites, research projects), lives in **`04-CRUD-AND-EDITING.md`**. Read it alongside this doc —
> anywhere a table lists only `Create`, assume the full `GetById`/`Update`/`Delete` set exists too.

---

## 1. The layer pattern (what every feature needs)

For each entity you add **four things**, in these projects:

| Layer | Project / folder | File(s) |
|---|---|---|
| DTO | `TaskService.DataTransferObjects/<Area>/` | `XxxDTO.cs` |
| Service interface + impl | `TaskService.Services/<Area>/` | `IXxxService.cs`, `XxxService.cs` |
| Controller | `TaskService/Controllers/` | `XxxController.cs` |
| (Repository) | `TaskServiceRepository/CRMCore/` | **no change** — reuse `ICRMCoreRepository` |

**No DI container to touch.** `WebApiConfig.cs` uses `config.MapHttpAttributeRoutes()` and
controllers `new` their service directly (e.g. `_service = new StoredSampleService();`). Adding a
controller with `[RoutePrefix]` is enough for it to be routed. There is no Unity/Autofac
registration step.

**Auth & identity.** Every controller carries `[Authorize]` and
`[EnableCors(origins:"*", headers:"*", methods:"*")]`. The signed-in contact id is available as
`HttpContext.Current.Items["ContactId"]` (set by the HMAC/Bearer message handler) — read it the
same way `PreviousProcedureController` does, rather than trusting a client-supplied id. The
frontend attaches the `hmacauth …` token via the existing `AllApiCall.GettheHmToken()` seam.

**Logging.** Use NLog with the repo's exact string shape:
`"Type: Info, Location: <Class>, Method: <Method>, Action: Starting.."`.

**Injection guard.** Reuse `ACSHelper.ContainsSpecialCharacters(...)` on write payloads exactly
as `PreviousProcedureService.Create` does.

---

## 2. Reference templates (copy, rename, fill columns)

### 2.1 DTO — `TaskService.DataTransferObjects/EmbryoEvent/EmbryoEventDTO.cs`
```csharp
using System;

namespace OXAR.DataTransferObjects.EmbryoEvent
{
    public class EmbryoEventDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string EventType { get; set; }          // FormattedValue on read; option label on write
        public int?   EventTypeValue { get; set; }      // integer optionset value on write
        public string Status { get; set; }
        public int?   StatusValue { get; set; }
        public string Priority { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public string PatientId { get; set; }           // contact guid
        public string PatientName { get; set; }
        public string PartnerId { get; set; }
        public string TreatmentCycleId { get; set; }
        public string TreatmentCycleName { get; set; }
        public string EmbryologistId { get; set; }
        public string WitnessId { get; set; }
        public string MaterialType { get; set; }
        public string MaterialRecordId { get; set; }    // bcrm_egg guid
        public string Outcome { get; set; }
        public string OutcomeNotes { get; set; }
        public string Comments { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
```
Follow the **existing `StoredSampleDTO`** shape: `string`/`DateTime?` properties, `Id` as string.
Lookups are surfaced as `XxxId` (guid) + `XxxName` (the `EntityReference.Name`).

### 2.2 Service interface — `TaskService.Services/EmbryoEvent/IEmbryoEventService.cs`
```csharp
using System;
using System.Collections.Generic;
using OXAR.DataTransferObjects.EmbryoEvent;

namespace OXAR.Services.EmbryoEvent
{
    public interface IEmbryoEventService
    {
        object GetByCycleId(string cycleId);
        object GetWorklist(string siteId, DateTime from, DateTime to, string status, string eventType);
        EmbryoEventDTO GetById(string id);
        Guid   Create(EmbryoEventDTO dto, string userId);
        bool   Update(EmbryoEventDTO dto, string userId);
        bool   Delete(Guid id);   // team-manager only (M18-037/038)
    }
}
```

### 2.3 Service impl — `TaskService.Services/EmbryoEvent/EmbryoEventService.cs`
Mirror `StoredSampleService` for **reads** and `PreviousProcedureService` for **writes**.

```csharp
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OXAR.DataTransferObjects.EmbryoEvent;
using OXAR.Repository.UnitOfWork;

namespace OXAR.Services.EmbryoEvent
{
    public class EmbryoEventService : IEmbryoEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public EmbryoEventService() { _unitOfWork = new UnitOfWork(); }
        public EmbryoEventService(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

        // ---------- READ (FetchXML → ExpandoObject list) ----------
        public object GetByCycleId(string cycleId)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetByCycleId, Action: Starting..");
                dynamic result = new List<ExpandoObject>();
                var fetchXML = string.Format(@"
                  <fetch version='1.0' mapping='logical' distinct='false'>
                    <entity name='bcrm_embryoevent'>
                      <attribute name='bcrm_embryoeventid' />
                      <attribute name='bcrm_name' />
                      <attribute name='bcrm_eventtype' />
                      <attribute name='bcrm_status' />
                      <attribute name='bcrm_scheduleddate' />
                      <attribute name='bcrm_patient' />
                      <attribute name='bcrm_materialrecord' />
                      <order attribute='bcrm_scheduleddate' descending='false' />
                      <filter type='and'>
                        <condition attribute='bcrm_treatment_cycle' operator='eq' value='{0}' />
                      </filter>
                    </entity>
                  </fetch>", cycleId);

                EntityCollection rows = _unitOfWork.CRMCoreRepository.Get(fetchXML);
                if (rows?.Entities?.Count > 0)
                {
                    foreach (var e in rows.Entities)
                    {
                        dynamic a = new ExpandoObject();
                        a.Id = e.Id.ToString();
                        a.Name = e.Attributes.Contains("bcrm_name") ? e["bcrm_name"].ToString() : string.Empty;
                        a.EventType = e.Attributes.Contains("bcrm_eventtype") ? e.FormattedValues["bcrm_eventtype"].ToString() : string.Empty;
                        a.Status = e.Attributes.Contains("bcrm_status") ? e.FormattedValues["bcrm_status"].ToString() : string.Empty;
                        a.ScheduledDate = e.Attributes.Contains("bcrm_scheduleddate") ? Convert.ToDateTime(e["bcrm_scheduleddate"]) : DateTime.MinValue;
                        a.PatientName = e.Attributes.Contains("bcrm_patient") ? ((EntityReference)e["bcrm_patient"]).Name : string.Empty;
                        a.MaterialRecordId = e.Attributes.Contains("bcrm_materialrecord") ? ((EntityReference)e["bcrm_materialrecord"]).Id.ToString() : string.Empty;
                        result.Add(a);
                    }
                }
                logger.Info("Type: Info, Location: EmbryoEventService, Method: GetByCycleId, Action: Completed.");
                return result;
            }
            catch (Exception ex) { logger.Error("EmbryoEventService.GetByCycleId: " + ex.Message); throw; }
        }

        // ---------- CREATE (Entity → Repository.Create) ----------
        public Guid Create(EmbryoEventDTO dto, string userId)
        {
            try
            {
                logger.Info("Type: Info, Location: EmbryoEventService, Method: Create, Action: Starting..");

                // Injection guard — same as PreviousProcedureService
                JObject obj = JObject.Parse(JsonConvert.SerializeObject(dto));
                foreach (var p in obj.Properties())
                    if (new ACSHelper().ContainsSpecialCharacters(p.Value.ToString()))
                        throw new Exception("Invalid json");

                var entity = new Entity("bcrm_embryoevent");
                if (!string.IsNullOrWhiteSpace(dto.Name)) entity["bcrm_name"] = dto.Name;
                if (dto.EventTypeValue.HasValue)  entity["bcrm_eventtype"] = new OptionSetValue(dto.EventTypeValue.Value);
                if (dto.StatusValue.HasValue)     entity["bcrm_status"]    = new OptionSetValue(dto.StatusValue.Value);
                if (dto.ScheduledDate.HasValue && dto.ScheduledDate != DateTime.MinValue)
                    entity["bcrm_scheduleddate"] = DateTime.SpecifyKind(dto.ScheduledDate.Value, DateTimeKind.Utc);
                if (!string.IsNullOrWhiteSpace(dto.TreatmentCycleId))
                    entity["bcrm_treatment_cycle"] = new EntityReference("bcrm_treatment_cycle", new Guid(dto.TreatmentCycleId));
                if (!string.IsNullOrWhiteSpace(dto.PatientId))
                    entity["bcrm_patient"] = new EntityReference("contact", new Guid(dto.PatientId));
                if (!string.IsNullOrWhiteSpace(dto.MaterialRecordId))
                    entity["bcrm_materialrecord"] = new EntityReference("bcrm_egg", new Guid(dto.MaterialRecordId));
                if (!string.IsNullOrWhiteSpace(dto.WitnessId))
                    entity["bcrm_witness"] = new EntityReference("systemuser", new Guid(dto.WitnessId));

                logger.Info("Type: Info, Location: EmbryoEventService, Method: Create, Action: Completed.");
                return _unitOfWork.CRMCoreRepository.Create(entity);
            }
            catch (Exception ex) { logger.Error("EmbryoEventService.Create: " + ex.Message); throw; }
        }

        public bool Update(EmbryoEventDTO dto, string userId) { /* same shape, Entity with Id set, Repository.Update */ return true; }
        public bool Delete(Guid id)
        {
            _unitOfWork.CRMCoreRepository.Delete(new Entity("bcrm_embryoevent", id));
            return true;
        }
        public EmbryoEventDTO GetById(string id) { /* single-row FetchXML → DTO, cf. StoredSampleService.GetById */ return null; }
        public object GetWorklist(string siteId, DateTime from, DateTime to, string status, string eventType) { /* date-range + optional filters */ return null; }
    }
}
```

**Write-path idioms (from `PreviousProcedureService`) — do not deviate:**
- Option set → `new OptionSetValue(int)`.
- Lookup → `new EntityReference("<logicalname>", guid)`.
- Dates → `DateTime.SpecifyKind(value, DateTimeKind.Utc)` (Dataverse stores UTC).
- Two-option → assign the `bool` directly.
- Money → `new Money(decimal)` (not needed here, but that's the idiom).
- Always run the `ACSHelper.ContainsSpecialCharacters` guard on writes.

### 2.4 Controller — `TaskService/Controllers/EmbryoEventController.cs`
```csharp
using System;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using NLog;
using OXAR.DataTransferObjects.EmbryoEvent;
using OXAR.Services.EmbryoEvent;

namespace OXAR.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [Authorize]
    [RoutePrefix("api/EmbryoEvent")]
    public class EmbryoEventController : ApiController
    {
        private readonly IEmbryoEventService _service;
        static Logger logger = LogManager.GetCurrentClassLogger();
        private EmbryoEventController() { _service = new EmbryoEventService(); }

        [HttpGet, Route("GetByCycleId")]
        public IHttpActionResult GetByCycleId(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _))
                    return BadRequest("id must be a valid guid.");
                return Ok(_service.GetByCycleId(id));
            }
            catch (Exception ex) { logger.Error("EmbryoEventController.GetByCycleId: " + ex.Message); return BadRequest(ex.Message); }
        }

        [HttpPost, Route("Create")]
        public IHttpActionResult Post(EmbryoEventDTO dto)
        {
            try
            {
                var userId = HttpContext.Current.Items["ContactId"] as string;   // identity from handler
                return Ok(_service.Create(dto, userId));
            }
            catch (Exception ex) { logger.Error("EmbryoEventController.Post: " + ex.Message); return BadRequest(ex.Message); }
        }

        [HttpPut,  Route("Update")] public IHttpActionResult Put(EmbryoEventDTO dto)  { var u = HttpContext.Current.Items["ContactId"] as string; return Ok(_service.Update(dto, u)); }
        [HttpDelete, Route("Delete")] public IHttpActionResult Delete(Guid id)        { return Ok(_service.Delete(id)); }
    }
}
```

Add the three new `.cs` files to their `.csproj` `<Compile Include>` lists (Visual Studio does
this automatically; if you scaffold by hand, edit the `.csproj`).

---

## 3. Endpoint contract (what the React app will call)

Follow the existing naming (`GetByContactId`, `GetById`, `Create`, `Update`, `Delete`). Base URL
is `REACT_APP_OXAR_API_URL` (`https://oxarportalapireact.azurewebsites.net/`).

| Controller | Route | Verb | Purpose | Maps to req |
|---|---|---|---|---|
| `EmbryoEvent` | `GetWorklist?siteId=&from=&to=&status=&eventType=` | GET | Worklist / outstanding events / diary feed | M18-002/003/039 |
| `EmbryoEvent` | `GetByCycleId?id=` | GET | Cycle overview timeline | M18-001/009 |
| `EmbryoEvent` | `Create` / `Update` / `Delete` | POST/PUT/DELETE | Schedule/record events | M18-007/008/037/038 |
| `EventTemplate` | `GetActive` / `GetDetails?id=` | GET | Pick + expand a template | M18-007 |
| `EventTemplate` | `ApplyToCycle` | POST | Instantiate template → events for a cycle | M18-007/008 |
| `ThreePointCheck` | `GetByEventId?id=` / `Create` | GET/POST | Safety gate (blocks step) | M18-012 |
| `ConsentChecklist` | `GetByCycleId?id=` / `Upsert` | GET/POST | Prerequisites gate | M18-011 |
| `Opu` | `GetByCycleId?id=` / `Create` / `Update` | GET/POST/PUT | Egg collection + MII/MI/GV | M18-014 |
| `SemenPrep` | `GetByCycleId?id=` / `Create` / `Update` | GET/POST/PUT | Semen prep/thaw/quality | M18-016/017/018/019 |
| `Insemination` | `GetByCycleId?id=` / `Create` | GET/POST | IVF/ICSI event | M18-022 |
| `Specimen` (`bcrm_egg`) | `GetGametesByCycle?id=` | GET | Gametes in a cycle | M18-009 |
| `Specimen` | `GetEmbryosByCycle?id=` | GET | Embryos in a cycle | M18-009/010 |
| `Specimen` | `GetDetailsBySpecimen?id=` | GET | Per-day assessment rows | M18-010/025 |
| `Assessment` (`bcrm_eggdetail`) | `CreateDenuding` | POST | Maturity (MII/MI/GV) | M18-020 |
| `Assessment` | `CreateFertilisation` | POST | PN/PB, 1st & 2nd check | M18-023/024 |
| `Assessment` | `CreateDevelopment` | POST | Day 2–7 grade/frag/stage/fate | M18-025/026 |
| `EmbryoTransfer` | `Create` / `GetByCycleId?id=` | POST/GET | Transfer procedure | M18-027/035 |
| `FreezeEvent` | `Create` / `AllocateLocation` | POST | Freeze + storage allocation | M18-028/031 |
| `ThawEvent` | `Create` / `GetByCycleId?id=` | POST/GET | Thaw + survival | M18-015 |
| `Biopsy` | `Create` / `MarkReBiopsy` | POST | Biopsy / re-biopsy | M18-029/030 |
| `CryoTank` | `GetBySite?siteId=` / `GetById?id=` | GET | Graphical tank view | M18-004 |
| `CryoTank` | `GetInventory?tankId=` | GET | Patients/materials in tank (derived over `bcrm_egg`) | M18-005 |
| `Specimen` | `MarkDonated` | POST | Donate egg/embryo to recipient | M18-036 |
| `LabelPrint` | `Create` / `GetByCycle?id=` | POST/GET | Label print log | M18-006 |
| `ResearchProject` | `GetActive` / `EnrolCycle` | GET/POST | Research banner + enrolment | M18-NEW |
| `Report` | `CycleOutcome?id=` | GET | Outcome report/letter data | M18-032/033/034/040 |

---

## 4. Business rules to enforce **server-side** (not just in the UI)

These are patient-safety rules — the API is the last line of defence and must enforce them even
if the UI is bypassed:

1. **Independent witness.** On `ThreePointCheck`, `FreezeEvent`, `ThawEvent`, `Insemination`,
   `EmbryoTransfer`: reject if `WitnessId == EmbryologistId/VerifiedById`. Two-person integrity
   (HFEA). Reject a critical procedure event moving to `In Progress` unless a **Passed**
   `bcrm_threepointcheck` exists for it.
2. **Consent gate.** Refuse to create OPU/insemination/transfer events for a cycle whose
   `bcrm_consentchecklist.ReadyForProcedure` is not true (M18-011).
3. **ICSI eligibility.** In `CreateDenuding`, set `SuitableForICSI = (Maturity == MII)`. Only MII
   oocytes may be referenced by an ICSI insemination.
4. **Fertilisation normality.** In `CreateFertilisation`, flag anything other than `2PN` as
   abnormal; abnormal zygotes should not silently advance to transfer.
5. **Carry-over rule (M18-026).** In `CreateDevelopment`, if `Fate != Freeze/Transfer/Discard/
   Donate/Biopsy`, the specimen status stays `Under Culture` and a next-day assessment is expected.
6. **Freeze ⇒ location.** `FreezeEvent.Create` must set a `bcrm_cryolocation` and flip it to
   Occupied; update the `bcrm_egg` cryo columns transactionally (create event, then update
   specimen, then update location — log each).
7. **Thaw ⇒ free location + survival.** `ThawEvent` frees the location (→ Available) and records
   `QuantitySurvived ≤ QuantityThawed`.
8. **Delete is privileged.** `Delete` on any event/assessment is a team-manager action
   (M18-037/038) — gate on role, write an audit note, and prefer soft-cancel (`statecode`) over
   hard delete where possibly reportable to the regulator.

Put these in the service layer (throw a descriptive `Exception` — the controller's catch turns it
into `BadRequest(message)`, matching the existing pattern).

---

## 5. KPI computation (Vienna consensus) — server-side

The cycle-overview KPIs (fertilisation rate, usable-blastocyst rate, etc.) should be computed in a
`Report`/`Kpi` service from the specimen + assessment rows, **not** stored denormalised:

- **Fertilisation rate** = 2PN zygotes ÷ (MII oocytes inseminated by ICSI, or COCs by IVF).
- **Blastocyst development rate** = blastocysts on Day 5 ÷ 2PN zygotes (Vienna: competence 25–60%,
  benchmark 44–80%).
- **Usable blastocyst rate**, **cleavage rate**, **Day-5 transfer rate** likewise.

Return these with their benchmark bands so the UI can show green/amber/red against Vienna targets.
See `../../../OxArFrontendReact/docs/embryo-module/README.md` for the clinical references.

---

## 6. Testing & verification

- **Swagger** is enabled (`config.EnableSwagger` in `WebApiConfig.cs`) — smoke-test each new route
  there first.
- Reads: assert `FormattedValues` are populated for every option-set/lookup column you surface.
- Writes: after `Create`, immediately `GetById` and confirm option-set/lookup round-trips.
- Witness/consent rules: add negative tests (same witness=performer → 400; procedure without
  passed 3-point check → 400).
- Confirm UTC handling: a date sent as local must persist as the same instant (`SpecifyKind`).

Nothing here depends on OX.gp. The only external systems are the OX.ar Dataverse org and the
existing HMAC auth handler already in this repo.
