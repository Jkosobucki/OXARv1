# OX.ar — Treatment Planning tab (redesign) → Three-Point Check

> **Independent of OX.gp.** OX.ar only.

## Objective
Re-organise the Treatment Cycle so the embryologist **plans first, then verifies**:
**Treatment Planning** tab (consolidates today's Overview cycle info) immediately followed by the
**Three-Point Check**. Combined with the treatment-type display templates
(`06-TREATMENT-TYPE-TEMPLATES.md`), the user sees only what matters and can't start a procedure
before identity/consent is verified.

Full field lists (logical names, data type, source entity, relationship, required, confirmed?) are
in the workbook **`OXar_Treatment_Cycle_Tab_Attributes.xlsx`** — sheets *"1. Treatment Planning"*
and *"2. Three Point Check"*. This doc is the narrative.

## Tab 1 — Treatment Planning (primary entity `bcrm_treatment_cycle`)
Consolidates the current Overview into clear sections and adds a **Plan** section.

- **Cycle Information** — Treatment Name, Treatment Type (drives the template), Cycle Sequence
  Number (read-only), Treatment Billing Option, Treatment Template, Anzard Cycle Type,
  Cycle Instructions.
- **People** — Patient, Partner, Patient Group, Patient RX Group, Cycle Doctor, Cycle Nurse.
- **Clinic** — Managing Clinic, Procedure Clinic.
- **Dates** — Expected Start Date, Expected Treatment Date, Treatment Date, Expected Date Period
  Started, Actual Period Date, First Injection Date, Date of Baseline Scan.
- **Diagnosis** — Female Infertility Diagnosis, Male Infertility Diagnosis, Diagnosis Comment.
- **Plan (new)** — **Event Template** (→ `EventTemplate/ApplyToCycle` seeds the `bcrm_embryoevent`
  worklist), **Research Project** + Research Enrolled (banner), Sperm Source, Type of Cycle,
  Age at Treatment, Deferred Reason (only when deferred).

**Behaviour:** on **Treatment Type** change, apply that type's display template — show/hide the
fields and tabs listed in `06-TREATMENT-TYPE-TEMPLATES.md` / `templates/treatment-type-display-templates.json`.
Always-visible core fields never hide.

## Tab 2 — Three-Point Check (entity `bcrm_threepointcheck`, new)
The safety gate immediately after planning. Fields: Treatment Cycle, Related Event,
Patient Verified, Partner Verified, Consent Verified, Verified By, **Witness (independent — must
differ from Verified By)**, Verification Date, Status (Pending/Passed/Failed), Notes.

**Behaviour:** render the three identifiers as tick/cross; **block** the procedure tabs' actions
(OPU, insemination, transfer, freeze, thaw…) until Status = Passed with an independent witness.
The API enforces the same (returns 400 on performer == witness, and refuses a critical event moving
to In Progress without a passed check).

## Why this order
Planning → verification mirrors how an accredited lab actually starts a cycle: confirm the plan and
people, seed the day's events, then perform the mandatory identity/consent check before touching
gametes. It also removes clutter — a Tracking or OI cycle, for example, hides all the lab tabs and
shows a short planning form.

## Implementation pointers
- Frontend: `03-FRONTEND-GUIDE.md` §6c (tabs) + §5b/§5c (editing & admin).
- Backend: `bcrm_threepointcheck` service/controller per `02-API-GUIDE.md`; witness/consent rules in
  `02-API-GUIDE.md` §4; template application via `EventTemplate/ApplyToCycle`.
- Confirm the `bcrm_treatment_cycle` field logical names (marked "form" in the workbook) against the
  live Dataverse form before wiring.
