# OX.ar Embryo Module — Editing, Adding & Managing Data (Full CRUD)

> **Independent of OX.gp.** OX.ar (`OXAR` Web API) only.

This doc makes the module **fully read-write**: every entity supports **Create (add)**,
**Read**, **Update (edit)**, and **Delete**, and the **reference/lookup data** that drives the
forms (templates, cryo tanks, sites, research projects, quality grades, etc.) is manageable too —
not hard-coded. It extends `02-API-GUIDE.md` (which showed the pattern on one entity) to a
consistent CRUD contract across the whole module, plus the concurrency, audit, soft-delete and
role rules that editing clinical data demands.

The repo **already proves the full pattern**: `PreviousProcedureService` has `Create` / `Update`
/ `Delete` and `PreviousProcedureController` exposes `POST /Create`, `PUT /Update`,
`DELETE /Delete`. We standardise that shape everywhere.

---

## 1. Standard CRUD contract (every entity gets all of these)

For entity `Xxx` (e.g. `Opu`, `SemenPrep`, `EmbryoTransfer`, `FreezeEvent`, `CryoTank`,
`EventTemplate`, …):

| Verb | Route | Service method | Purpose |
|---|---|---|---|
| GET | `GetBy<Parent>Id?id=` / `GetWorklist?…` | `GetBy…` → `List<ExpandoObject>` | list |
| GET | `GetById?id=` | `GetById` → `XxxDTO` | load one (for the edit form) |
| POST | `Create` | `Create(dto, userId)` → `Guid` | **add** a new record |
| PUT | `Update` | `Update(dto, userId)` → `bool` | **edit** an existing record |
| DELETE | `Delete?id=` | `Delete(guid)` → `bool` | remove / retire |

`GetById` is what the UI calls to populate an edit form; `Update` writes the changes back.
This is the missing half for entities that were read-only before (e.g. `bcrm_egg` via
`StoredSampleService` had only reads — add `Create`/`Update`/`Delete` so specimens can be edited).

### 1.1 Update = partial merge (send only what changed)
Dataverse `Update` merges the attributes present on the `Entity`. So an edit only needs to set the
columns the user changed (plus the `Id`). This gives you PATCH-like partial edits for free — the
same idiom `PreviousProcedureService.Update` uses:

```csharp
public bool Update(OpuDTO dto, string userId)
{
    Guard(dto);                                   // ACSHelper injection guard (as in the repo)
    var e = new Entity("bcrm_opu") { Id = Guid.Parse(dto.Id) };

    if (dto.ConcurrencyToken != null)             // optimistic concurrency — §3
        e.RowVersion = dto.ConcurrencyToken;

    if (dto.ProcedureDate.HasValue && dto.ProcedureDate != DateTime.MinValue)
        e["bcrm_proceduredate"] = DateTime.SpecifyKind(dto.ProcedureDate.Value, DateTimeKind.Utc);
    if (dto.EggsRetrieved.HasValue) e["bcrm_eggsretrieved"] = dto.EggsRetrieved.Value;
    if (dto.Mii.HasValue)           e["bcrm_mii"]           = dto.Mii.Value;
    if (dto.MiValue.HasValue)       e["bcrm_mi"]            = dto.MiValue.Value;
    if (dto.Gv.HasValue)            e["bcrm_gv"]            = dto.Gv.Value;
    if (!string.IsNullOrWhiteSpace(dto.Notes)) e["bcrm_notes"] = dto.Notes;
    if (!string.IsNullOrWhiteSpace(dto.WitnessId))
        e["bcrm_witness"] = new EntityReference("systemuser", new Guid(dto.WitnessId));

    _unitOfWork.CRMCoreRepository.Update(e);      // merges only the set attributes
    return true;
}
```

> To **clear** a field on edit (set it to null), you must set the attribute to `null`
> explicitly — the "only if populated" guards above never clear. Add an explicit
> `e["bcrm_notes"] = null;` branch when the DTO signals a cleared value (e.g. a
> `ClearedFields: string[]` array on the DTO), otherwise a blanked box is silently ignored.

### 1.2 Add (Create) — unchanged from `02-API-GUIDE.md` §2.3
`Create` builds a fresh `Entity`, sets attributes, returns the new `Guid`. "Add row to a subgrid"
in the UI = a `Create` with the parent lookup set.

---

## 2. Editing the day-by-day assessments (`bcrm_eggdetail`)

The development grid, PN board, and denuding screen all edit `bcrm_eggdetail` rows. Provide:

| Verb | Route (`Assessment` controller) | Purpose |
|---|---|---|
| POST | `CreateDenuding` / `CreateFertilisation` / `CreateDevelopment` | add an assessment row |
| PUT | `UpdateAssessment` | **edit** a cell/row (grade, frag, stage, fate, PN/PB, maturity) |
| DELETE | `DeleteAssessment?id=` | remove a mistaken row (manager, audited) |
| GET | `GetDetailsBySpecimen?id=` | load all rows for a specimen |

Inline grid editing (change `4AA` → `4AB`, or fate Continue → Freeze) is a single
`UpdateAssessment` call carrying `{ Id, field(s)changed, ConcurrencyToken }`. Keep the Gardner
grade as component fields (`bcrm_expansion`/`bcrm_icm`/`bcrm_te`) so a cell edit updates one part
cleanly (see `01-DATA-MODEL.md` §4.11).

---

## 3. Optimistic concurrency (two embryologists, one cycle)

Editing clinical data means guarding against silent overwrites. Two safe options:

- **Preferred — Dataverse RowVersion.** Surface the row's `RowVersion` (base64 `@odata.etag`) on
  the DTO as `ConcurrencyToken`. On `Update`, set `entity.RowVersion = token` and
  `entity.EntityState`/request `ConcurrencyBehavior = IfRowVersionMatches`. A stale token throws a
  `ConcurrencyException` → the controller returns **409 Conflict**; the UI reloads and asks the
  user to redo the edit.
- **Lightweight — `modifiedon` check.** Return `ModifiedOn` on read; the service re-reads before
  write and rejects (throw → `BadRequest`) if `modifiedon` moved. Simpler, slightly racy; fine for
  low-contention screens.

Expose the token/`modifiedon` on every editable DTO and echo it back on `Update`. The services
already read `modifiedon`/`modifiedby`, so surfacing "last edited by X at Y" in the UI is free.

---

## 4. Delete vs retire (regulated data — be careful)

ART records may be reportable to the regulator, so **destructive delete is the exception**:

- **Soft-retire (default).** For anything clinically meaningful (events, assessments, specimens),
  "delete" = set `statecode` Inactive (+ a `bcrm_cancelreason`), not a hard `Delete`. Keeps the
  audit chain. Provide `PUT /Cancel?id=&reason=`.
- **Hard delete (privileged).** Only for genuine data-entry mistakes, gated to **team-manager**
  role (M18-038), always writing an audit note first. Uses `CRMCoreRepository.Delete`.
- **Correct-and-re-enter (M18-037/038).** The manager flow = retire the wrong row (with reason) +
  create the corrected one, both audited, so the history shows what happened. Prefer this over an
  in-place edit for values already used downstream (e.g. a fertilisation result a transfer relied
  on).

Turn on **Dataverse auditing** for every module table so field-level edit history is captured
automatically — this is your regulatory edit trail.

---

## 5. Role gating (who may add / edit / delete)

Read the caller's role from identity (the HMAC/Bearer handler that sets
`HttpContext.Current.Items["ContactId"]` — extend it to also expose the staff role, or look the
role up in the service). Enforce in the service so the API is authoritative:

| Capability | Allowed role |
|---|---|
| Read (all screens) | any authenticated lab user |
| Add / edit (Create, Update) | embryologist, andrologist |
| Cancel / soft-retire | senior embryologist |
| Hard delete, correct-and-re-enter, edit-after-lock | team manager (M18-037/038) |
| Manage reference data (§6) | team manager / lab admin |

Return **403** (not 400) when the role check fails, so the UI can distinguish "not allowed" from
"bad input".

---

## 6. Managing the underlying reference / lookup data ("data sources")

The forms are driven by lookup data. Decide per list whether values change at **runtime**
(business adds them without a developer) or only at **schema time**:

| Data | Model as | Editable at runtime? | How to manage |
|---|---|---|---|
| Event types, statuses, maturity, PN/PB, fate, disposition | **global option set** | No (schema) | via the `OXAR_EmbryoModule` solution — versioned, deployed |
| Event templates + details | **entity** `bcrm_eventtemplate(detail)` | **Yes** | `EventTemplate` CRUD + a Templates admin screen |
| Cryo tanks & locations | **entity** `bcrm_cryotank` / `bcrm_cryolocation` | **Yes** | `CryoTank` CRUD + a Tank admin screen |
| Research projects | **entity** `bcrm_researchproject` | **Yes** | `ResearchProject` CRUD |
| Lab sites, quality grades, biopsy types | **entity** (reference table) *if runtime-editable* | **Yes** | small CRUD controllers |

**Decision rule:** if a lab manager will realistically need to add a value themselves (a new
storage tank, a new event template, a new research project, a new quality grade), model it as a
**reference entity with full CRUD** — *not* a Dataverse option set, because option-set values are
schema and require a solution deployment to change. Everything genuinely fixed and clinical
(maturity MII/MI/GV, PN 0–3, fate list) stays an option set.

Reference-data controllers follow the exact same CRUD contract (§1). Example — templates:

| Verb | Route (`EventTemplate`) | Purpose |
|---|---|---|
| GET | `GetActive` / `GetById?id=` | list / load |
| POST | `Create` | add a template |
| PUT | `Update` | edit template header |
| DELETE | `Delete?id=` (or `Deactivate`) | retire |
| POST | `AddDetail` / PUT `UpdateDetail` / DELETE `DeleteDetail?id=` | manage the child event rows |
| POST | `ApplyToCycle` | instantiate → `bcrm_embryoevent` rows on a cycle |

Give `bcrm_cryotank` and `bcrm_researchproject` the same set. Now a manager can add a tank, edit
its capacity, spin up a new template, or register a research project entirely from the UI.

---

## 7. Multi-entity (transactional) edits

Some actions touch several tables and must stay consistent:

- **Freeze** = create `bcrm_freezeevent` → update the `bcrm_egg` (frozen, freeze date, location) →
  set `bcrm_cryolocation` Occupied.
- **Thaw** = create `bcrm_thawevent` → update `bcrm_egg` (Thawed) → set `bcrm_cryolocation` Available.
- **Apply template** = create N `bcrm_embryoevent` rows.

Wrap these in a Dataverse **`ExecuteTransactionRequest`** (all-or-nothing) inside the service, so a
half-applied edit can't leave a specimen frozen with no location. If you can't batch, do them in
dependency order and log each step (the repo already logs start/complete per method) so a partial
failure is diagnosable. Add an `ExecuteTransaction` helper on `CRMCoreRepository` if not present.

---

## 8. Checklist for making an entity fully editable

1. Service has `GetById`, `Create`, `Update`, `Delete`/`Cancel`.
2. DTO carries `Id` + `ConcurrencyToken` (RowVersion) + `ModifiedBy`/`ModifiedOn`.
3. Controller exposes GET/POST/PUT/DELETE with role checks (403 on deny).
4. `Update` supports clearing fields (explicit null), not just setting.
5. Destructive delete is manager-gated + audited; default is soft-retire with a reason.
6. Concurrency returns 409 on stale edits; UI reloads.
7. Reference data this entity depends on is itself CRUD-managed if runtime-editable (§6).
8. Multi-entity writes are transactional (§7).

Frontend editing/add/delete UX is in
`../../../OxArFrontendReact/docs/embryo-module/03-FRONTEND-GUIDE.md` §"Editing, adding & deleting".
