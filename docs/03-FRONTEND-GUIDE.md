# OX.ar Embryo Module — Frontend Build Guide (React)

> **Independent of OX.gp.** OX.ar `OxArFrontendReact` only. No OX.gp imports, assets, or routes.

Step-by-step React build, using this repo's actual conventions (React 17 CRA, react-router v6 in
`src/Layout Component/Layout1.js`, axios services in `src/services/`, redux-toolkit store,
bootstrap/react-bootstrap, react-toastify, react-select, sweetalert2). Do the backend first
(`../../../OxArBackendReact/docs/embryo-module/`).

---

## 1. Folder & routing setup (Phase 0)

Create a self-contained lab area so nothing bleeds into the patient portal:

```
src/
  lab/
    labTheme.css                 # design tokens (README §7)
    LabShell.js                  # top bar + witness chip + alert strip + left rail + <Outlet/>
    components/                  # WitnessChip, StatTile, StatusBadge, DevelopmentGrid, PnBoard, TankView…
    pages/
      Worklist.js  CycleOverview.js  ChecksConsent.js
      Opu.js  SemenPrep.js  Fertilisation.js
      EmbryoDevelopment.js  Transfer.js
      CryoTank.js  Inventory.js  WitnessingLog.js
  services/
    embryoApi.js                 # ALL Embryo Module axios calls live here
```

**Register routes** in `src/Layout Component/Layout1.js` alongside the existing `<Route>`s (react
-router v6 nested syntax already in use there). Add a `/lab` parent with a staff-role guard:

```jsx
// imports at top of Layout1.js
import LabShell from '../lab/LabShell';
import Worklist from '../lab/pages/Worklist';
import CycleOverview from '../lab/pages/CycleOverview';
// …one import per page

// inside <Routes> … <Route path="/">   (staff-gated)
<Route path="lab" element={<RequireStaff><LabShell/></RequireStaff>}>
  <Route index element={<Worklist/>} />
  <Route path="worklist" element={<Worklist/>} />
  <Route path="cycle/:cycleId" element={<CycleOverview/>} />
  <Route path="checks/:cycleId" element={<ChecksConsent/>} />
  <Route path="opu/:cycleId" element={<Opu/>} />
  <Route path="semen/:cycleId" element={<SemenPrep/>} />
  <Route path="fert/:cycleId" element={<Fertilisation/>} />
  <Route path="develop/:cycleId" element={<EmbryoDevelopment/>} />
  <Route path="transfer/:cycleId" element={<Transfer/>} />
  <Route path="tank" element={<CryoTank/>} />
  <Route path="inventory/:tankId" element={<Inventory/>} />
  <Route path="witnessing" element={<WitnessingLog/>} />
</Route>
```

`LabShell` renders the chrome + `<Outlet/>` (react-router v6). `RequireStaff` is a small wrapper
that checks the signed-in user's role from the redux `UserReducer` / B2C claims and redirects
patients away. (If the product owner picks a *separate* staff app — README §3 option 2 — this
route tree becomes that app's root instead; everything else in this guide is unchanged.)

Load the fonts once (index.html `<head>` or an `@import` in `labTheme.css`):
`Montserrat:500,600,700,800` · `Lato:300,400,700` · `JetBrains Mono:400,500,600`.

---

## 2. The API service (mirror the existing service style)

`src/services/embryoApi.js` — one module, same axios+env pattern as `ProfileService.js`
(base URL `process.env.REACT_APP_OXAR_API_URL`, Bearer via the `src/index.js` interceptor, and
`GettheHmToken()` from `AllApiCall.js` for the `hmacauth` token before first call).

```js
import axios from "axios";
const url = process.env.REACT_APP_OXAR_API_URL;

// ---- Planning ----
export const getWorklist = (siteId, from, to, status, eventType) =>
  axios.get(`${url}api/EmbryoEvent/GetWorklist`, { params: { siteId, from, to, status, eventType } })
       .then(r => r.data);
export const getCycleEvents = (cycleId) =>
  axios.get(`${url}api/EmbryoEvent/GetByCycleId?id=${cycleId}`).then(r => r.data);
export const createEvent = (dto) =>
  axios.post(`${url}api/EmbryoEvent/Create`, dto).then(r => r.data);

export const getActiveTemplates = () =>
  axios.get(`${url}api/EventTemplate/GetActive`).then(r => r.data);
export const applyTemplateToCycle = (dto) =>
  axios.post(`${url}api/EventTemplate/ApplyToCycle`, dto).then(r => r.data);

export const getConsentChecklist = (cycleId) =>
  axios.get(`${url}api/ConsentChecklist/GetByCycleId?id=${cycleId}`).then(r => r.data);
export const createThreePointCheck = (dto) =>
  axios.post(`${url}api/ThreePointCheck/Create`, dto).then(r => r.data);

// ---- Procedures ----
export const createOpu = (dto)        => axios.post(`${url}api/Opu/Create`, dto).then(r => r.data);
export const createSemenPrep = (dto)  => axios.post(`${url}api/SemenPrep/Create`, dto).then(r => r.data);
export const createInsemination = (d) => axios.post(`${url}api/Insemination/Create`, d).then(r => r.data);
export const createFertilisation = (d)=> axios.post(`${url}api/Assessment/CreateFertilisation`, d).then(r => r.data);

// ---- Lab culture ----
export const getEmbryosByCycle = (cycleId) =>
  axios.get(`${url}api/Specimen/GetEmbryosByCycle?id=${cycleId}`).then(r => r.data);
export const getSpecimenDetails = (specimenId) =>
  axios.get(`${url}api/Specimen/GetDetailsBySpecimen?id=${specimenId}`).then(r => r.data);
export const createDevelopment = (dto) =>
  axios.post(`${url}api/Assessment/CreateDevelopment`, dto).then(r => r.data);
export const createTransfer = (dto) =>
  axios.post(`${url}api/EmbryoTransfer/Create`, dto).then(r => r.data);

// ---- Cryo ----
export const getTanksBySite = (siteId) => axios.get(`${url}api/CryoTank/GetBySite?siteId=${siteId}`).then(r=>r.data);
export const getTankInventory = (tankId) => axios.get(`${url}api/CryoTank/GetInventory?tankId=${tankId}`).then(r=>r.data);
export const createFreeze = (dto) => axios.post(`${url}api/FreezeEvent/Create`, dto).then(r=>r.data);
export const createThaw   = (dto) => axios.post(`${url}api/ThawEvent/Create`, dto).then(r=>r.data);
export const createBiopsy = (dto) => axios.post(`${url}api/Biopsy/Create`, dto).then(r=>r.data);

// ---- KPIs / reports ----
export const getCycleKpis = (cycleId) => axios.get(`${url}api/Report/CycleKpis?id=${cycleId}`).then(r=>r.data);
```

Handle errors with `react-toastify` toasts (already a dependency), matching the portal's UX.

---

## 3. Shared components

### 3.1 `WitnessChip` (top bar, always visible)
Two overlapping avatars (performer + independent witness) + a state pill. Green when both present
and differ; amber when awaiting witness; red if the same person is set for both.
```jsx
export default function WitnessChip({ performer, witness }) {
  const ok = performer && witness && performer.id !== witness.id;
  const same = performer && witness && performer.id === witness.id;
  const cls = same ? 'witness-chip bad' : ok ? 'witness-chip' : 'witness-chip warn';
  return (
    <div className={cls}>
      <span className="lbl">Witness</span>
      <span className="pair">
        <span className="wdot a">{initials(performer)}</span>
        <span className="wdot b">{initials(witness)}</span>
      </span>
      <span className="ok">{ok ? '✓ verified' : same ? 'same person!' : 'awaiting'}</span>
    </div>
  );
}
```

### 3.2 `StatusBadge`
Maps a clinical status to a colour token (green/amber/red/purple/slate) + a dot + label. Never
colour-only — always render the label text too.

### 3.3 `StatTile` (KPI with benchmark band)
Accent bar + big mono value + benchmark caption. Colour by band from the KPI service:
`>= benchmarkLow` → green, within competence → amber, below competence → red. Example for
blastocyst rate: benchmark 44–80%, competence 25–60%.

### 3.4 `PnBoard` (fertilisation)
A grid of zygote cards; 2PN → green, 0/1/3PN/abnormal → red, assessed at the 16–18h check. Read
from `getSpecimenDetails` (the `bcrm_eggdetail` rows).

### 3.5 `DevelopmentGrid` (signature view — build to highest fidelity)
Rows = embryos (mono specimen ID + provenance: source oocyte + sperm). Columns = **Day 0 → Day 7**.
Each cell shows stage + grade + fragmentation:
- Cleavage cell: `cellCount`-cell + `frag%` (red if frag high).
- Blastocyst cell: Gardner `expansion + ICM + TE` rendered as e.g. `4AA` in mono; expansion drives
  a small fill indicator.
- Arrested embryos: greyed/red, no further columns.
- Right-most **fate** column: badge from `bcrm_embryofate` (Continue/Freeze/Transfer/Biopsy/
  Discard/Donate). Selecting a fate opens the matching action dialog (Transfer/Freeze/Biopsy).
```jsx
// grade rendering helper — keep components separate so 4AA sorts correctly
const gardner = (d) => d.expansion ? `${d.expansion}${d.icm||''}${d.te||''}` : '—';
```

---

## 4. Page-by-page recipes

Each page: header (eyebrow + title + actions), then content. Pull `:cycleId`/`:tankId` from
`useParams()`. Load in `useEffect`; keep local state; write via `embryoApi`; toast on success/error.

| Page | Loads | Key interactions | Rules to surface |
|---|---|---|---|
| **Worklist** | `getWorklist` | Filter chips (date range / status / event type), sort by cycle/type; row → cycle | Outstanding vs completed counts (M18-003) |
| **CycleOverview** | `getCycleEvents` + `getCycleKpis` + specimens | Event timeline, KPI tiles (Vienna bands), gamete/embryo lists, research banner | KPI band colours; research/allergy/consent alert strip |
| **ChecksConsent** | `getConsentChecklist` | Consent/screening/approval toggles; **3-point check dialog** with independent witness picker | **Block procedure start until check Passed**; witness ≠ performer |
| **Opu** | cycle + doctor/embryologist pickers | Record follicles/eggs, MII/MI/GV split; creates oocyte specimens w/ seq# | MII count drives ICSI eligibility |
| **SemenPrep** | cycle | Source (partner/donor/surgical), fresh/frozen, volume/count/motility/morphology, qty used; if frozen → mark owner straw thawed | Frozen sample links to `bcrm_egg` |
| **Fertilisation** | `getSpecimenDetails` | Denuding maturity → ICSI eligibility; Insemination (IVF/ICSI); **PnBoard** 1st & 2nd check | 2PN normal; others flagged |
| **EmbryoDevelopment** | `getEmbryosByCycle` + details | **DevelopmentGrid** Day 2–7; set daily grade/frag/stage/fate | Carry-over rule; arrested styling |
| **Transfer** | embryos marked transfer | Fresh/FET, catheter, order, doctor+embryologist+witness, outcome | Witness required |
| **CryoTank** | `getTanksBySite` | Graphical tank cards w/ usage %; status (OK/Low N₂/Alarm); click → inventory | Capacity vs usage colour |
| **Inventory** | `getTankInventory` | Table of patient/material/position/freeze date/status; canister filter | Derived over `bcrm_egg` |
| **WitnessingLog** | 3-point checks + event witnesses | Chronological chain-of-custody record; export | Governance/audit |

Freeze / Thaw / Biopsy are **dialogs** launched from the Development grid fate action or the Cryo
pages (not standalone routes), calling `createFreeze/createThaw/createBiopsy`.

---

## 5. Wiring the safety gates in the UI (mirror the server rules)

The server enforces these (`02-API-GUIDE.md` §4); the UI must *reflect* them so the block is
obvious, not a surprise 400:

1. **3-point check gate.** On a procedure page, if no Passed `ThreePointCheck` exists for the
   event, disable the "Start / Record" button and show an inline panel listing the three
   identifiers with tick/cross (like the prototype's checks screen).
2. **Consent gate.** If `ConsentChecklist.ReadyForProcedure` is false, banner + disabled actions.
3. **Independent witness.** The witness picker must exclude the current performer; if equal, show
   the red "same person!" state on `WitnessChip` and block submit.
4. **ICSI eligibility.** Only MII oocytes selectable for an ICSI insemination.
5. **Fate carry-over.** If no terminal fate is chosen for an embryo on a culture day, it stays in
   the next day's column automatically.

Use `sweetalert2` for irreversible confirmations (discard, delete) and `react-toastify` for
outcome feedback — both already in `package.json`.

---

## 5b. Editing, adding & deleting records

Every screen is read-write. Build one reusable form pattern and reuse it everywhere.

**One form, two modes.** A form component takes `mode = 'create' | 'edit'` and an optional record.
Edit mode pre-fills from `GetById`; a single submit routes to `create*` or `update*`:
```jsx
function OpuForm({ mode, record, cycleId, onSaved }) {
  const [form, setForm] = useState(record ?? blankOpu(cycleId));
  const submit = async () => {
    try {
      if (mode === 'edit') await updateOpu(form);   // PUT …/Opu/Update  (sends Id + ConcurrencyToken)
      else                  await createOpu(form);   // POST …/Opu/Create
      toast.success(mode === 'edit' ? 'Updated' : 'Added');
      onSaved();
    } catch (e) {
      if (e.response?.status === 409) { toast.error('Someone else edited this — reloading'); onSaved(); }
      else toast.error(e.response?.data ?? 'Save failed');
    }
  };
  // …fields bound to form/setForm…
}
```
Add the matching `update*` / `delete*` calls to `src/services/embryoApi.js` next to each `create*`
(they already imply the backend `GetById`/`Update`/`Delete` from `04-CRUD-AND-EDITING.md`):
```js
export const getOpu    = (id)  => axios.get(`${url}api/Opu/GetById?id=${id}`).then(r=>r.data);
export const updateOpu = (dto) => axios.put(`${url}api/Opu/Update`, dto).then(r=>r.data);
export const deleteOpu = (id)  => axios.delete(`${url}api/Opu/Delete?id=${id}`).then(r=>r.data);
```

**"Add" buttons.** Every subgrid (events on a cycle, oocytes on an OPU, assessments on a specimen,
locations in a tank) gets an `+ Add` button opening the form in `create` mode with the parent id
pre-set.

**Inline editing in the development grid.** Clicking a grid cell turns it into a `<select>`/input
(stage, grade parts, frag %, fate). On blur/change, fire one `updateAssessment({ Id, changed,
ConcurrencyToken })` — a partial update. Keep Gardner grade as three controls
(expansion / ICM / TE) so an edit changes one part.

**Deleting is deliberate.** Use `sweetalert2` to confirm, and for regulated rows **capture a
reason** (feeds `bcrm_cancelreason`); default action is soft-retire, hard delete only for managers:
```jsx
const { value: reason } = await Swal.fire({
  title: 'Retire this record?', input: 'text', inputLabel: 'Reason (required)',
  showCancelButton: true, inputValidator: v => !v && 'A reason is required'
});
if (reason) { await cancelOpu(id, reason); toast.success('Retired'); reload(); }
```

**Concurrency.** Every editable record carries `concurrencyToken` (RowVersion) + `modifiedBy`/
`modifiedOn` from the read; send the token back on update; on **409** reload and tell the user.
Show "last edited by X at Y" so edits are transparent.

**Role-aware UI.** Hide/disable Add·Edit·Delete by role (read-only for viewers; edit for
embryologists; delete/correct for managers). The server enforces it too (403) — the UI just avoids
dead-ends.

## 5c. Reference-data admin screens (manage the "data sources")

So the lab can add/edit the lists that drive the forms **without a developer** (see
`04-CRUD-AND-EDITING.md` §6), add small admin pages under `/lab/admin/*` (manager-gated):

| Screen | Route | Manages | Service calls |
|---|---|---|---|
| Templates | `/lab/admin/templates` | Event templates + their event rows | `EventTemplate` CRUD + `AddDetail`/`UpdateDetail`/`DeleteDetail` |
| Cryo Tanks | `/lab/admin/tanks` | Tanks + capacity + locations | `CryoTank` / `CryoLocation` CRUD |
| Research Projects | `/lab/admin/research` | Projects for the cycle banner | `ResearchProject` CRUD |
| Reference lists | `/lab/admin/reference` | Sites / quality grades / biopsy types (if modelled as reference entities) | small CRUD controllers |

Each is the same list + form-in-a-modal + delete-confirm pattern as §5b. Values that are fixed
clinical option sets (maturity, PN, fate) are **not** editable here — they change via a Dataverse
solution deployment, and the admin UI should say so rather than pretend to edit them.

## 6. State & data conventions
- Prefer local component state + service calls (the portal's pattern). Use the redux store only
  for cross-cutting context (current staff user, selected cycle) if needed.
- Dates: display in clinic-local time; the API stores UTC.
- Specimen IDs, tank/goblet/straw positions, and all numeric lab values render in `.mono`.
- Keep all Embryo Module code under `src/lab/` + `src/services/embryoApi.js`. Do not modify
  existing portal components except to add the `/lab` route block in `Layout1.js`.

## 6b. Cryo Tank Management (detailed UI)

Managing cryo tanks properly needs more than a list — the lab must see capacity at a glance, drill
into the position hierarchy, and add/edit tanks and slots. Build three linked screens under
`/lab/tank*`, all manager-editable (backend: `CryoTank` + `CryoLocation` CRUD + `CryoTank/GetInventory`).

### Screens
1. **Tank wall** — `/lab/tank` : a card grid of all tanks for the site. Each card = tank name (mono),
   a **fill gauge** (`currentUsage / capacity`), and a **status pill** (OK / Low N₂ / Maintenance /
   Alarm). Colour the gauge green < 80 %, amber 80–95 %, red ≥ 95 %; a non-OK status shows red
   regardless. A `+ Add tank` button (managers) opens the tank form; each card has Edit / Deactivate.
2. **Tank detail** — `/lab/tank/:tankId` : the **position hierarchy** Canister → Goblet → Straw/Vial
   rendered as an expandable tree/grid from `CryoLocation/GetByTank`. Each slot is colour-coded by
   `storageStatus` (Occupied / Available / Reserved) and by `storageType` (Embryo / Sperm / Oocyte).
   Managers can `+ Add location`, edit, or retire slots; clicking an occupied slot shows its specimen.
3. **Inventory** — `/lab/inventory/:tankId` (or a tab on tank detail) : table from
   `CryoTank/GetInventory` — patient, specimen ID (mono), material type, freeze date, goblet/straw,
   status. Filter by canister and material type; export.

### Services (add to `embryoApi.js`)
```js
export const getTanksBySite   = (siteId) => axios.get(`${url}api/CryoTank/GetBySite?siteId=${siteId}`).then(r=>r.data);
export const getTank          = (id)     => axios.get(`${url}api/CryoTank/GetById?id=${id}`).then(r=>r.data);
export const createTank       = (dto)    => axios.post(`${url}api/CryoTank/Create`, dto).then(r=>r.data);
export const updateTank       = (dto)    => axios.put(`${url}api/CryoTank/Update`, dto).then(r=>r.data);
export const deactivateTank   = (id)     => axios.delete(`${url}api/CryoTank/Delete?id=${id}`).then(r=>r.data);
export const getTankInventory = (id)     => axios.get(`${url}api/CryoTank/GetInventory?tankId=${id}`).then(r=>r.data);

export const getLocationsByTank   = (tankId) => axios.get(`${url}api/CryoLocation/GetByTank?tankId=${tankId}`).then(r=>r.data);
export const getAvailableSlots    = (tankId) => axios.get(`${url}api/CryoLocation/GetAvailableByTank?tankId=${tankId}`).then(r=>r.data);
export const createLocation       = (dto)    => axios.post(`${url}api/CryoLocation/Create`, dto).then(r=>r.data);
export const updateLocation       = (dto)    => axios.put(`${url}api/CryoLocation/Update`, dto).then(r=>r.data);
export const setLocationStatus    = (id, statusValue) => axios.put(`${url}api/CryoLocation/SetStatus?id=${id}&statusValue=${statusValue}`).then(r=>r.data);
export const deleteLocation       = (id)     => axios.delete(`${url}api/CryoLocation/Delete?id=${id}`).then(r=>r.data);
```

### Components
- `TankCard` (gauge + status pill), `TankWall` (grid + Add), `TankForm` (create/edit — name, site,
  capacity, status), `LocationTree` (canister→goblet→straw expandable), `LocationForm`,
  `InventoryTable`. All use the create/edit/delete pattern in §5b (concurrency 409, delete-with-reason,
  role-gated) — writes are manager/admin only.

### Ties to freeze/thaw
The freeze dialog calls `getAvailableSlots(tankId)` to pick a free slot, then the freeze service
flips it Occupied (`CryoLocation/SetStatus`) and stamps the specimen; thaw frees it (→ Available).
So the tank views always reflect live occupancy.

## 6c. Treatment Planning tab → Three-Point Check

The Treatment Cycle is being re-organised so the embryologist plans, then verifies:
1. **Treatment Planning** (first tab) — consolidates today's Overview cycle info into clear sections
   (Cycle Information · People · Clinic · Dates · Diagnosis · **Plan**), and adds a Plan section:
   pick the **Event Template** (seeds the worklist) and optional **Research Project**. The selected
   **Treatment Type** immediately applies its display template (§treatment-type templates,
   `06-TREATMENT-TYPE-TEMPLATES.md`) — hiding irrelevant fields/tabs.
2. **Three-Point Check** (next tab) — the identity/consent gate (`bcrm_threepointcheck`) that must
   **pass with an independent witness** before any critical procedure. Render the three identifiers
   with tick/cross and block downstream tabs' actions until passed.

Field-by-field content for both tabs (and every other tab) is in the workbook
`OXar_Treatment_Cycle_Tab_Attributes.xlsx` and `07-TREATMENT-PLANNING-TAB.md`.

## 7. Verify each slice
1. `npm start` (CRA), sign in with a staff account, open `/lab`.
2. Confirm the screen loads from the live OXAR endpoint (Network tab → `…/api/…`, no mock array).
3. Exercise the write path; confirm the row appears on reload (round-trip).
4. Try to break a gate (same witness=performer; procedure without a passed 3-point check) — the UI
   must block and the API must 400.
5. Brand check against the prototype: fonts, aqua, mono values, badge colours.

Definition of done per slice is in the master guide (`README.md` §9).
