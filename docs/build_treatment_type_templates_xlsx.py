#!/usr/bin/env python3
"""OXar_Treatment_Type_Templates.xlsx — one template-definition tab per treatment type.

This is the source-of-truth the Treatment Planning UI is driven by: for each treatment type it
defines the plan steps, which Treatment Cycle tabs show/hide, and which sections+fields are shown
(with sample values for the synthetic test couple). Mirrors the TEMPLATES object in
reference/oxar_prototype_screens.html. Independent of OX.gp.
"""
import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

DEEP="2F7D76"; AQUA="45A59D"; SOFT="EAF5F3"; INK="221F20"; WHITE="FFFFFF"
ZEBRA="F5F8F7"; AMBER="C9821C"; PURPLE="7A5CC0"; MUTED="6C7C7A"

wb=openpyxl.Workbook()
thin=Side(style="thin",color="D9E4E2"); border=Border(left=thin,right=thin,top=thin,bottom=thin)
hf=Font(bold=True,color=WHITE,size=11); hfill=PatternFill("solid",fgColor=DEEP)
title=Font(bold=True,color=INK,size=15); sub=Font(italic=True,color=MUTED,size=10)
lab=Font(bold=True,color=DEEP,size=10); wrap=Alignment(vertical="top",wrap_text=True); mid=Alignment(vertical="center")

PATIENT="Priya Sample (DEMO) · NHR-0042 · F · DOB 12 Mar 1991"
PARTNER="Alex Sample (DEMO) · NHR-0043 · M · DOB 04 Aug 1988"

ALL_TABS=["Planning","Folliculogram","Drugs","Oocyte Retrieval","Semen","Lab Results",
          "Embryo Transfer","IUI/DI","Gametes","Stored Sample","Outcome Data","Checklists"]

# (Section, [ (Field, Sample value, Required) ])
def f(l,v,r=""): return (l,v,r)

TEMPLATES={
"IVF":{"purpose":"Full stimulated cycle — collect eggs, fertilise, transfer / freeze.","partner":"Yes","note":"",
 "steps":["Consent & 3PC","Stimulation","Folliculogram","Trigger","OPU","Semen Prep","Insemination (ICSI)","Fertilisation","Culture D2-7","Transfer","Freeze surplus","Outcome"],
 "hide":["IUI/DI"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","IVF — Antagonist protocol","Yes"),f("Treatment Type","IVF","Yes"),f("Cycle Sequence No","CY-1183"),f("Billing Option","Self-funded","Yes"),f("ANZARD Cycle Type","Stimulated — fresh"),f("Treatment Template","Standard ICSI, Day 5")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Partner",PARTNER),f("Cycle Doctor","Dr M. Osei","Yes"),f("Cycle Nurse","R. Nolan"),f("Patient RX Group","Group B","Yes")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("Procedure Clinic","OX.ar City Lab"),f("Expected Start","14 Jul 2026"),f("First Injection","16 Jul 2026"),f("Expected OPU","28 Jul 2026")]),
  ("Infertility Diagnosis",[f("Female Diagnosis","Tubal factor"),f("Male Diagnosis","Mild oligospermia"),f("Comment","—")]),
  ("Plan",[f("Event Template","IVF Standard"),f("Research Project","PROTO time-lapse study"),f("Sperm Source","Partner (fresh)"),f("Insemination Method","ICSI"),f("No. Embryos to Replace","1")]),
 ]},
"Egg Only":{"purpose":"Egg freezing / fertility preservation — collect & vitrify oocytes (no fertilisation).","partner":"No","note":"",
 "steps":["Consent & 3PC","Stimulation","Folliculogram","Trigger","OPU","Denuding / MII","Vitrify Oocytes","Storage"],
 "hide":["Embryo Transfer","IUI/DI","Lab Results","Outcome Data"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","Egg Freezing","Yes"),f("Treatment Type","Egg Only","Yes"),f("Cycle Sequence No","CY-1184"),f("Billing Option","Self-funded","Yes"),f("ANZARD Cycle Type","Stimulated — freeze all"),f("Treatment Template","Egg Freezing")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Cycle Doctor","Dr M. Osei","Yes"),f("Cycle Nurse","R. Nolan"),f("Patient RX Group","Group B","Yes")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("Procedure Clinic","OX.ar City Lab"),f("Expected Start","14 Jul 2026"),f("First Injection","16 Jul 2026"),f("Expected OPU","28 Jul 2026")]),
  ("Indication",[f("Reason","Elective fertility preservation"),f("AMH Validated","Yes — 18.2 pmol/L")]),
  ("Plan",[f("Event Template","Egg Freezing"),f("Research Project","—"),f("Freeze Method","Vitrification"),f("Target eggs to store","≥ 10 MII")]),
 ]},
"Embryo Transfer":{"purpose":"Frozen embryo transfer (FET) — thaw a stored embryo and transfer.","partner":"Yes","note":"",
 "steps":["Consent & 3PC","Endometrial Prep / Lining","Select Embryo","Embryo Thaw","3PC","Transfer","Outcome"],
 "hide":["Oocyte Retrieval","IUI/DI","Lab Results"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","FET — Medicated","Yes"),f("Treatment Type","Embryo Transfer","Yes"),f("Cycle Sequence No","CY-1185"),f("Billing Option","Medicare + gap","Yes"),f("ANZARD Cycle Type","FET"),f("Treatment Template","FET (Frozen Embryo Transfer)")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Partner",PARTNER),f("Cycle Doctor","Dr M. Osei","Yes"),f("Cycle Nurse","R. Nolan"),f("Patient RX Group","Group B","Yes")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("Lining Check","22 Jul 2026"),f("Expected Transfer","29 Jul 2026")]),
  ("Plan",[f("Event Template","FET"),f("Endometrial Prep","Medicated (HRT)"),f("Selected Embryo","EMB-1042 · Blast 4AA · Day 5"),f("No. Embryos to Replace","1")]),
 ]},
"VOT":{"purpose":"Vitrified Oocyte Thaw — thaw frozen eggs → ICSI → culture → transfer.","partner":"Yes","note":"VOT is a site-specific acronym — confirm exact meaning with MIVF/SME.",
 "steps":["Consent & 3PC","Select Frozen Eggs","Oocyte Thaw","Survival Check","Semen Prep","ICSI","Fertilisation","Culture","Transfer / Freeze","Outcome"],
 "hide":["Oocyte Retrieval","Folliculogram","IUI/DI"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","VOT — thaw & ICSI","Yes"),f("Treatment Type","VOT","Yes"),f("Cycle Sequence No","CY-1186"),f("Billing Option","Self-funded","Yes"),f("ANZARD Cycle Type","Thaw oocytes"),f("Treatment Template","Vitrified Oocyte Thaw")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Partner",PARTNER),f("Cycle Doctor","Dr M. Osei","Yes"),f("Patient RX Group","Group B","Yes")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("Expected Thaw","26 Jul 2026"),f("Expected Transfer","31 Jul 2026")]),
  ("Plan",[f("Event Template","VOT"),f("Frozen Oocytes to Thaw","6 (MII)"),f("Sperm Source","Partner (fresh)"),f("Insemination Method","ICSI"),f("No. Embryos to Replace","1")]),
 ]},
"IUI":{"purpose":"Intrauterine insemination — prepared sperm placed in the uterus (no egg collection).","partner":"Yes","note":"",
 "steps":["Consent & 3PC","OI / Natural","Folliculogram","Trigger","Semen Prep","IUI Insemination","Outcome"],
 "hide":["Oocyte Retrieval","Embryo Transfer","Lab Results","Stored Sample"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","IUI — Natural cycle","Yes"),f("Treatment Type","IUI","Yes"),f("Cycle Sequence No","CY-1187"),f("Billing Option","Self-funded","Yes"),f("ANZARD Cycle Type","IUI"),f("Treatment Template","IUI")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Partner",PARTNER),f("Cycle Doctor","Dr M. Osei","Yes"),f("Cycle Nurse","R. Nolan"),f("Patient RX Group","Group A","Yes")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("LH Surge","24 Jul 2026"),f("Expected Insemination","25 Jul 2026")]),
  ("Infertility Diagnosis",[f("Female Diagnosis","Unexplained"),f("Male Diagnosis","Normal parameters")]),
  ("Plan",[f("Event Template","IUI"),f("Sperm Source","Partner (prepared)"),f("Prepared Motile Count","18 M/mL"),f("Trigger","hCG")]),
 ]},
"Ovulation Induction":{"purpose":"Drug-induced ovulation + timed intercourse (monitoring only, no lab procedure).","partner":"No","note":"",
 "steps":["Consent","OI Drugs","Folliculogram","Trigger","Timed Intercourse","Outcome"],
 "hide":["Oocyte Retrieval","Embryo Transfer","IUI/DI","Gametes","Lab Results","Stored Sample"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","Ovulation Induction — Letrozole","Yes"),f("Treatment Type","Ovulation Induction","Yes"),f("Cycle Sequence No","CY-1188"),f("Billing Option","Self-funded","Yes"),f("Treatment Template","Ovulation Induction")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Cycle Doctor","Dr M. Osei","Yes"),f("Cycle Nurse","R. Nolan")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("Baseline Scan","14 Jul 2026"),f("Day-10 Scan","24 Jul 2026")]),
  ("Infertility Diagnosis",[f("Female Diagnosis","PCOS — anovulation")]),
  ("Plan",[f("Drug","Letrozole 5 mg (days 3-7)"),f("Monitoring","Serial follicle scans"),f("Trigger","hCG when lead follicle ≥ 18 mm")]),
 ]},
"Tracking":{"purpose":"Cycle monitoring / scan tracking only (no procedure).","partner":"No","note":"",
 "steps":["Folliculogram Tracking","LH & Trigger Monitoring","Outcome / Hand-off"],
 "hide":["Oocyte Retrieval","Embryo Transfer","IUI/DI","Gametes","Lab Results","Stored Sample","Checklists"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","Cycle Tracking","Yes"),f("Treatment Type","Tracking","Yes"),f("Cycle Sequence No","CY-1189"),f("Treatment Template","Cycle Tracking")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Cycle Doctor","Dr M. Osei"),f("Cycle Nurse","R. Nolan")]),
  ("Clinic & Dates",[f("Managing Clinic","OX.ar City (DEMO)"),f("Period Started","10 Jul 2026"),f("Actual Period Date","10 Jul 2026")]),
  ("Plan",[f("Monitoring","Serial scans + LH"),f("Hand-off","to IUI / IVF if indicated")]),
 ]},
"Thaw and Refreeze":{"purpose":"Retrieve a stored specimen → thaw → (± biopsy) → refreeze → storage.","partner":"Yes","note":"",
 "steps":["Consent & 3PC","Select Specimen","Thaw","Assess (± Biopsy)","Refreeze","Storage","Witnessing"],
 "hide":["Folliculogram","Drugs","Oocyte Retrieval","Embryo Transfer","IUI/DI","Lab Results","Outcome Data"],
 "sections":[
  ("Cycle Information",[f("Treatment Name","Thaw & Refreeze — PGT","Yes"),f("Treatment Type","Thaw and Refreeze","Yes"),f("Cycle Sequence No","CY-1190"),f("Treatment Template","Thaw and Refreeze")]),
  ("People",[f("Patient",PATIENT,"Yes"),f("Partner",PARTNER),f("Embryologist","A. Kaur","Yes"),f("Witness","R. Nolan","Yes")]),
  ("Specimen & Storage",[f("Specimen","EMB-1043 · Blast 3BB"),f("Reason","Biopsy for PGT-A"),f("Refreeze Method","Vitrification"),f("Storage Location","TANK-01 · C1 · G2 · S5")]),
 ]},
}

def style_header(ws,row,cols,widths):
    for j,c in enumerate(cols,1):
        cell=ws.cell(row,j,c); cell.font=hf; cell.fill=hfill; cell.border=border; cell.alignment=wrap
        ws.column_dimensions[get_column_letter(j)].width=widths[j-1]

# ---- Index sheet ----
ix=wb.active; ix.title="Index"; ix.sheet_view.showGridLines=False
ix["A1"]="OX.ar — Treatment-Type Templates"; ix["A1"].font=Font(bold=True,size=16,color=INK)
ix["A2"]=("One tab per treatment type = the template definition that drives the Treatment Planning UI. "
          "Change the treatment type and the planning page shows exactly these sections/fields/tabs/steps. "
          "Synthetic test couple. Independent of OX.gp.")
ix["A2"].font=sub; ix.merge_cells("A2:E2"); ix["A2"].alignment=wrap; ix.row_dimensions[2].height=44
style_header(ix,4,["#","Treatment Type","Purpose","Uses partner?","Note"],[5,24,52,14,40])
for i,(t,tpl) in enumerate(TEMPLATES.items()):
    r=5+i
    vals=[str(i+1),t,tpl["purpose"],tpl["partner"],tpl["note"] or "—"]
    for j,v in enumerate(vals,1):
        c=ix.cell(r,j,v); c.alignment=wrap; c.border=border
        if r%2==0: c.fill=PatternFill("solid",fgColor=ZEBRA)
ix.freeze_panes="A5"
# test couple note
tr=5+len(TEMPLATES)+2
ix.cell(tr,1,"Test couple (synthetic):").font=Font(bold=True)
ix.cell(tr+1,1,"Patient — "+PATIENT); ix.cell(tr+2,1,"Partner — "+PARTNER)

# ---- one sheet per type ----
for t,tpl in TEMPLATES.items():
    ws=wb.create_sheet(t[:31]); ws.sheet_view.showGridLines=False
    ws["A1"]=t+" — Template Definition"; ws["A1"].font=title
    ws["A2"]=tpl["purpose"]; ws["A2"].font=sub; ws.merge_cells("A2:D2"); ws["A2"].alignment=wrap
    ws["A3"]="Uses partner: "+tpl["partner"]+("   ·   ⚠ "+tpl["note"] if tpl["note"] else "")
    ws["A3"].font=Font(size=10,color=(AMBER if tpl["note"] else MUTED))
    ws.merge_cells("A3:D3"); ws["A3"].alignment=wrap
    ws["A5"]="Plan steps (seeds the worklist):"; ws["A5"].font=lab
    ws["A6"]="  →  ".join(tpl["steps"]); ws.merge_cells("A6:D6"); ws["A6"].alignment=wrap
    ws.row_dimensions[6].height=30
    # fields table
    hr=8
    style_header(ws,hr,["Section","Field shown","Sample value (test couple)","Required"],[24,28,50,12])
    r=hr+1
    for sec,fields in tpl["sections"]:
        for k,(fl,val,req) in enumerate(fields):
            ws.cell(r,1,sec if k==0 else "").alignment=wrap
            ws.cell(r,2,fl).alignment=wrap
            ws.cell(r,3,val).alignment=wrap
            ws.cell(r,4,req or "").alignment=Alignment(horizontal="center",vertical="top")
            for j in range(1,5):
                cell=ws.cell(r,j); cell.border=border
                if r%2==0: cell.fill=PatternFill("solid",fgColor=ZEBRA)
                if j==1 and k==0: cell.font=Font(bold=True,color=DEEP)
            r+=1
    # tabs table
    r+=1
    ws.cell(r,1,"Cycle tab").font=hf; ws.cell(r,1).fill=hfill; ws.cell(r,1).border=border
    ws.cell(r,2,"Show / Hide").font=hf; ws.cell(r,2).fill=hfill; ws.cell(r,2).border=border
    r+=1
    for tab in ALL_TABS:
        shown = tab not in tpl["hide"]
        ws.cell(r,1,tab).border=border
        c=ws.cell(r,2,"SHOW" if shown else "HIDE"); c.border=border
        c.font=Font(bold=True,color=(DEEP if shown else "CF4A4A"),size=10)
        if not shown: c.fill=PatternFill("solid",fgColor="FBE9E9")
        else: c.fill=PatternFill("solid",fgColor=SOFT)
        r+=1
    ws.freeze_panes="A9"

wb.save("/workspace/oxarv1/docs/OXar_Treatment_Type_Templates.xlsx")
print("saved; sheets:", wb.sheetnames)
