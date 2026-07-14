# OX.ar Embryo Module — Code Drop (Visual Studio)

> **Independent of OX.gp.** OX.ar `OXAR` solution only.

Working C# for three representative entities has been added to the solution, following the repo's
exact conventions and the CRUD contract in `04-CRUD-AND-EDITING.md`. Use these as the template to
clone the remaining entities.

## Files added

**DTOs — project `OXAR.DataTransferObjects`**
- `EmbryoEvent/EmbryoEventDTO.cs`
- `Opu/OpuDTO.cs`
- `CryoTank/CryoTankDTO.cs`

**Services — project `OXAR.Services`**
- `EmbryoEvent/IEmbryoEventService.cs` + `EmbryoEventService.cs`
- `Opu/IOpuService.cs` + `OpuService.cs`
- `CryoTank/ICryoTankService.cs` + `CryoTankService.cs`

**Controllers — project `OXAR` (Web API)**
- `Controllers/EmbryoEventController.cs`  → `api/EmbryoEvent/*`
- `Controllers/OpuController.cs`          → `api/Opu/*`
- `Controllers/CryoTankController.cs`     → `api/CryoTank/*`

All three `.csproj` files were updated with the matching `<Compile Include>` entries (these are
classic, non-SDK projects, so files must be listed explicitly).

## What each one shows
- **EmbryoEvent** — the full pattern: list (`GetByCycleId`), `GetWorklist` (date range + filters),
  `GetById`, `Create`, `Update` (partial merge), `Cancel` (soft-retire + reason), `Delete`
  (manager-gated). Includes the **independent-witness rule** and **optimistic concurrency** (409).
- **Opu** — a clinical procedure with extra validation (MII+MI+GV ≤ eggs retrieved) + witness rule.
- **CryoTank** — runtime-editable **reference data** with writes gated to manager/admin.

## Before it runs — create the Dataverse tables
These services target tables that must exist first (see `01-DATA-MODEL.md`):
`bcrm_embryoevent`, `bcrm_opu`, `bcrm_cryotank` (+ their columns/option sets, and the referenced
`bcrm_treatment_cycle`, `bcrm_egg`, `bcrm_site`). Create them in the `OXAR_EmbryoModule`
unmanaged solution, then build.

## Steps in Visual Studio
1. Pull the branch `claude/oxar-embryo-module-prototype-tks4fi` (once pushed) or copy the files in.
2. Open `OXAR WebApi.sln`. The new files appear under each project (already in the `.csproj`).
3. Ensure NuGet restore brings in `Microsoft.CrmSdk.CoreAssemblies` / `Microsoft.Xrm.Sdk`
   (already referenced by the existing projects) and `Newtonsoft.Json`, `NLog`.
4. **Build** the solution (Debug). Fix nothing — it targets existing repo types only.
5. Run; open **Swagger** (enabled in `WebApiConfig.cs`) and smoke-test:
   - `GET  api/CryoTank/GetBySite`
   - `POST api/Opu/Create`  (body = `OpuDTO`) then `GET api/Opu/GetById?id=…`
   - `PUT  api/EmbryoEvent/Update` with a stale `ModifiedOn` → expect **409 Conflict**
   - `POST` with `WitnessId == EmbryologistId` → expect **400** (witness rule)
6. Wire the React side per `../../../OxArFrontendReact/docs/embryo-module/03-FRONTEND-GUIDE.md`.

## Notes / conventions honoured
- Reads = FetchXML via `CRMCoreRepository.Get`; writes = `Entity` + `Create/Update/Delete`.
- Injection guard `ACSHelper.ContainsSpecialCharacters` on every write (as in `PreviousProcedureService`).
- NLog messages match the house format.
- Identity from `HttpContext.Current.Items["ContactId"]`; role gate reads `X-User-Role`
  (swap for the real role source once available — search for `IsManager`).
- Concurrency uses a `modifiedon` re-read (lightweight). Upgrade to Dataverse `RowVersion` +
  `ConcurrencyBehavior.IfRowVersionMatches` if you add an `ExecuteRequest` seam to the repository
  (`04-CRUD-AND-EDITING.md` §3/§7).

## To replicate for the other entities
Copy one triple (DTO + I/Service + Controller), rename `Opu`→`SemenPrep` (etc.), swap the table +
column names from `01-DATA-MODEL.md`, add the three `<Compile Include>` lines. That's the whole loop.
