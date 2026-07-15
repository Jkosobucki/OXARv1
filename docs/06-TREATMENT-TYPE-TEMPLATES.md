# OX.ar — Treatment-Type Display Templates

> **Independent of OX.gp.** OX.ar only.

## Objective
On the **Treatment Cycle** form, show the embryologist **only the fields, tabs and steps that
matter for the selected treatment type** — driven by `bcrm_treatmenttype` (and the patient). Today
every cycle shows every tab and field (Folliculogram, Drugs, Oocyte Retrieval, Embryo Transfer,
IUI/DI, Lab Results, Gametes, …) regardless of type, which slows data entry and invites errors.
Each treatment type gets a **template** that hides what's irrelevant and surfaces the right
workflow.

This reuses the module's existing **Event Template** design (`bcrm_eventtemplate` keyed by
treatment type — `01-DATA-MODEL.md` §3.2): the same template that seeds the lab **events** also
declares which **cycle fields/tabs** to display. One template per treatment type.

## The 8 treatment types (`bcrm_treatmenttype`)
Values from the Treatment Cycle form. **Confirm the option-set integer values against the live
`bcrm_treatmenttype` choice** before wiring (left as "confirm" below).

| # | Treatment type | Option value | One-line purpose |
|---|---|---|---|
| 1 | **IVF** | confirm | Full stimulated cycle: collect eggs → inseminate → culture → transfer/freeze |
| 2 | **Egg Only** | confirm | Egg freezing / fertility preservation: collect → vitrify oocytes (no fertilisation) |
| 3 | **Embryo Transfer** | confirm | Frozen embryo transfer (FET): thaw a stored embryo → transfer |
| 4 | **VOT** ⚠️ | confirm | *Interpreted as Vitrified Oocyte Thaw*: thaw frozen eggs → ICSI → culture → transfer. **Confirm exact meaning with MIVF/SME.** |
| 5 | **IUI** | confirm | Intrauterine insemination: prepared sperm placed in uterus (no egg collection) |
| 6 | **Ovulation Induction** | confirm | Drug-induced ovulation + timed intercourse (monitoring only, no lab procedure) |
| 7 | **Tracking** | confirm | Cycle monitoring / scan tracking only (no procedure) |
| 8 | **Thaw and Refreeze** | confirm | Retrieve stored specimen → thaw → (± biopsy) → refreeze → storage |

## How a template is applied (UI behaviour)
1. User opens/creates a Treatment Cycle and picks **Patient** + **Treatment Type**.
2. The matching template is looked up (by `bcrm_treatmenttype`).
3. The form **hides** the tabs/fields in the template's `hide` set and **shows** the `show` set;
   the header keeps the always-on fields.
4. The template also seeds the **lab event plan** (the "key steps") via
   `EventTemplate/ApplyToCycle` (`02-API-GUIDE.md`), so the worklist for that cycle already lists
   the right procedures.
5. Editable at runtime — a manager can tune a template in the Templates admin screen
   (`03-FRONTEND-GUIDE.md` §5c); it is reference data, not hard-coded (`04-CRUD-AND-EDITING.md` §6).

> **Always-visible core fields** (every type): Treatment Name · Treatment Type · Cycle Sequence
> Number · Patient · Cycle Doctor · Managing Clinic · Treatment Billing Option · Patient RX Group ·
> Expected Start Date · Checklists (consent/3-point-check). Everything else is type-driven.

---

## Per-type templates

Legend — **Steps** = the ordered lab/clinical events (seed as `bcrm_eventtemplatedetail`).
**Tabs** from the cycle form: Overview, Folliculogram, Drugs, Oocyte Retrieval, Embryo Transfer,
IUI/DI, Outcome Data, Lab Results, Gametes, Recommendation, Checklists, Stored Sample(s),
General Information.

### 1. IVF
**Key steps:** Consent & 3-Point Check → Ovarian Stimulation (Drugs) → Folliculogram monitoring →
Trigger → **OPU (Egg Collection)** → Semen Prep → **Insemination (IVF/ICSI)** → Fertilisation Check
→ Embryo Culture (Day 2–7) → **Embryo Transfer (fresh)** → Freeze surplus → Outcome.
- **Show tabs:** Overview, Folliculogram, Drugs, Oocyte Retrieval, Gametes, Lab Results,
  Embryo Transfer, Stored Sample(s), Recommendation, Outcome Data, Checklists, General Information.
- **Hide tabs:** IUI/DI.
- **Show fields:** Partner, Female/Male Infertility Diagnosis, Anzard Cycle Type, Procedure Clinic,
  Cycle Nurse, Sperm Source, No. of Embryos to be Replaced, Expected Treatment Date,
  Expected Date Period Started / Actual Period Date.
- **Hide fields:** Deferred Reason (unless deferred).

### 2. Egg Only (fertility preservation)
**Key steps:** Consent & 3-Point Check → Ovarian Stimulation → Folliculogram → Trigger → **OPU** →
Denuding / Maturity (MII count) → **Vitrify oocytes (Freeze)** → Storage allocation. *(No
insemination, no fertilisation, no embryo culture, no transfer.)*
- **Show tabs:** Overview, Folliculogram, Drugs, Oocyte Retrieval, Gametes, Stored Sample(s),
  Checklists.
- **Hide tabs:** Embryo Transfer, IUI/DI, Lab Results, Recommendation, Outcome Data.
- **Show fields:** Anzard Cycle Type, Procedure Clinic, Cycle Nurse, number of eggs collected/frozen.
- **Hide fields:** Partner (unless partner sperm to be used later — usually N/A), Sperm Source,
  No. of Embryos to be Replaced, Male Infertility Diagnosis.

### 3. Embryo Transfer (FET)
**Key steps:** Consent & 3-Point Check → Endometrial prep (Drugs) *or* natural-cycle lining
tracking (Folliculogram) → Select embryo (Gametes) → **Embryo Thaw** → 3-Point Check →
**Embryo Transfer** → Outcome. *(No egg collection, no stimulation-for-collection, no insemination.)*
- **Show tabs:** Overview, Folliculogram (lining), Drugs (endo prep), Gametes,
  Stored Sample(s) (thaw source), Embryo Transfer, Outcome Data, Checklists.
- **Hide tabs:** Oocyte Retrieval, IUI/DI, Lab Results.
- **Show fields:** No. of Embryos to be Replaced, Expected Treatment Date (transfer date),
  Anzard Cycle Type (FET).
- **Hide fields:** Sperm Source, egg-collection fields, ovarian-stimulation fields.

### 4. VOT — Vitrified Oocyte Thaw ⚠️ *(confirm meaning)*
**Key steps (best interpretation):** Consent & 3-Point Check → Select frozen oocytes
(Gametes/Stored) → **Oocyte Thaw** → survival assessment → Semen Prep → **ICSI** → Fertilisation
Check → Embryo Culture → Transfer / Freeze → Outcome. *(No fresh OPU, no ovarian stimulation.)*
- **Show tabs:** Overview, Gametes, Stored Sample(s), Lab Results, Embryo Transfer, Outcome Data,
  Checklists; Drugs (endo prep for the transfer).
- **Hide tabs:** Oocyte Retrieval, Folliculogram (no stim), IUI/DI.
- **Show fields:** Partner (sperm source), Sperm Source, No. of Embryos to be Replaced.
- **Hide fields:** ovarian-stimulation / egg-collection fields.
> ⚠️ **Confirm with MIVF/SME:** "VOT" is a site-specific acronym. Treated here as *Vitrified Oocyte
> Thaw*. If it means something else (e.g. a donor-egg or transfer variant), adjust this template.

### 5. IUI (Intrauterine Insemination)
**Key steps:** Consent & 3-Point Check → (mild stimulation / OI Drugs *or* natural) →
Folliculogram monitoring → Trigger → **Semen Prep (andrology)** → **IUI insemination** → Outcome
(pregnancy test). *(No egg collection, no embryo culture.)*
- **Show tabs:** Overview, Folliculogram, Drugs, IUI/DI, Outcome Data, Checklists; Gametes (sperm source).
- **Hide tabs:** Oocyte Retrieval, Embryo Transfer, Lab Results, Stored Sample(s), Recommendation.
- **Show fields:** Partner, Sperm Source, Male Infertility Diagnosis, Expected Treatment Date.
- **Hide fields:** No. of Embryos to be Replaced, egg/embryo fields.

### 6. Ovulation Induction (OI)
**Key steps:** Consent → **Ovulation Induction (Drugs)** → Folliculogram monitoring → Trigger →
timed intercourse → Outcome. *(No lab procedure.)*
- **Show tabs:** Overview, Drugs, Folliculogram, Outcome Data, Checklists.
- **Hide tabs:** Oocyte Retrieval, Embryo Transfer, IUI/DI, Gametes, Lab Results, Stored Sample(s),
  Recommendation.
- **Show fields:** Female Infertility Diagnosis, Cycle Nurse, Expected Treatment Date.
- **Hide fields:** Partner/Sperm Source (unless relevant), No. of Embryos to be Replaced.

### 7. Tracking (monitoring only)
**Key steps:** (optional Consent) → Folliculogram / scan tracking → LH & trigger monitoring →
Outcome / hand-off to a treatment cycle. *(No procedure.)*
- **Show tabs:** Overview, Folliculogram, Outcome Data; Drugs (light, if any).
- **Hide tabs:** Oocyte Retrieval, Embryo Transfer, IUI/DI, Gametes, Lab Results, Stored Sample(s),
  Recommendation, Checklists (minimal).
- **Show fields:** Cycle Doctor, Cycle Nurse, Expected Date Period Started, Actual Period Date.
- **Hide fields:** Partner, Sperm Source, No. of Embryos to be Replaced, diagnosis (optional).

### 8. Thaw and Refreeze
**Key steps:** Consent & 3-Point Check → Select stored specimen (Gametes/Stored) → **Thaw** →
assessment (± **Biopsy** for PGT) → **Refreeze** → Storage allocation → Witnessing log.
- **Show tabs:** Overview, Gametes, Stored Sample(s), Checklists (witnessing).
- **Hide tabs:** Folliculogram, Drugs, Oocyte Retrieval, Embryo Transfer, IUI/DI, Lab Results,
  Outcome Data, Recommendation.
- **Show fields:** Storage location, consent/expiry, specimen selection.
- **Hide fields:** stimulation, diagnosis, No. of Embryos to be Replaced, Expected Treatment Date.

---

## Matrix — key lab steps × treatment type
(✓ = applies · — = not applicable)

| Step ↓ / Type → | IVF | Egg Only | Emb Transfer | VOT | IUI | OI | Tracking | Thaw+Refreeze |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Consent & 3-Point Check | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| Ovarian Stimulation (Drugs) | ✓ | ✓ | – | – | ± | ✓ | – | – |
| Folliculogram monitoring | ✓ | ✓ | ± | – | ✓ | ✓ | ✓ | – |
| Trigger | ✓ | ✓ | – | – | ✓ | ✓ | ± | – |
| OPU (Egg Collection) | ✓ | ✓ | – | – | – | – | – | – |
| Oocyte Thaw | – | – | – | ✓ | – | – | – | ✓ |
| Denuding / Maturity | ✓ | ✓ | – | ✓ | – | – | – | – |
| Semen Prep | ✓ | – | – | ✓ | ✓ | – | – | – |
| Insemination (IVF/ICSI) | ✓ | – | – | ✓ | – | – | – | – |
| IUI insemination | – | – | – | – | ✓ | – | – | – |
| Fertilisation Check | ✓ | – | – | ✓ | – | – | – | – |
| Embryo Culture (Day 2–7) | ✓ | – | – | ✓ | – | – | – | – |
| Biopsy (PGT) | ± | – | – | ± | – | – | – | ± |
| Embryo Thaw | ± | – | ✓ | – | – | – | – | ± |
| Embryo Transfer | ✓ | – | ✓ | ✓ | – | – | – | – |
| Freeze / Vitrify | ✓ | ✓ | – | ± | – | – | – | ✓ (refreeze) |
| Storage allocation | ✓ | ✓ | – | ± | – | – | – | ✓ |
| Outcome / Pregnancy test | ✓ | – | ✓ | ✓ | ✓ | ✓ | ± | – |

## Matrix — cycle tabs × treatment type
(S = show · H = hide)

| Tab ↓ / Type → | IVF | Egg Only | Emb Transfer | VOT | IUI | OI | Tracking | Thaw+Refreeze |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Overview | S | S | S | S | S | S | S | S |
| Folliculogram | S | S | S | H | S | S | S | H |
| Drugs | S | S | S | S | S | S | ± | H |
| Oocyte Retrieval | S | S | H | H | H | H | H | H |
| Gametes | S | S | S | S | ± | H | H | S |
| Lab Results | S | H | H | S | H | H | H | H |
| Embryo Transfer | S | H | S | S | H | H | H | H |
| IUI/DI | H | H | H | H | S | H | H | H |
| Stored Sample(s) | S | S | S | S | H | H | H | S |
| Outcome Data | S | H | S | S | S | S | ± | H |
| Recommendation | S | H | ± | ± | H | H | H | H |
| Checklists | S | S | S | S | S | S | ± | S |

## Implementation notes
- Drive visibility from the JSON in `templates/treatment-type-display-templates.json` (a
  frontend-consumable config) so tabs/fields toggle on Treatment-Type change without a rebuild.
- Seed the matching **lab events** by mapping each template's `steps[]` to
  `bcrm_eventtemplatedetail` rows and calling `EventTemplate/ApplyToCycle`.
- Keep the config as CRUD-managed reference data (managers can tune per site) — not hard-coded.
- Confirm the `bcrm_treatmenttype` option-set integer values and the exact logical names of the
  cycle form fields before wiring; the labels above come from the Treatment Cycle form.
