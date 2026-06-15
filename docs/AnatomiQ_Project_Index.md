# AnatomiQ — Project Index & Execution Plan

> **What this is:** The map for the AnatomiQ planning artifacts and the concrete next steps to begin execution. Read this first when starting any new chat or returning to the project after a break.
>
> **Status:** Planning phase complete + gap-fill rounds complete. 12 documents define vision, architecture, content, safety, performance, operations, UX, demo strategy, build environment, and risk. Ready to begin scaffolding.

---

## The Twelve Documents

```
AnatomiQ_Project_Overview.docx          ← vision, philosophy, design decisions
AnatomiQ_Features_Document.docx         ← all 24 features with full specifications
AnatomiQ_Project_Instructions.md        ← Claude Project system prompt
AnatomiQ_Data_Schemas.md                ← organ + disease JSON schemas + T2D example
AnatomiQ_AI_System_Prompts.md           ← production-ready AI prompts with safety rules
AnatomiQ_Performance_And_Models.md      ← Snapdragon 778G budget + Z-Anatomy guide
AnatomiQ_Operations_And_Planning.md     ← AI provider, risk register, chat templates
AnatomiQ_Phase1_Medical_Content.md      ← organ list, HTN + CKD cascades, test fixtures
AnatomiQ_UX_And_Identity.md             ← UX architecture + visual identity constraints
AnatomiQ_Demo_Run_Of_Show.md            ← 4:30 demo script, fallback playbook, checklist
AnatomiQ_Build_Environment.md           ← workstation, Git LFS, localization, services
bugs_and_decisions.md                   ← living log of decisions, bugs, patterns
```

---

## How They Relate

```
                   Project Overview
                   (vision — read first)
                          │
                          ▼
                   Features Document
                   (what gets built)
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
   Data Schemas      AI Prompts       Performance &
   (CORE-005,        (CORE-006        Models
    CORE-008         prompts +        (CORE-002 +
    technical)       safety)          CORE-007 limits)
        │                 │                 │
        ▼                 │                 │
  Phase1 Medical          │                 │
  Content                 │                 │
  (CORE-008               │                 │
  data, medical           │                 │
  reviewer batch)         │                 │
        │                 │                 │
        └────────┬────────┴────────┬────────┘
                 ▼                 ▼
              UX & Identity    Project Instructions
              (UI chats)       (how Claude builds)
                 │                 │
                 └────────┬────────┘
                          ▼
                  Build Environment
                  (workstation, Git LFS,
                  localization, services —
                  read before scaffold)
                          │
                          ▼
                   Demo Run-of-Show
                   (scope filter for everything)
                          │
                          ▼
                   Operations & Planning
                   (provider, risks, templates)
                          │
                          ▼
                   bugs_and_decisions.md
                   (running log — updated forever)
```

### When to consult which document

| If you need to know... | Read |
|---|---|
| What is AnatomiQ? Why does it exist? | Project Overview |
| What features exist and what they do | Features Document |
| How a specific feature should be built | Features Document + Project Instructions |
| What data shape an organ or disease has | Data Schemas |
| What the actual organs and diseases are | Phase 1 Medical Content |
| What the AI should say in feature X | AI System Prompts |
| Whether feature Y will run on the device | Performance & Models |
| Where to get 3D anatomy models | Performance & Models, Part B |
| Which AI provider to use | Operations & Planning, Part A |
| What can go wrong with the project | Operations & Planning, Part B |
| How to start a debug or feature chat | Operations & Planning, Part C |
| How is the app navigated | UX & Identity, Part B |
| What does the body model look like | UX & Identity, Part F.5 |
| What does the demo actually show | Demo Run-of-Show |
| Should I build feature Z? | Demo Run-of-Show, Part F (scope filter) |
| What do I upload per chat / how does Claude see my code | Prompt Guide, "File management" section |
| Which JDK / NDK / SDK versions to install | Build Environment, Part A |
| How to set up Git LFS correctly | Build Environment, Part B |
| How to handle UI strings / localization | Build Environment, Part C |
| How features access cross-cutting services | Build Environment, Part D |
| Privacy / data handling story | Build Environment, Part E.1 |
| What was previously decided about X | bugs_and_decisions.md |

---

## Document Coverage Map

| Topic | Overview | Features | Instructions | Schemas | Phase 1 | AI Prompts | Performance | UX | Demo | Build Env | Operations | Bugs |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Vision & philosophy | ★ | | | | | | | | | | | |
| Pillar definitions | ★ | ★ | ◆ | | | | | | | | | |
| Feature specs | ◆ | ★ | ◆ | | | | | | | | | |
| Coding standards | | | ★ | | | | | | | ◆ | | |
| Tech stack | ◆ | | ★ | | | | ◆ | | | ◆ | | ◆ |
| Build phases | ◆ | ★ | ◆ | | | | | | ◆ | | | |
| Disease cascades data shape | | | | ★ | ◆ | | | | | | | |
| Disease cascades content | | | | ◆ | ★ | | | | | | | |
| Organ data shape | | | | ★ | ◆ | | | | | | | |
| Organ data content | | | | | ★ | | | | | | | |
| AI safety rules | ◆ | | ◆ | | | ★ | | | | | | |
| AI prompts | | | | | | ★ | | | | | | |
| Performance limits | | | | | | | ★ | | | | | |
| 3D model sources | | | | | | | ★ | ◆ | | | | |
| AI provider | | | | | | | | | | | ★ | ◆ |
| Project risks | | | | | | | | | | | ★ | ◆ |
| Chat templates | | | | | | | | | | | ★ | |
| UX navigation | | | | | | | | ★ | | | | |
| Visual identity | | | | | | | | ★ | | | | |
| Demo strategy | | | | | | | | | ★ | | ◆ | |
| Workstation setup | | | ◆ | | | | | | | ★ | | ◆ |
| Git LFS strategy | | | | | | | | | | ★ | | ◆ |
| Localization | | | ◆ | | | | | ◆ | | ★ | | ◆ |
| Service access pattern | | | ◆ | | | | | | | ★ | | ◆ |
| Privacy / data handling | | | | | | | | ◆ | | ★ | | |
| Decision log | | | | | | | | | | | | ★ |

★ = primary owner of this topic · ◆ = referenced or summarized

When deciding something new, identify which document is the primary owner and update it there. Add an entry to bugs_and_decisions.md with a pointer.

---

## What's Done

- [x] Vision and three-pillar architecture defined
- [x] All 24 features specified with dependencies, AI/AR roles, fallbacks
- [x] Tech stack verified against current Unity/AR Foundation/Inference Engine versions
- [x] ARCore body tracking limitation identified, ATLAS-006 redesigned
- [x] Coding standards, naming conventions, async patterns documented
- [x] Testing approach defined
- [x] Disease and organ JSON schemas designed
- [x] Worked Type 2 Diabetes cascade authored (medically grounded)
- [x] Hypertension cascade authored
- [x] Chronic Kidney Disease cascade authored
- [x] Phase 1 canonical 24-node organ list defined
- [x] Test fixtures (minimal_organ_graph + minimal_disease) defined
- [x] All AI system prompts written with safety guardrails and prompt-injection defenses
- [x] Performance budget set for Snapdragon 778G
- [x] 3D model source decision made (Z-Anatomy / Open3DModel)
- [x] AI provider decision made (Claude primary + OpenAI fallback)
- [x] Provider-agnostic CORE-006 architecture designed
- [x] Risk register with 10 risks tracked
- [x] Reusable Claude Projects chat templates ready
- [x] UX navigation model and screen inventory defined
- [x] Visual identity constraints set (specifics deferred to iteration)
- [x] Demo run-of-show v0 drafted (4:30 focused on signature feature)
- [x] Failure mode playbook for live demo
- [x] Pre-demo checklist defined
- [x] Workstation setup specified (JDK 17, NDK r27c, SDK 36.0.0 via Unity Hub)
- [x] Git LFS strategy with full .gitattributes ready
- [x] Localization approach decided (Unity Localization package, key naming)
- [x] Cross-cutting service access pattern decided (ServiceRegistry SO with interfaces)
- [x] Schema v2 migration policy specified
- [x] PRISM data handling statement drafted
- [x] All planning decisions logged in bugs_and_decisions.md

---

## What's Open

Two genuine open items, both blocking nothing yet but both needing resolution:

**1. Medical reviewer identification**
Action: Identify a reviewer (medical student, professor, doctor) and confirm commitment before Phase 1 ends. Send all three cascades + organ list as one batch using the Medical Review Request template. The Phase 1 Medical Content document is ready to ship.

**2. Procedure and Symptom Pattern schemas**
Status: Intentionally deferred to start of Phase 3. CADENCE-001 and PRISM-001 cannot start without these. Schedule a half-day of schema design at the start of Phase 3, modeled on the existing Disease/Organ schemas.

---

## The Concrete Next Five Steps

### Step 1 — Set up the Claude Project

**Where:** claude.ai, create new Project.

**What to do:**
1. Create a new Project named "AnatomiQ"
2. Paste the contents of `AnatomiQ_Project_Instructions.md` into the Project's custom instructions field
3. Upload all 12 documents to the Project's knowledge:
   - AnatomiQ_Project_Overview.docx
   - AnatomiQ_Features_Document.docx
   - AnatomiQ_Project_Instructions.md
   - AnatomiQ_Data_Schemas.md
   - AnatomiQ_AI_System_Prompts.md
   - AnatomiQ_Performance_And_Models.md
   - AnatomiQ_Operations_And_Planning.md
   - AnatomiQ_Phase1_Medical_Content.md
   - AnatomiQ_UX_And_Identity.md
   - AnatomiQ_Demo_Run_Of_Show.md
   - AnatomiQ_Build_Environment.md
   - bugs_and_decisions.md
4. Verify all uploads succeeded

**Time estimate:** 10 minutes.

**Done when:** Opening any new chat in the Project automatically loads all 12 documents as context.

---

### Step 2 — Set up Git + Git LFS + Unity .gitignore

**Where:** New chat in Claude Project.

**Critical:** Git LFS must be configured BEFORE the first commit that contains any binary file. Once a binary commits without LFS, recovery requires history rewriting that breaks collaborator clones. See `AnatomiQ_Build_Environment.md` Part B for the full strategy.

**Use this opener:**
```
This is the Git and source control setup chat for AnatomiQ.

Reference AnatomiQ_Build_Environment.md Parts B.4 and B.5 for the
exact .gitignore and .gitattributes content.

Create:
1. A proper .gitignore for Unity 6.3 LTS Android development per
   AnatomiQ_Build_Environment.md Part B.4 — covers Library/, Temp/,
   Logs/, Build/, IDE files, OS files, AND the AnatomiQ-specific
   exclusions (secrets.json, *.apikey, .env*, profiling data,
   Blender autosaves)
2. A .gitattributes with full Git LFS rules per
   AnatomiQ_Build_Environment.md Part B.5 — covers .blend, .glb,
   .gltf, .onnx, .png, fonts, audio, video, and the Unity-specific
   large generated assets (LightingData.asset, OcclusionCullingData.asset)
3. A README.md at project root with: project name, brief description,
   tech stack, how to clone and open, attribution to Z-Anatomy/BodyParts3D
4. A LICENSE file (academic project — CC BY-SA 4.0 is appropriate
   given the Z-Anatomy ShareAlike requirement)
5. A docs/ folder structure for storing the 12 planning documents
   alongside the codebase
6. Walk me through the LFS verification step:
   - run `git lfs install` (one time per machine)
   - after .gitattributes is committed, verify with
     `git check-attr filter -- test.blend` that LFS catches it
     (should report "filter: lfs")

Don't create the Unity project itself yet — we do that in the
scaffold chat. Just the version control scaffolding.
```

**What you do:**
1. Run the chat
2. Create a GitHub repository (private initially)
3. **Run `git lfs install`** on your machine (one-time setup)
4. Initialize locally with the .gitignore, .gitattributes (with LFS rules), README, LICENSE
5. First commit — these scaffolding files only, no binaries yet
6. Verify LFS rules will catch binaries: `git check-attr filter -- test.blend`
7. Push to GitHub

**Time estimate:** 30–45 minutes.

**Done when:** Empty repository on GitHub with `.gitignore` and `.gitattributes` (with LFS) committed. `git check-attr filter -- test.blend` reports `filter: lfs`. Ready to receive the Unity project and binary assets safely.

---

### Step 3 — Identify the medical reviewer (parallel track, do early)

**Where:** Real life. Email, hallway conversation, supervisor request.

**Why now:** Risk R1 is the only "open and high-impact" item left. Resolving it now means medical content review can happen on schedule rather than becoming a Phase 2 emergency.

**What to do:**
1. Identify 2–3 candidates (medical student peer, professor, working doctor)
2. Send the Medical Review Request from `AnatomiQ_Operations_And_Planning.md` Part C.7
3. Attach the Phase 1 Medical Content document
4. Confirm time commitment (30–60 min per disease, ~3 hours total)
5. Schedule the review for end of Phase 2

**Time estimate:** Variable. Could be a single email or could take weeks. Start now.

**Done when:** Reviewer confirmed and Phase 2 review checkpoint scheduled.

---

### Step 4 — Scaffold chat

**Where:** New chat in Claude Project.

**Pre-check before starting:** Workstation set up per `AnatomiQ_Build_Environment.md` Part A — Unity 6.3 LTS installed via Unity Hub with Android Build Support module checked, JDK 17 + NDK r27c + SDK Build Tools 36.0.0 auto-bundled. IDE installed (Rider preferred). `git lfs install` already run.

**Use this opener:**
```
This is the AnatomiQ scaffolding session. Today we're building the
foundational codebase only — no features yet, only structure.

Before writing any code, please verify the current state of these
packages with a web search:

- Unity 6.3 LTS current point release (we documented 6000.3.x)
- AR Foundation latest stable for Unity 6.3 (we documented 6.3.x)
- Google ARCore XR Plugin current version (we documented 6.3.x)
- Unity Inference Engine (com.unity.ai.inference) current version
  (we documented 2.4.x+)
- Newtonsoft JSON for Unity status
- Unity Localization package (com.unity.localization) current version

If any has moved, flag it and adjust.

Today's deliverables:
1. Unity 6.3 LTS project created with URP 3D template, Android target
2. Folder structure under Assets/_AnatomiQ/ matching Project Instructions
3. Base namespace stubs (AnatomiQ.Core, .AR, .Anatomy, .AI, .UI, .Data)
4. CORE-007 Fallback Manager shell — empty class with the AppState
   enum and the monitoring loop scaffold, no logic yet
5. ServiceRegistry ScriptableObject per AnatomiQ_Build_Environment.md
   Part D — with placeholder fields for FallbackManager, AIOrchestrator,
   DataLayer, BodyRenderer, exposed as interfaces (IFallbackManager
   etc.). One ServiceRegistry asset created in the project.
6. Event bus pattern for inter-system communication
7. Empty ScriptableObject schema definitions matching the Data Schemas
   doc — INCLUDING the v1.1 additions: nodeType field (Anatomical or
   PhysiologicalState) and fmaId field on OrganAsset
8. Unity Localization package installed and configured per
   AnatomiQ_Build_Environment.md Part C — English locale, three string
   tables (UIStrings, OrganNames, DiseaseContent) created empty
9. URP project settings configured per the performance budget
   (Forward+, HDR off, MSAA 2x, render scale 0.9, etc.)
10. ARCore + Inference Engine packages installed and configured
11. AndroidManifest.xml entries for camera permission and ARCore
12. Build settings: IL2CPP, ARM64, min API 29, target API 34, Vulkan first

Do not implement any feature logic. Just scaffolding.
```

**What you do:**
1. Run the chat — likely 4–6 hours of guided work
2. Verify the project builds and deploys to the Poco X5 Pro (empty scene, but successful build)
3. Commit the scaffold to git — verify any binaries went through LFS
4. Update bugs_and_decisions.md with the scaffold completion entry

**Time estimate:** Half a day to a full day depending on Unity setup time.

**Done when:** Empty AnatomiQ Unity project on the Poco X5 Pro, opening to an empty AR-capable scene without crashing. APK builds successfully. ServiceRegistry asset exists. Localization package set up with three empty tables. All scaffolding committed to git.

---

### Step 5 — Get the 3D model assets ready (parallel track with Step 4 and Step 6)

**Where:** Blender, not Claude.

**What to do:**
1. Download Z-Anatomy from https://lluisv.itch.io/z-anatomy
2. Archive a complete copy locally + Google Drive (Risk R8 mitigation)
3. Open in Blender
4. Identify the 24 organs from `AnatomiQ_Phase1_Medical_Content.md` Part B
5. Apply the 8-step preparation pipeline from Performance & Models Part B.4
6. Export six layer-grouped glTF files into `Assets/_AnatomiQ/Models/`

**Time estimate:** 2–3 weeks of part-time work for someone learning Blender. 1 week if already proficient.

**This step runs in parallel with Step 4 and Step 6 — don't block on it.** Start it early so meshes are ready when CORE-002 needs them.

**Done when:** Six layer-grouped glTF files in `Assets/_AnatomiQ/Models/` with proper material organization, FMA IDs as custom properties, and matching the 24-node Phase 1 organ list.

---

### Step 6 — Build the first feature: CORE-007 Fallback Manager

**Where:** New chat in Claude Project.

**Use the Feature Build Chat Opener** from Operations & Planning Part C.2:

```
Today we're building CORE-007: Fallback & State Manager.

Before writing code:
1. Look up CORE-007 in the Features Document and confirm description,
   dependencies, AI role, AR role, and fallback behavior.
2. Confirm dependencies — CORE-007 has none, it's the first system.
3. Check bugs_and_decisions.md for any prior decisions relevant.
4. Check Performance & Models doc for the metrics CORE-007 must monitor
   and the thresholds at which it triggers degradation.

Then propose an implementation plan covering:
- Class structure for the Fallback Manager
- AppState enum (AR_ACTIVE, AR_VIEWER_MODE, AR_LIMITED, OFFLINE_MODE)
- Performance metric monitoring (FPS rolling avg, RAM, thermal state)
- Threshold detection logic per Performance & Models doc
- Public API for other systems to subscribe to state changes
- C# event design for state change notifications
- What unit tests should cover

I'll review the plan before you start implementing. Build incrementally,
test after each chunk.
```

**What you do:**
1. Run the chat
2. Implement CORE-007 with the unit tests
3. Test on device with the on-device test scenarios from Performance & Models Part C.2
4. At end of chat, use the End-of-Feature Wrap-up template to generate the bugs_and_decisions.md update
5. Mark CORE-007 as ✅ in the feature status table
6. Commit and push

**Time estimate:** 1–2 weeks of part-time work. First feature is always slowest because it establishes patterns the rest will reuse.

**Done when:** CORE-007 implemented, unit tests passing, ✅ status in bugs_and_decisions.md, debug overlay visible on the Poco X5 Pro showing FPS/RAM/thermal metrics during a session.

---

## Beyond the First Six Steps

After Step 6 the rhythm is established. Build order from Features Document:

```
CORE-007 ✓ → CORE-008 (with Phase 1 organ list + cascades) → CORE-001
        → CORE-002 (uses Z-Anatomy meshes) → CORE-003 → CORE-004
        → CORE-006 → CORE-005 → ATLAS-003 → ATLAS-001 → ATLAS-002
        → MEDICAL REVIEW CHECKPOINT → ...
```

Each feature loop:
1. Open new chat with Feature Build template
2. Build incrementally
3. Test on device
4. Wrap-up chat to generate bugs_and_decisions update
5. Commit
6. Move to next feature

Every 4–5 features: open a Performance Review chat to verify the budget.
Every 7–10 days: open a Weekly Review chat for honest pacing check.
Once you have a working cascade: rehearse the demo against it. The demo Run-of-Show is a living document; refine it as the build progresses.

---

## A Note on Pacing

Three things worth holding onto:

**1. The plan is detailed because planning is fun. Building is harder.**
The shift from planning to building is psychologically tough. The plan was designed in a few sessions. The build is months of solitary work. Expect the first feature to feel slow and frustrating — that's normal.

**2. The plan is a tool, not a contract.**
Reality will diverge from the plan. Some decisions will turn out wrong. Some features will be harder than estimated. The plan exists to give a structured starting position, not to be followed mechanically. When reality contradicts the plan, update the plan and keep going.

**3. The demo run-of-show is the scope filter.**
When tempted to build something not in the demo, ask: "Does this serve the 4:30 I'm presenting?" If no, defer it. The demo focuses on the signature feature — disease cascade simulation with AI narration and the Interconnectivity Engine reveal. Everything in service of that ships polished. Everything else can be partial.

The most important features to finish: **CORE-007 → CORE-008 → CORE-001 → CORE-002 → CORE-005 → CORE-006 → ATLAS-003 → ATLAS-001 → ATLAS-002**. If only those finish, the project demonstrates the signature feature and has a credible academic submission. Everything else is enrichment.

---

## When This Document Should Be Updated

- After any major decision change → update this index + bugs_and_decisions.md
- After any new document is added → update the document list and relationship diagram
- After any phase completes → update "What's Done"
- After Phase 4 → archive this whole document set as v1.0 of the project planning record

---

*Project Index v3 · 2026 · The map. Read first when returning to the project.*
