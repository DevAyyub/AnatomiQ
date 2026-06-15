# AnatomiQ — Planning Documents

This folder holds the twelve planning artifacts that define AnatomiQ's vision, architecture, content, safety, performance, operations, UX, demo strategy, build environment, and decision history. Read `AnatomiQ_Project_Index.md` first when starting any session or returning after a break — it is the map for all the others.

Copy your existing planning documents into this folder so they live alongside the code and travel with every clone.

## The twelve documents

| File | What it owns |
|---|---|
| `AnatomiQ_Project_Index.md` | The map + execution plan. Read first. |
| `AnatomiQ_Project_Overview.docx` | Vision, philosophy, design decisions. |
| `AnatomiQ_Features_Document.docx` | All 24 features with full specs, dependencies, AI/AR roles, fallbacks. |
| `AnatomiQ_Project_Instructions.md` | Claude Project system prompt / coding standards. |
| `AnatomiQ_Data_Schemas.md` | Organ + disease JSON schemas, T2D worked example. |
| `AnatomiQ_AI_System_Prompts.md` | Production AI prompts with safety rules + injection defenses. |
| `AnatomiQ_Performance_And_Models.md` | Snapdragon 778G perf budget + Z-Anatomy sourcing guide. |
| `AnatomiQ_Operations_And_Planning.md` | AI provider decision, risk register, chat templates. |
| `AnatomiQ_Phase1_Medical_Content.md` | Organ list, HTN + CKD cascades, test fixtures. |
| `AnatomiQ_UX_And_Identity.md` | UX architecture + visual identity constraints. |
| `AnatomiQ_Demo_Run_Of_Show.md` | 4:30 demo script, fallback playbook, checklist. |
| `AnatomiQ_Build_Environment.md` | Workstation, Git LFS, localization, service-access pattern. |

Plus the living log, which is updated continuously and should sit at the repo root or here:

| File | What it owns |
|---|---|
| `bugs_and_decisions.md` | Running log of decisions, bugs solved, patterns discovered. |

## Conventions

- When a decision changes, update the document that *owns* the topic, then add a pointer entry to `bugs_and_decisions.md`.
- `.docx` sources are tracked but treated as binary; if you convert any to Markdown for easier diffing, keep the `.docx` as the source of record until you decide otherwise.
- After each phase completes, update the "What's Done" section in `AnatomiQ_Project_Index.md`.
