# OX.ar Embryo Module — Implementation Guide (Master)

**Module:** M-18 Embryology Cycle Management
**Solution:** OX.assisted reproduction (OX.ar)
**Repos:** `OxArFrontendReact` (React 17 SPA) · `OxArBackendReact` (`OXAR` ASP.NET Web API + Dataverse)
**Status:** Planning / build guide · **Author aid:** grounded in the uploaded `Embryo_Module.xlsx`
spec + `embryo_module_prototype.html`

---

> ## ⛔ Independence from OX.gp
> This module, and every document in this folder, belongs to **OX.ar only**. It shares **no code,
> entities, endpoints, config, branding assets, or wire contracts** with OX.gp. Do not import from,
> copy from, or link to any OX.gp repository. If any task appears to require touching OX.gp,
> **stop** — it is out of scope by explicit instruction. The only systems this module touches are
> the two OX.ar repos above and the OX.ar Dataverse organisation.

---

## 0. How to read this guide

| Doc | Repo | What it covers |
|---|---|---|
| **README.md** (this) | frontend | Scope, best-practice foundations, architecture, phased roadmap, screen inventory, design system, requirement traceability, acceptance |
| `03-FRONTEND-GUIDE.md` | frontend | Step-by-step React build: routing, services, page-by-page component recipes, design tokens |
| `01-DATA-MODEL.md` | backend | Dataverse entity dictionary — reuse `bcrm_egg`/`bcrm_eggdetail`, 15 new tables, option sets, relationships |
| `02-API-GUIDE.md` | backend | Controller/service/DTO scaffolding templates + endpoint contract + server-side safety rules |
| `04-CRUD-AND-EDITING.md` | backend | **Full read-write:** Create/edit/update/delete per entity, optimistic concurrency, audit, soft-delete, role gating, and managing the reference/lookup data |

Build order across a feature slice is always **Dataverse schema → API → React service → React
page → witness/consent gates → verify**. Never start a screen before its entity + endpoint exist.

---

## 1. What we are building

The Embryo Module is the **embryology laboratory workspace** for OX.ar: the tool embryologists and
clinicians use to plan a treatment cycle's lab events, run and record each procedure (egg
collection, semen prep, insemination, fertilisation and daily embryo assessment, transfer,
freeze/thaw, biopsy), manage cryo storage, and keep the double-witnessing / chain-of-custody
record an accredited ART lab is legally required to maintain.

The uploaded spec defines **41 requirements (M18-000 … M18-040 + one NEW)** across **23 data
sheets**. The uploaded prototype expresses the target UX. This guide turns both into an
implementable plan against the *actual* OX.ar codebase.

### Key finding that shapes everything
The OX.ar Dataverse org **already contains** the two hardest tables:
- **`bcrm_egg`** — the specimen/material master (egg, sperm, embryo, straw) with full cryo
  attributes (tank, goblet, straw, freeze/thaw dates, consent, location).
- **`bcrm_eggdetail`** — per-specimen, per-day assessment detail (maturity, PN/PB, cell count,
  fragmentation, ICM, TE, blastocyst, morula, embryo fate, biopsy, insemination type).

So the plan is **reuse these two + add the 15 workflow/event/storage tables that don't exist**,
rather than build 23 tables from scratch. This preserves one specimen identity per egg/embryo for
its whole life — a hard requirement for witnessing and traceability. Full mapping: `01-DATA-MODEL.md`.

---

## 2. Best-practice foundations (why the design is what it is)

The prototype and this plan are deliberately grounded in current ART-lab clinical standards, not
just the spreadsheet. These are the standards every screen and rule traces back to:

- **Double witnessing / electronic witnessing (chain of custody).** Every *critical* step —
  egg collection, sperm collection, sperm prep, insemination, fertilisation check, embryo
  culture, transfer, freeze, thaw, discard, donation — must be witnessed by an independent second
  person and/or a certified electronic witnessing system (EWS), after a documented risk
  assessment. This is the defining safety ritual of a licensed lab (UK: **HFEA Code of Practice**).
  → In the module: the **3-point check blocks a procedure** until an independent witness confirms;
  a persistent **witness chip** and the **witnessing log** keep the record; the API rejects a
  procedure whose witness equals its performer (`02-API-GUIDE.md` §4).
- **Oocyte maturity: MII / MI / GV.** Only **MII** (metaphase-II, mature) oocytes are
  ICSI-eligible. The OPU screen splits retrieved oocytes into MII/MI/GV; the denuding assessment
  and ICSI eligibility flow from it.
- **Fertilisation: PN / PB scoring** at the ~16–18 h check. **2PN = normal**; 0PN/1PN/3PN are
  flagged abnormal. A PN board is the fertilisation view.
- **Embryo grading — component parts, not a blob.** Cleavage stage: cell count + fragmentation %
  + symmetry. Blastocyst: **Gardner** expansion (1–6) + ICM (A/B/C) + TE (A/B/C), so `4AA` reads
  and sorts correctly. Store the parts separately (see `01-DATA-MODEL.md` §4.11).
- **Developmental staging & terminology** follow the **ESHRE/ALPHA Istanbul consensus update
  (2025)** — the current international standard for static and time-lapse (dynamic) morphological
  assessment of oocytes, zygotes and embryos.
- **Lab KPIs framed against the Vienna consensus (2017).** e.g. **blastocyst development rate**
  (blastocysts Day 5 ÷ 2PN zygotes): competence band **25–60%**, benchmark **44–80%**; plus
  fertilisation rate and usable-blastocyst rate. KPIs on the cycle overview show green/amber/red
  against these bands (computed server-side, `02-API-GUIDE.md` §5).
- **Carry-over rule.** Any embryo not frozen/transferred/discarded/donated on a given day must be
  carried forward to the next day's culture (M18-026) — enforced in the assessment service.

### Sources
- Vienna consensus (ART lab KPIs), *Human Reproduction Open* 2017 —
  https://academic.oup.com/hropen/article/2017/2/hox011/4062213 ·
  ESHRE SIG copy: https://www.eshre.eu/-/media/sitecore-files/SIGs/Safety-and-quality/The-Vienna-consenus_Lab-KPI.pdf
- Istanbul consensus update (ESHRE/ALPHA oocyte & embryo assessment), *Human Reproduction* 2025 —
  https://academic.oup.com/humrep/article/40/6/989/8120385 ·
  open copy: https://pmc.ncbi.nlm.nih.gov/articles/PMC12127515/
- HFEA-licensed clinic witnessing procedures (double/electronic witnessing) —
  https://qualifications.pearson.com/content/dam/pdf/NVQ-and-competence-based-qualifications/healthcare-science/2017/Specification/unit-20-procs-for-witnessing-in-hfea-fert-clinic.pdf
- Electronic vs manual witnessing (timing/efficiency), *F&S Reports* 2021 —
  https://pmc.ncbi.nlm.nih.gov/articles/PMC8267391/

> These are engineering-grounding references. Final clinical acceptance sits with the MIVF/SME
> UAT owners named in the spec's Requirement sheet — build the fields and rules, let the clinical
> owners sign off the thresholds.

---

## 3. Architecture on the real stack

```
OxArFrontendReact  (React 17 CRA, react-router v6, redux-toolkit, axios, bootstrap, MSAL B2C)
   src/services/*          → axios calls to OXAR Web API (REACT_APP_OXAR_API_URL)
   src/.../<pages>         → screens routed in  src/Layout Component/Layout1.js
        │  axios interceptor adds Bearer; AllApiCall.GettheHmToken() supplies hmacauth
        ▼
OxArBackendReact   (ASP.NET Web API 2, attribute-routed, [Authorize] + HMAC)
   Controllers → Services (IXxx/Xxx) → UnitOfWork.CRMCoreRepository → Dataverse (bcrm_*)
        ▼
OX.ar Dataverse    contact · bcrm_treatment_cycle · bcrm_egg · bcrm_eggdetail · (15 new tables)
```

**Auth:** MSAL Azure AD B2C on the frontend (`authconfig.js`); the axios request interceptor in
`src/index.js` attaches the `Authorization: Bearer` token; `AllApiCall.GettheHmToken()` mints the
`hmacauth` token the API's HMAC handler expects. The Embryo Module reuses this unchanged — **no new
auth mechanism.**

> **Audience note.** `OxArFrontendReact` today is largely a *patient-facing* portal. The Embryo
> Module is *staff/lab-facing*. Two viable placements — decide with the product owner before
> Phase 1 (this is the one open decision that changes file layout):
> 1. **A gated staff area inside this SPA** — a `/lab/*` route tree behind a staff-role guard
>    (fastest; reuses auth, services, build). Recommended for the prototype/MVP.
> 2. **A separate staff app** sharing the same API — cleaner long-term separation of patient vs
>    lab UX. More setup.
> This guide assumes option 1 (a `/lab` route tree) and notes where option 2 differs.

---

## 4. Screen inventory (prototype → build)

The prototype's left rail groups screens the way an embryologist moves through a day. Each maps to
a route, a set of API calls, and requirements:

| Nav group | Screen (`data-view`) | Route (option 1) | Primary entities/endpoints | Requirements |
|---|---|---|---|---|
| Planning | Worklist | `/lab/worklist` | `EmbryoEvent/GetWorklist` | M18-002/003 |
| Planning | Cycle Overview | `/lab/cycle/:id` | `EmbryoEvent/GetByCycleId`, KPIs, specimens | M18-001/009/010 |
| Planning | Consent & 3-Point Check | `/lab/checks/:cycleId` | `ConsentChecklist`, `ThreePointCheck` | M18-011/012 |
| Procedures | OPU (Egg Collection) | `/lab/opu/:cycleId` | `Opu`, creates oocyte `bcrm_egg` | M18-013/014 |
| Procedures | Semen Preparation | `/lab/semen/:cycleId` | `SemenPrep` | M18-016/017/018/019 |
| Procedures | Insemination / Fertilisation | `/lab/fert/:cycleId` | `Insemination`, `Assessment/CreateFertilisation` | M18-020/022/023/024 |
| Lab Culture | Embryo Development | `/lab/develop/:cycleId` | `Specimen/GetEmbryosByCycle`, `Assessment/CreateDevelopment` | M18-025/026 |
| Lab Culture | Transfer | `/lab/transfer/:cycleId` | `EmbryoTransfer` | M18-027/035/039 |
| Cryo Storage | Tank View | `/lab/tank` | `CryoTank/GetBySite`, `GetById` | M18-004 |
| Cryo Storage | Inventory | `/lab/inventory/:tankId` | `CryoTank/GetInventory` | M18-005 |
| Governance | Witnessing Log | `/lab/witnessing` | `ThreePointCheck` + event witnesses | M18-012 governance |
| (cross-cutting) | Event Templates | modal/`/lab/templates` | `EventTemplate` | M18-007/008 |
| (cross-cutting) | Freeze / Thaw / Biopsy | detail dialogs off Culture/Cryo | `FreezeEvent`,`ThawEvent`,`Biopsy` | M18-015/028/029/030/031 |
| (cross-cutting) | Labels / Reports | actions | `LabelPrint`, `Report/CycleOutcome` | M18-006/032/033/034/040 |
| (cross-cutting) | Research banner | cycle header | `ResearchProject/EnrolCycle` | M18-NEW |

The **embryo development grid** (Day 0→7 lane per embryo, stage + Gardner grade + fragmentation +
fate) is the signature view — build it to the highest fidelity.

---

## 5. Phased delivery roadmap

Ordered so each phase ships a usable, independently-demoable slice and de-risks the hardest parts
early (witnessing, the specimen lifecycle, the development grid).

### Phase 0 — Foundations (schema + shell)
- Confirm live `bcrm_egg` / `bcrm_eggdetail` / `bcrm_treatment_cycle` schema.
- Create option sets + the 15 new tables in an unmanaged solution `OXAR_EmbryoModule`
  (`01-DATA-MODEL.md` §7).
- Frontend: add the `/lab` route tree + left-rail shell + top bar (context + **witness chip**) +
  alert strip, using the design tokens in §7. No live data yet.
- **Exit:** shell renders, routes resolve, brand matches prototype.

### Phase 1 — Planning (worklist, cycle overview, gates)
- `EmbryoEvent` + `EventTemplate` + `ConsentChecklist` + `ThreePointCheck` entities, services,
  controllers.
- Screens: Worklist, Cycle Overview (with KPI tiles wired to a stub then real KPI service),
  Consent & 3-Point Check.
- Implement the **3-point-check block** and the **consent gate** end-to-end (UI + server rule).
- **Exit:** an embryologist can plan a cycle's events from a template and cannot start a critical
  step without a passed independent-witness 3-point check. (M18-001/002/003/007/008/011/012)

### Phase 2 — Procedures (OPU, semen, insemination, fertilisation)
- `Opu`, `SemenPrep`, `Insemination` entities/services; assessment writes on `bcrm_eggdetail`.
- OPU creates oocyte `bcrm_egg` rows with per-cycle seq numbers; MII/MI/GV split; denuding sets
  ICSI eligibility; fertilisation PN board (1st + 2nd check).
- **Exit:** retrieval → prep → insemination → fertilisation recorded, specimens exist with
  provenance. (M18-013/014/016–024)

### Phase 3 — Lab Culture (development grid + transfer)
- Development assessment (Day 2–7) with Gardner grading component fields + carry-over rule.
- The signature **development grid**; **Transfer** procedure (fresh/FET).
- KPI service live (fertilisation rate, blastocyst rate vs Vienna bands).
- **Exit:** daily grading + transfer with outcomes; KPIs render with benchmark bands.
  (M18-010/025/026/027/035/039)

### Phase 4 — Cryo Storage & Governance
- `CryoTank` + `CryoLocation`; graphical tank view + inventory (derived over `bcrm_egg`).
- `FreezeEvent` / `ThawEvent` / `Biopsy` with location allocation/free + survival counts.
- Witnessing Log; Label print log; Research enrolment banner.
- **Exit:** freeze allocates a location, thaw frees it, tanks show usage, full witnessing record.
  (M18-004/005/006/015/028/029/030/031/036/NEW)

### Phase 5 — Reporting & manager tools
- Cycle outcome report + patient/referrer letters; lab worksheet print; manager
  correct/delete-and-re-enter (privileged, audited).
- **Exit:** outcomes shareable; managers can correct records with audit trail.
  (M18-032/033/034/037/038/040)

---

## 6. Requirement traceability (M18-000 … NEW)

| Req | Summary | MVP | Phase | Screen / entity |
|---|---|---|---|---|
| M18-000 | Embryology Cycle Management (umbrella) | Y | all | module |
| M18-001 | View upcoming cycles (forecast diary) | Y | 1 | Cycle Overview / diary |
| M18-002 | View events for site/date range, filter/sort | Y | 1 | Worklist |
| M18-003 | Outstanding events by type | Y | 1 | Worklist filter |
| M18-004 | Cryo tank graphical view + usage | Y | 4 | Tank View / `bcrm_cryotank` |
| M18-005 | Patients/materials in a tank | Y | 4 | Inventory (derived) |
| M18-006 | Print patient/partner labels | N | 4 | `bcrm_labelprintlog` |
| M18-007 | Select predefined event template | Y | 1 | `bcrm_eventtemplate` |
| M18-008 | Add/remove events from template | Y | 1 | template editor |
| M18-009 | View gametes/embryos for a cycle | Y | 1/2 | Cycle Overview / `bcrm_egg` |
| M18-010 | View dev status & quality to pick thaw | Y | 3 | Development grid |
| M18-011 | Check consents/screenings complete | N | 1 | `bcrm_consentchecklist` |
| M18-012 | Record 3-point check before OPU | Y | 1 | `bcrm_threepointcheck` |
| M18-013 | Unique seq# per egg/embryo | Y | 2 | `bcrm_egg.bcrm_seqno` |
| M18-014 | Record egg collection details | Y | 2 | `bcrm_opu` |
| M18-015 | Record thawing of frozen oocytes | Y | 4 | `bcrm_thawevent` |
| M18-016 | Record semen prep (collection/thaw) | Y | 2 | `bcrm_semenprep` |
| M18-017 | Record semen source used | Y | 2 | `bcrm_semenprep` |
| M18-018 | Record semen quality + quantity used | Y | 2 | `bcrm_semenprep` |
| M18-019 | Record frozen semen used + mark thawed | Y | 2 | `bcrm_semenprep` + `bcrm_egg` |
| M18-020 | Denuding dev status (ICSI) | Y | 2 | `bcrm_eggdetail` (maturity) |
| M18-021 | Mark eggs for freeze + allocate tank pos | Y | 4 | `bcrm_freezeevent` + `bcrm_cryolocation` |
| M18-022 | Insemination details per egg | Y | 2 | `bcrm_insemination` + `bcrm_eggdetail` |
| M18-023 | Fertilisation PN/PB per embryo | Y | 2 | `bcrm_eggdetail` |
| M18-024 | Second fertilisation check (growth) | Y | 2 | `bcrm_eggdetail` |
| M18-025 | Day 2–7 stage/grade/frags per embryo | Y | 3 | `bcrm_eggdetail` |
| M18-026 | Mark daily fate + carry-over | Y | 3 | assessment rule |
| M18-027 | Mark for transfer + record transfer | Y | 3 | `bcrm_embryotransfer` |
| M18-028 | Mark for freeze + record freezing | Y | 4 | `bcrm_freezeevent` |
| M18-029 | Mark biopsied + biopsy details | Y | 4 | `bcrm_biopsy` |
| M18-030 | Mark for re-biopsy | Y | 4 | `bcrm_biopsy` |
| M18-031 | Enter storage location for frozen | Y | 4 | `bcrm_cryolocation` |
| M18-032 | Share/print embryology outcome report | N | 5 | `Report` |
| M18-033 | Patient cycle-outcome letter | N | 5 | `Report` |
| M18-034 | Referring-doctor letter | N | 5 | `Report` |
| M18-035 | Choose correct embryos to thaw/transfer | Y | 3 | Development grid + Transfer |
| M18-036 | Mark eggs/embryos donated to recipient | Y | 4 | `Specimen/MarkDonated` |
| M18-037 | Manager correct/update records | N | 5 | privileged Update |
| M18-038 | Manager delete + re-enter | N | 5 | privileged Delete (audited) |
| M18-039 | Allocate transfer time in diary | Y | 3 | `bcrm_embryoevent` (diary) |
| M18-040 | Print lab worksheet for a cycle | Y | 5 | `Report` |
| M18-NEW | Research banner + enrolment on cycle | Y | 4 | `bcrm_researchproject` + cycle flag |

---

## 7. Design system (OX.DH / OX.ar branding)

Match the prototype exactly. Fonts: **Montserrat** (headings), **Lato** (body),
**JetBrains Mono** (specimen IDs + lab data — tabular numerals). Add these tokens to a shared
stylesheet (`src/lab/labTheme.css` or CSS-in-JS) — do **not** restyle the existing patient portal.

```css
:root{
  --ink:#221f20;
  --aqua:#45a59d;        /* OX.ar signature aqua */
  --aqua-deep:#2f7d76;   /* primary buttons, active nav */
  --aqua-soft:#eaf5f3;   /* hover, active backgrounds */
  --aqua-line:#cfe6e2;
  --blue:#010f99;
  --paper:#f5f8f7;       /* app background */
  --card:#ffffff;
  --line:#e4ecea;
  --muted:#6c7c7a; --muted-2:#8a9997;
  --green:#2f9e6f; --green-soft:#e7f5ee;   /* normal / pass / witnessed */
  --amber:#c9821c; --amber-soft:#fbf1de;   /* attention */
  --red:#cf4a4a;   --red-soft:#fbe9e9;     /* abnormal / arrested / fail */
  --purple:#7a5cc0;--purple-soft:#efeafb;  /* research / info */
  --slate:#697a78; --slate-soft:#eef2f1;
  --radius:12px; --radius-sm:8px;
  --shadow:0 1px 2px rgba(34,31,32,.04),0 6px 20px rgba(34,31,32,.05);
}
.mono{font-family:'JetBrains Mono',monospace;font-feature-settings:"tnum" 1}
h1,h2,h3,h4{font-family:'Montserrat',sans-serif;letter-spacing:-.01em}
body{font-family:'Lato',system-ui,sans-serif;color:var(--ink);background:var(--paper)}
```

Semantic colour usage (keep consistent so clinicians read state at a glance):
- **green** = normal fertilisation (2PN), passed check, witnessed, KPI within benchmark.
- **amber** = needs attention, KPI in competence-but-below-benchmark band.
- **red** = abnormal (0/1/3PN), arrested embryo, high fragmentation, failed check/thaw, KPI below competence.
- **purple** = research enrolment / informational.
- **mono** for every specimen ID, tank/goblet/straw position, and numeric lab value.

Component vocabulary from the prototype to reproduce: top bar (logo `OX.ar`, patient/cycle context
chip, live **two-person witness chip**), alert strip (allergy/consent/research/cycle-status), left
rail nav groups, stat tiles (KPI with accent bar + benchmark band), badges (status dots), and the
Day 0→7 development grid. Full recipes in `03-FRONTEND-GUIDE.md`.

**All demo data must stay clearly synthetic** (SAMPLE / MODEL / DEMO / PROTO patients), as in the
prototype — never seed real patient identifiers.

---

## 8. Cross-cutting requirements

- **Fully read-write (add / edit / update / delete).** Every entity — and every reference list
  that drives the forms (event templates, cryo tanks, sites, research projects, quality grades) —
  supports full CRUD from the UI, not just reads. Editing is guarded by **optimistic concurrency**
  (RowVersion → 409 on stale edits), **Dataverse auditing** (field-level edit history),
  **role gating** (viewers read; embryologists add/edit; managers delete/correct-and-re-enter,
  M18-037/038), and **soft-retire by default** (`statecode` + reason) for reportable data — hard
  delete is the manager-only exception. Fixed clinical option sets (maturity, PN, fate) change via
  a solution deployment; runtime-editable lists are CRUD-managed reference entities. Full detail:
  `../../../OxArBackendReact/docs/embryo-module/04-CRUD-AND-EDITING.md` (backend) and
  `03-FRONTEND-GUIDE.md` §5b/§5c (frontend).
- **Witnessing is pervasive**, not a screen: the witness chip is always visible; every
  create/update on a critical entity carries `bcrm_witness`; the API rejects performer==witness.
- **Auditing on** for every lab table (Dataverse auditing) — regulatory.
- **UTC dates** end-to-end (`SpecifyKind` on write; display in clinic local time).
- **Optimistic concurrency**: two embryologists may touch one cycle — prefer per-event rows over
  editing a shared blob; surface "modified by X" (the services already read `modifiedby`).
- **Accessibility**: colour is never the only signal (pair with label/icon) — clinicians may be
  colour-blind and the data is safety-critical.
- **No mock fallback in production paths** — wire screens to live endpoints; use synthetic
  Dataverse rows for demos, not hardcoded arrays.

## 9. Acceptance & definition of done (per slice)
1. Dataverse entity exists in the `OXAR_EmbryoModule` solution with option sets + relationships.
2. Endpoint smoke-tested in Swagger; option-set/lookup values round-trip.
3. React service method + page wired to the live endpoint (no mock fallback).
4. Witness/consent/eligibility rules enforced **server-side** and reflected in the UI.
5. Brand matches the prototype tokens; specimen IDs and lab values in mono.
6. Requirement ticked in the traceability table; MIVF/SME UAT owner notified for clinical sign-off.

---

**Next:** backend engineers start at `../../../OxArBackendReact/docs/embryo-module/01-DATA-MODEL.md`;
frontend engineers continue to `03-FRONTEND-GUIDE.md`.
