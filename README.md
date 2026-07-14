# OXARv1 — OX.ar Embryo Module (M-18)

Implementation package for the **OX.assisted reproduction (OX.ar)** Embryology Cycle Management
module: the embryology-lab workspace covering event planning, 3-point checks, egg collection,
semen prep, insemination/fertilisation, day-by-day embryo development, transfer, freeze/thaw,
biopsy, cryo storage, and double-witnessing.

> **Independent of OX.gp.** Nothing here shares code, entities, endpoints, assets, or wire
> contracts with OX.gp. It targets only the OX.ar repos (`OxArFrontendReact`, `OxArBackendReact`)
> and the OX.ar Dataverse organisation.

## What's here

```
docs/
  README.md               Master implementation guide (scope, best practice, phases, screens,
                          traceability M18-000..NEW, design system)
  01-DATA-MODEL.md        Dataverse entities — reuse bcrm_egg/bcrm_eggdetail + 15 new tables,
                          option sets, relationships
  02-API-GUIDE.md         Controller/Service/DTO scaffolding + endpoint contract + safety rules
  03-FRONTEND-GUIDE.md    React build — /lab routes, embryoApi.js, components, editing/admin UX
  04-CRUD-AND-EDITING.md  Full read-write: add/edit/update/delete, concurrency, audit, role gating,
                          reference-data management
  05-CODE-DROP.md         Visual Studio build/run steps for the backend code
  reference/              The self-contained UX prototype (synthetic data only)
  slides/                 "Idiot's Guide" implementation deck (+ generator)

backend/                  Working C# for the OXAR Web API (drop into OXAR WebApi.sln), preserving
                          project-relative paths:
  TaskService.DataTransferObjects/{EmbryoEvent,Opu,CryoTank}/*DTO.cs
  TaskService.Services/{EmbryoEvent,Opu,CryoTank}/*.cs      full CRUD services
  TaskService/Controllers/{EmbryoEvent,Opu,CryoTank}Controller.cs
```

## Start here
1. Read `docs/README.md` (master guide).
2. Create the Dataverse tables per `docs/01-DATA-MODEL.md`.
3. Drop `backend/` files into the `OXAR` solution (see `docs/05-CODE-DROP.md`) and build.
4. Wire the React screens per `docs/03-FRONTEND-GUIDE.md`.

## Best-practice grounding
Double/electronic witnessing (HFEA), oocyte maturity MII/MI/GV, PN/PB fertilisation scoring,
Gardner blastocyst grading (expansion + ICM + TE), ESHRE/ALPHA Istanbul consensus (2025)
terminology, and Vienna consensus (2017) lab KPIs. Citations in `docs/README.md` §2.

## Status
The C# was hand-verified against the `OxArBackendReact` conventions (it targets existing repo
types only); it is compiled in Visual Studio, not in this package. The backend code targets
Dataverse tables that must be created first (see `docs/01-DATA-MODEL.md`).
