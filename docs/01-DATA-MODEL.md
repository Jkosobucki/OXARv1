# OX.ar Embryo Module — Data Model & Dataverse Entities (Backend)

> **Scope boundary.** This document and the whole `docs/embryo-module/` set belong to the
> **OX.assisted-reproduction (OX.ar)** solution only. It is **entirely independent of OX.gp**.
> No OX.gp entity, service, endpoint, config, or wire contract is referenced, imported, or
> reused here. Everything below lives in the `OxArBackendReact` (`OXAR` Web API) and
> `OxArFrontendReact` repositories and the OX.ar Dataverse organisation.

This is the **source-of-truth data specification** for Module **M-18 (Embryology Cycle
Management)**. It maps the uploaded `Embryo_Module.xlsx` specification onto the OX.ar Dataverse
schema, tells you **what already exists** versus **what must be created**, and defines the
option sets, relationships, and auto-numbering that the API layer (see `02-API-GUIDE.md`) and the
React UI (see `../../../OxArFrontendReact/docs/embryo-module/`) depend on.

---

## 1. How the OX.ar backend already stores lab data

`OxArBackendReact` is an ASP.NET Web API 2 (.NET Framework) service in a strict layered shape:

```
Controller (attribute-routed, [Authorize], HMAC/Bearer)
   → Service (IXxxService / XxxService)
      → UnitOfWork.CRMCoreRepository
         → Dataverse (Microsoft.Xrm.Sdk, FetchXML for reads; Entity for writes)
```

Reads use **FetchXML**; writes build a `Microsoft.Xrm.Sdk.Entity` and call
`CRMCoreRepository.Create/Update/Delete`. All business tables use the **`bcrm_` publisher
prefix**. The relevant existing tables (confirmed from live FetchXML in
`TaskService.Services/StoredSamples/StoredSampleService.cs`) are:

| Existing table | Role today | Key columns seen in code |
|---|---|---|
| `contact` | Patient / partner / donor | standard |
| `bcrm_treatment_cycle` | The treatment cycle a specimen belongs to | referenced as lookup |
| `bcrm_treatment_stage` (`bcrm_treatment_stageid`) | Stage within a cycle | referenced as lookup |
| **`bcrm_egg`** | **Specimen / material master** (egg, sperm, embryo, straw) with cryo attributes | `bcrm_eggs_id`, `bcrm_type`, `bcrm_patient`, `bcrm_partner`, `bcrm_treatment_cycle`, `bcrm_freeze_date`, `bcrm_datetimeofeggthaw`, `bcrm_location`, `bcrm_well`, `bcrm_dish`, `bcrm_straw`, `bcrm_goblet`, `bcrm_ampoule`, `bcrm_cane_stick`, `bcrm_nitrogen_tank`, `bcrm_consent`, `bcrm_consent_expires_on`, `bcrm_storage_paid_until`, `bcrm_egg_status`, `bcrm_stage`, `bcrm_frozen`, `bcrm_sperm`, `bcrm_typeofsperm`, `bcrm_number_remaining`, `bcrm_straws_remaining`, `statecode`, `statuscode` |
| **`bcrm_eggdetail`** | **Per-specimen assessment / development detail** (per day, per check) | `bcrm_egg` (parent lookup), `bcrm_day`, `bcrm_maturity`, `bcrm_pb`, `bcrm_cellcount`, `bcrm_fragmentation`, `bcrm_icm`, `bcrm_te`, `bcrm_blastocyst`, `bcrm_morula`, `bcrm_symmetry`, `bcrm_embryofate`, `bcrm_biospy` (sic), `bcrm_inseminationtype`, `bcrm_fretilizationtime` (sic), `bcrm_instrumentnumber`, `bcrm_well`, `bcrm_dish`, `bcrm_position`, `bcrm_pin`, `bcrm_path` |

> ⚠️ **Two inherited misspellings are on the wire — keep them exactly:**
> `bcrm_biospy` (biopsy) and `bcrm_fretilizationtime` (fertilisation time). Do not "fix" them in
> code; the Dataverse columns are named this way.

### The central modelling decision

The Excel workbook lists **23 conceptual tables**. Do **not** create 23 new Dataverse tables.
The existing `bcrm_egg` + `bcrm_eggdetail` pair already implements the hardest part of the
spec — the **Specimen** master and its **per-day assessment** children. The right architecture is:

- **Reuse** `bcrm_egg` as the unified **Specimen** entity (the Excel "Gamete" + "Embryo" +
  "Storage Inventory" sheets are all views/filters over this one table, discriminated by
  `bcrm_type`).
- **Reuse** `bcrm_eggdetail` as the unified **Assessment** entity (the Excel "Denuding
  Assessment", "Fertilisation Assessment", and "Embryo Development Assessment" sheets are all the
  same child rows, discriminated by `bcrm_day` / assessment type).
- **Add** the **workflow / event / governance / storage-structure** tables that genuinely do not
  exist yet (events, templates, 3-point check, OPU, semen prep, insemination, freeze/thaw,
  biopsy, cryo tank hierarchy, consent checklist, diary, label log, research project).

This keeps one specimen identity per egg/embryo across its whole life (collection → culture →
freeze → thaw → transfer/discard), which is exactly what electronic witnessing and HFEA
traceability require. See `../../../OxArFrontendReact/docs/embryo-module/README.md` §"Best-practice
foundations" for why single-specimen-identity matters.

> **Action before you build:** export the live `bcrm_egg` and `bcrm_eggdetail` metadata from the
> OX.ar Dataverse org (Maker portal → Tables → Columns, or
> `GET [Organization Service]/EntityDefinitions`) and confirm the column list below against it.
> Some columns in the spec may already exist under a slightly different name. **Never add a column
> that already exists.**

---

## 2. Excel sheet → Dataverse entity mapping

| # | Excel sheet | Target entity | Exists? | Notes |
|---|---|---|---|---|
| 1 | Requirement | — | n/a | Backlog (M18-000…M18-040), see traceability in master guide |
| 2 | Event Template | `bcrm_eventtemplate` | **NEW** | Predefined plan of events per treatment type |
| 3 | Event Template Detail | `bcrm_eventtemplatedetail` | **NEW** | Child rows: event type, sequence, mandatory, default day |
| 4 | Three Point Check | `bcrm_threepointcheck` | **NEW** | Identity/consent verification gate before every critical step |
| 5 | Embryo Event | `bcrm_embryoevent` | **NEW** | Scheduling / worklist / diary spine for the whole module |
| 6 | OPU (Egg Collection) | `bcrm_opu` | **NEW** | One per retrieval; produces `bcrm_egg` oocyte rows |
| 7 | Semen Preparation | `bcrm_semenprep` | **NEW** | Collection / thaw / quality of the sperm sample |
| 8 | Denuding Assessment (ICSI) | `bcrm_eggdetail` | **REUSE** | Maturity assessment row (`bcrm_maturity`, `bcrm_day`=denude) |
| 9 | Insemination | `bcrm_insemination` | **NEW** | IVF/ICSI event linking sperm prep + oocytes |
| 10 | Fertilisation Assessment | `bcrm_eggdetail` | **REUSE** | PN/PB row (`bcrm_pb` + a new `bcrm_pn`), day = fert-check |
| 11 | Gamete | `bcrm_egg` (type=egg/sperm) | **REUSE** | Filtered view of the specimen master |
| 12 | Embryo | `bcrm_egg` (type=embryo) | **REUSE** | Filtered view of the specimen master |
| 13 | Embryo Development Assessment | `bcrm_eggdetail` | **REUSE** | Day 2–7 rows (cell count, grade, frag, stage, fate) |
| 14 | Embryo Transfer | `bcrm_embryotransfer` | **NEW** | Transfer procedure (fresh/frozen), catheter, order, outcome |
| 15 | Freeze Event | `bcrm_freezeevent` | **NEW** | Cryopreservation event + witness + storage allocation |
| 16 | Thaw Event | `bcrm_thawevent` | **NEW** | Warming event + survival counts + outcome |
| 17 | Biopsy | `bcrm_biopsy` | **NEW** | PGT biopsy + re-biopsy, lab ref, result |
| 18 | Cryo Storage Location | `bcrm_cryolocation` | **NEW** | Tank→canister→goblet→straw/vial position |
| 19 | Cryo Tank | `bcrm_cryotank` | **NEW** | Tank master + capacity/usage for the graphical view |
| 20 | Storage Inventory | (view over `bcrm_egg`) | **DERIVE** | Query frozen `bcrm_egg` joined to `bcrm_cryolocation` — no new table |
| 21 | Label Print Log | `bcrm_labelprintlog` | **NEW** | Audit of label prints (patient/partner/specimen) |
| 22 | Consent Checklist | `bcrm_consentchecklist` | **NEW** | Prerequisite gate per cycle (consent/screening/approval) |
| 23 | Diary Schedule | `bcrm_embryoevent` (+ room/user) | **REUSE/EXTEND** | Diary is the calendar projection of events; add room + assigned-user columns |
| — | Research project (NEW req) | `bcrm_researchproject` + flag on cycle | **NEW** | Research banner/enrolment on the treatment cycle |

**Net new tables: 15.** Reused/derived: 8. This is the recommended footprint.

---

## 3. New entity definitions

Conventions for every new table (match the org's existing tables):
- Prefix `bcrm_`, Ownership **User/Team**, enable **Notes** + **Auditing** (regulatory).
- Primary name column = human-readable auto-number (see §5).
- Add `statecode`/`statuscode` (all Dataverse tables have these) and use them for
  Active/Inactive + workflow status where a spec status list maps cleanly. **`statecode` is also
  the soft-delete / retire mechanism** — prefer retiring a reportable row over hard-deleting it
  (see `04-CRUD-AND-EDITING.md` §4).
- Every lab entity gets **`bcrm_witness`** (lookup → `systemuser` or `contact`/staff table) and
  the performing **`bcrm_embryologist`** — witnessing is not optional (HFEA). See §4.
- **Editability columns (every table):** `modifiedon` / `modifiedby` (built-in) surface
  "last edited by X"; the built-in **`RowVersion`** backs optimistic concurrency on edits
  (`04-CRUD-AND-EDITING.md` §3). Add `bcrm_cancelreason` (T) wherever a soft-retire needs a reason.

> **Every entity is fully editable** — Create / Update / Delete, not read-only. The existing
> `bcrm_egg` (surfaced today via read-only `StoredSampleService`) must gain `Create`/`Update`/
> `Delete` so specimens can be edited/added. The reference tables below (templates, cryo tanks,
> research projects, and any runtime-editable list) are CRUD-managed from the UI rather than
> hard-coded — see `04-CRUD-AND-EDITING.md` §6 for the option-set-vs-reference-entity decision.

Legend: `PK`=primary auto-number, `L→x`=lookup to x, `OS`=option set (§4), `DT`=DateTime,
`#`=whole/decimal number, `T`=text, `Y/N`=two-option.

### 3.1 `bcrm_embryoevent` — Embryology Event (worklist / diary spine)
The backbone of Planning. Every scheduled or completed lab action is an event row; the worklist,
outstanding-events filter, and diary are all queries over this table.

| Column | Type | Notes |
|---|---|---|
| `bcrm_embryoeventid` | PK | Auto `EV-{SEQ}` |
| `bcrm_name` | T | Event name |
| `bcrm_eventtype` | OS | `EventType` (§4.1) |
| `bcrm_status` | OS | `EventStatus` (§4.2): Scheduled/Ready/In Progress/Completed/Cancelled/Failed |
| `bcrm_priority` | OS | Low/Normal/High/Urgent |
| `bcrm_site` | L→`bcrm_site`/OS | Storage/clinic site |
| `bcrm_scheduleddate` | DT | |
| `bcrm_scheduledstarttime` / `bcrm_scheduledendtime` | DT | |
| `bcrm_actualstarttime` / `bcrm_actualendtime` | DT | |
| `bcrm_patient` | L→`contact` | |
| `bcrm_partner` | L→`contact` | |
| `bcrm_treatment_cycle` | L→`bcrm_treatment_cycle` | |
| `bcrm_cycletype` | OS | mirrors treatment type |
| `bcrm_embryologist` | L→staff | assigned |
| `bcrm_witness` | L→staff | |
| `bcrm_materialtype` | OS | `MaterialType` (§4.3): Embryo/Sperm/Oocyte |
| `bcrm_materialrecord` | L→`bcrm_egg` | the specimen this event acts on |
| `bcrm_outcome` | OS/T | |
| `bcrm_outcomenotes` / `bcrm_comments` | T (multiline) | |
| `bcrm_room` | L/OS | (diary) |
| `bcrm_assigneduser` | L→staff | (diary) |

### 3.2 `bcrm_eventtemplate` + `bcrm_eventtemplatedetail` — Event Templates
Predefined plan the embryologist selects per cycle (M18-007/008).

`bcrm_eventtemplate`: `bcrm_eventtemplateid` (PK `TMPL-{SEQ}`), `bcrm_name`,
`bcrm_treatmenttype` (OS), `bcrm_description` (T), `bcrm_active` (Y/N).

`bcrm_eventtemplatedetail` (child of template): `bcrm_eventtemplatedetailid` (PK),
`bcrm_template` (L→`bcrm_eventtemplate`), `bcrm_eventtype` (OS `EventType`),
`bcrm_sequence` (#), `bcrm_mandatory` (Y/N), `bcrm_defaultday` (#, day offset from Day 0).

### 3.3 `bcrm_threepointcheck` — Three-Point Check (safety gate)
Blocks a critical procedure until an independent witness confirms three patient identifiers
(M18-012). Recorded before every critical step (OPU, insemination, transfer, freeze, thaw,
discard, donation).

| Column | Type | Notes |
|---|---|---|
| `bcrm_threepointcheckid` | PK | `3PC-{SEQ}` |
| `bcrm_treatment_cycle` | L→cycle | |
| `bcrm_relatedevent` | L→`bcrm_embryoevent` | the step being gated |
| `bcrm_patientverified` | Y/N | identifier 1 |
| `bcrm_partnerverified` | Y/N | identifier 2 |
| `bcrm_consentverified` | Y/N | identifier 3 |
| `bcrm_verifiedby` | L→staff | performer |
| `bcrm_witness` | L→staff | **must differ from performer** (enforce in service, §02) |
| `bcrm_verificationdate` | DT | |
| `bcrm_status` | OS | Pending / Passed / Failed |
| `bcrm_notes` | T | |

### 3.4 `bcrm_opu` — Egg Collection (Oocyte Pick-Up)
One row per retrieval (M18-014). Produces oocyte `bcrm_egg` rows; the MII/MI/GV split drives how
many oocytes are eligible for ICSI.

`bcrm_opuid` (PK `OPU-{SEQ}`), `bcrm_treatment_cycle` (L), `bcrm_proceduredate` (DT),
`bcrm_doctor` (L→staff), `bcrm_embryologist` (L→staff), `bcrm_witness` (L→staff),
`bcrm_folliclesaspirated` (#), `bcrm_eggsretrieved` (#), `bcrm_mii` (#), `bcrm_mi` (#),
`bcrm_gv` (#), `bcrm_complications` (T), `bcrm_notes` (T).

### 3.5 `bcrm_semenprep` — Semen Preparation
M18-016/017/018/019. `bcrm_semenprepid` (PK `SP-{SEQ}`), `bcrm_treatment_cycle` (L),
`bcrm_semensource` (OS `SemenSource`: Partner/Donor/Surgical(TESE/PESA)),
`bcrm_freshfrozen` (OS), `bcrm_collectiondate` (DT), `bcrm_thawdate` (DT), `bcrm_volume` (# ml),
`bcrm_count` (# ×10⁶/ml), `bcrm_motility` (# %), `bcrm_morphology` (# %),
`bcrm_quantityused` (#), `bcrm_qualitygrade` (OS/T), `bcrm_usedfor` (OS: IVF/ICSI/IUI),
`bcrm_frozensamplerecord` (L→`bcrm_egg`, when frozen — marks the owner's straw as thawed),
`bcrm_notes` (T).

### 3.6 `bcrm_insemination` — Insemination
M18-022. `bcrm_inseminationid` (PK `INS-{SEQ}`), `bcrm_treatment_cycle` (L),
`bcrm_method` (OS: IVF / ICSI), `bcrm_date` (DT), `bcrm_embryologist` (L→staff),
`bcrm_semenprep` (L→`bcrm_semenprep`), `bcrm_spermsource` (OS), `bcrm_noofeggs` (#),
`bcrm_successfuleggs` (#), `bcrm_witness` (L→staff), `bcrm_notes` (T).
Per-oocyte insemination detail (which oocyte, instrument) is recorded on `bcrm_eggdetail`
(`bcrm_inseminationtype`, `bcrm_instrumentnumber`).

### 3.7 `bcrm_embryotransfer` — Embryo Transfer
M18-027/035/039. `bcrm_embryotransferid` (PK `ET-{SEQ}`), `bcrm_treatment_cycle` (L),
`bcrm_transferdate` (DT), `bcrm_embryologist` (L→staff), `bcrm_doctor` (L→staff),
`bcrm_embryo` (L→`bcrm_egg`, type=embryo), `bcrm_transfertype` (OS: Fresh / Frozen(FET)),
`bcrm_transferorder` (#), `bcrm_catheter` (T), `bcrm_outcome` (OS), `bcrm_witness` (L→staff),
`bcrm_notes` (T). (Supports multiple embryos via multiple rows or a child grid.)

### 3.8 `bcrm_freezeevent` — Freeze Event
M18-028/031. `bcrm_freezeeventid` (PK `FRZ-{SEQ}`), `bcrm_patient` (L), `bcrm_treatment_cycle` (L),
`bcrm_materialtype` (OS `MaterialType`), `bcrm_materialrecord` (L→`bcrm_egg`), `bcrm_freezedate` (DT),
`bcrm_embryologist` (L→staff), `bcrm_witness` (L→staff),
`bcrm_storagelocation` (L→`bcrm_cryolocation`), `bcrm_quantityfrozen` (#), `bcrm_outcome` (OS),
`bcrm_notes` (T). **On success the service must also update the `bcrm_egg` row**
(`bcrm_frozen`, `bcrm_freeze_date`, `bcrm_location`, tank/goblet/straw) and set the
`bcrm_cryolocation` status to Occupied.

### 3.9 `bcrm_thawevent` — Thaw Event
M18-015. `bcrm_thaweventid` (PK `THW-{SEQ}`), `bcrm_patient` (L), `bcrm_treatment_cycle` (L),
`bcrm_materialtype` (OS), `bcrm_materialrecord` (L→`bcrm_egg`), `bcrm_scheduledthawdate` (DT),
`bcrm_actualthawdate` (DT), `bcrm_embryologist` (L→staff), `bcrm_witness` (L→staff),
`bcrm_storagelocation` (L→`bcrm_cryolocation`), `bcrm_quantitythawed` (#),
`bcrm_quantitysurvived` (#), `bcrm_outcome` (OS: Successful / Partial Survival / Failed),
`bcrm_status` (OS), `bcrm_notes` (T). On success, free the `bcrm_cryolocation` (→ Available) and
update the specimen status to Thawed.

### 3.10 `bcrm_biopsy` — Biopsy / Re-Biopsy
M18-029/030. `bcrm_biopsyid` (PK `BX-{SEQ}`), `bcrm_embryo` (L→`bcrm_egg`), `bcrm_biopsydate` (DT),
`bcrm_biopsytype` (OS: PGT-A / PGT-M / PGT-SR), `bcrm_labreference` (T), `bcrm_result` (OS/T),
`bcrm_rebiopsyrequired` (Y/N), `bcrm_witness` (L→staff), `bcrm_notes` (T).

### 3.11 Cryo storage structure
**`bcrm_cryotank`** (M18-004): `bcrm_cryotankid` (PK `TANK-{SEQ}`), `bcrm_name`, `bcrm_site` (L/OS),
`bcrm_capacity` (#), `bcrm_currentusage` (# — roll-up or maintained), `bcrm_active` (Y/N),
`bcrm_status` (OS: OK / Low N₂ / Maintenance / Alarm), `bcrm_notes` (T).

**`bcrm_cryolocation`** (M18-005): `bcrm_cryolocationid` (PK `LOC-{SEQ}`),
`bcrm_tank` (L→`bcrm_cryotank`), `bcrm_canisternumber` (T/#), `bcrm_gobletnumber` (T/#),
`bcrm_strawvialnumber` (T/#), `bcrm_storagetype` (OS `MaterialType`),
`bcrm_storagestatus` (OS: Occupied / Available / Reserved), `bcrm_site` (L/OS), `bcrm_notes` (T).
A frozen `bcrm_egg` points at its `bcrm_cryolocation` (or reuse the existing
`bcrm_nitrogen_tank`/`bcrm_goblet`/`bcrm_straw` columns — confirm live schema first).

> **Storage Inventory (sheet 20) is a query, not a table.** `GET .../GetTankInventory?tankId=` →
> FetchXML over `bcrm_egg` where frozen, joined to `bcrm_cryolocation`, grouped by tank/canister.

### 3.12 Governance / prerequisites
**`bcrm_consentchecklist`** (M18-011): `bcrm_consentchecklistid` (PK `CCK-{SEQ}`),
`bcrm_treatment_cycle` (L), `bcrm_consentcomplete` (Y/N), `bcrm_screeningcomplete` (Y/N),
`bcrm_doctorapproval` (Y/N), `bcrm_readyforprocedure` (Y/N — derived AND of the three),
`bcrm_checkeddate` (DT), `bcrm_checkedby` (L→staff).

**`bcrm_labelprintlog`** (M18-006): `bcrm_labelprintlogid` (PK `LBL-{SEQ}`), `bcrm_patient` (L),
`bcrm_treatment_cycle` (L), `bcrm_materialtype` (OS), `bcrm_materialrecord` (L→`bcrm_egg`),
`bcrm_labeltype` (OS), `bcrm_copies` (#), `bcrm_printedby` (L→staff), `bcrm_printedon` (DT).

**`bcrm_researchproject`** (NEW req) + flag on cycle: `bcrm_researchprojectid` (PK `RES-{SEQ}`),
`bcrm_name`, `bcrm_description`, `bcrm_active` (Y/N), `bcrm_ethicsreference` (T). Add
`bcrm_researchproject` (L) and `bcrm_researchenrolled` (Y/N) columns to `bcrm_treatment_cycle`
so the UI can show the research banner (M18-NEW).

---

## 4. Option sets (global choices)

Create these as **global** option sets so worklist filters, templates, and forms share them.
Store the **integer value** in Dataverse; the API returns the `FormattedValues` label (the
services already do `x.FormattedValues["..."]`). Suggested value bases avoid collisions.

### 4.1 `bcrm_embryo_eventtype` (EventType) — from the Embryo Event sheet
`3 Point Check`, `OPU`, `Semen Collection`, `Semen Thaw`, `Denuding`, `ICSI`, `IVF`,
`Fertilisation Check`, `Second Fertilisation Check`, `Day 2 Assessment` … `Day 7 Assessment`,
`Embryo Selection`, `Embryo Freeze`, `Embryo Thaw`, `Oocyte Freeze`, `Oocyte Thaw`, `Biopsy`,
`Re-Biopsy`, `Embryo Transfer`, `Embryo Discard`, `Donation`.

### 4.2 `bcrm_embryo_eventstatus` (EventStatus)
`Scheduled`, `Ready`, `In Progress`, `Completed`, `Cancelled`, `Failed`.

### 4.3 `bcrm_material_type` (MaterialType)
`Embryo`, `Sperm`, `Oocyte`.

### 4.4 `bcrm_gamete_status` / `bcrm_gamete_type`
Type: `Sperm`, `Oocyte`, `Embryo`. Status: `Collected`, `Frozen`, `Thawed`, `Used`, `Discarded`.

### 4.5 `bcrm_embryo_status` + `bcrm_embryo_disposition`
Status: `Created`, `Under Culture`, `Frozen`, `Thawed`, `Transferred`, `Discarded`, `Donated`.
Disposition: `Stored`, `Transferred`, `Discarded`, `Donated`, `Research`.

### 4.6 `bcrm_oocyte_maturity` (Denuding) — clinical
`MII` (metaphase II — mature, ICSI-eligible), `MI` (metaphase I), `GV` (germinal vesicle),
`Degenerate/Atretic`. **Only MII is suitable for ICSI** — the service sets `SuitableForICSI`
from this automatically (see §02).

### 4.7 `bcrm_pn_status` (Fertilisation) — clinical
`0PN`, `1PN`, `2PN`, `3PN`, `Abnormal`. **2PN = normal fertilisation**; 0/1/3PN are flagged.

### 4.8 `bcrm_pb_status` (polar body)
`0PB`, `1PB`, `2PB`.

### 4.9 `bcrm_embryo_fate` (per-day disposition) — clinical
`Continue Culture`, `Freeze`, `Transfer`, `Biopsy`, `Discard`, `Donate`.
Rule (M18-026): any embryo not discarded/frozen/transferred **must** carry to next-day culture.

### 4.10 `bcrm_development_stage` — clinical (Istanbul consensus terminology)
`2PN/Zygote`, `Cleavage`, `Morula`, `Early Blastocyst`, `Blastocyst`, `Expanded Blastocyst`,
`Hatching Blastocyst`, `Hatched Blastocyst`, `Arrested`.

### 4.11 Grading — store as component parts, not one free-text blob
Gardner blastocyst grade decomposes to three fields so `4AA` sorts and reports correctly:
- `bcrm_expansion` (# 1–6), `bcrm_icm` (OS A/B/C — **exists** on `bcrm_eggdetail`),
  `bcrm_te` (OS A/B/C — **exists**). Cleavage grade uses `bcrm_cellcount` (#) +
  `bcrm_fragmentation` (# %) + `bcrm_symmetry` (**exists**).

> See the master guide's clinical grounding: Gardner expansion 1–6 + ICM/TE A–C, MII/MI/GV
> maturity, PN/PB scoring, and the ESHRE/ALPHA **Istanbul consensus update (2025)** stage
> terminology. Keep component fields separate so KPIs (Vienna consensus) can be computed.

---

## 5. Auto-numbering & keys

Give each new master table a Dataverse **autonumber** primary name column so specimens and events
have human-readable, witness-friendly IDs (critical for label printing and chain of custody):

| Entity | Format |
|---|---|
| `bcrm_egg` (specimen) | already has `bcrm_eggs_id` — reuse |
| `bcrm_embryoevent` | `EV-{SEQNUM:0000}` |
| `bcrm_opu` | `OPU-{SEQNUM:0000}` |
| `bcrm_insemination` | `INS-{SEQNUM:0000}` |
| `bcrm_embryotransfer` | `ET-{SEQNUM:0000}` |
| `bcrm_freezeevent` / `bcrm_thawevent` | `FRZ-` / `THW-{SEQNUM:0000}` |
| `bcrm_biopsy` | `BX-{SEQNUM:0000}` |
| `bcrm_cryotank` / `bcrm_cryolocation` | `TANK-` / `LOC-{SEQNUM:0000}` |

Per M18-013, each egg/embryo also carries a **sequence number within its cycle**
(`Gamete Seq No` / `Embryo Seq No`). Add `bcrm_seqno` (#) to `bcrm_egg` (if not present) and
allocate it per-cycle in the OPU/insemination service.

---

## 6. Relationship map (1:N unless noted)

```
contact (patient) ─┬─< bcrm_treatment_cycle >─┬─< bcrm_embryoevent
                   │                           ├─< bcrm_consentchecklist (1:1 logical)
contact (partner) ─┘                           ├─< bcrm_opu
                                               ├─< bcrm_semenprep
                                               ├─< bcrm_insemination
                                               ├─< bcrm_embryotransfer
                                               ├─< bcrm_freezeevent / bcrm_thawevent
                                               └─< bcrm_egg (specimen; type=oocyte/sperm/embryo)
bcrm_egg ─< bcrm_eggdetail (per-day assessments: denude, fert, day2..7)
bcrm_egg ─< bcrm_biopsy
bcrm_egg  >── bcrm_cryolocation ──> bcrm_cryotank
bcrm_eventtemplate ─< bcrm_eventtemplatedetail
bcrm_embryoevent ── bcrm_threepointcheck (1:1 gate)
bcrm_treatment_cycle >── bcrm_researchproject
```

An **embryo's provenance** (M18-023) links back to its source oocyte and sperm: add
`bcrm_sourceoocyte` (L→`bcrm_egg`) and `bcrm_sourcesperm` (L→`bcrm_egg`/`bcrm_semenprep`) on the
embryo `bcrm_egg` row. This is what lets the development grid trace 4AA back to a specific egg +
sperm — and is a hard requirement for donor traceability and HFEA reporting.

---

## 7. Build order (Dataverse first)

1. Confirm live schema of `bcrm_egg`, `bcrm_eggdetail`, `bcrm_treatment_cycle`, `bcrm_treatment_stage`.
2. Create global option sets (§4).
3. Create the 15 new tables (§3) with autonumbers (§5), witness/embryologist lookups, Notes+Audit on.
4. Add new columns to existing tables: `bcrm_egg.bcrm_seqno`, `bcrm_egg.bcrm_sourceoocyte`,
   `bcrm_egg.bcrm_sourcesperm`; `bcrm_eggdetail.bcrm_pn` (+ confirm `bcrm_pb`, `bcrm_icm`,
   `bcrm_te`, `bcrm_expansion`); `bcrm_treatment_cycle.bcrm_researchproject` + `.bcrm_researchenrolled`.
5. Create relationships (§6).
6. Package everything in a **single unmanaged solution `OXAR_EmbryoModule`** and export it — this
   is your migration artefact between Dev/Test/Prod. Do all schema work inside this solution so it
   stays independent and portable.

Proceed to `02-API-GUIDE.md` for the controller/service/DTO scaffolding that surfaces these tables.
