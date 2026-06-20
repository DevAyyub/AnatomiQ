# AnatomiQ — Bugs & Decisions Log

> **How to use this file:**
> Update this manually whenever something significant happens — a bug fixed, an architectural decision made, a performance issue resolved, or a dependency behavior discovered. When opening a new chat, upload this file so Claude immediately knows the full history of solved problems and past decisions. Prevents solving the same problem twice.
>
> **At end of each feature chat, ask Claude:** *"Generate the bugs_and_decisions.md update for this session."* Then paste the output below the appropriate section.

---

## Feature Build Status

Track each feature here. Only mark ✅ when tested on physical device (Poco X5 Pro), not just in Unity editor.

| Feature ID | Name | Status | Date |
|---|---|---|---|
| CORE-007 | Fallback & State Manager | 🧪 Logic phase built — connectivity + framerate + thermal live; AR/API/inference documented stubs; two-axis (AppState + PerformanceTier) contract; thermal localization strings authored; 14 PlayMode + 5 EditMode green; not device-tested | 2026-06-20 |
| CORE-008 | Data Layer & ScriptableObjects | 🧪 Built — service + importer + §9 validator + Phase 1 content (32 EditMode + 5 PlayMode green); not device-tested | 2026-06-19 |
| CORE-001 | AR Session Manager | ⬜ Not started | — |
| CORE-002 | 3D Body Model Renderer | ⬜ Not started | — |
| CORE-003 | Layer Toggle System | ⬜ Not started | — |
| CORE-004 | Organ Selection & Highlight | ⬜ Not started | — |
| CORE-006 | AI Orchestrator | ⬜ Not started | — |
| CORE-005 | Body Interconnectivity Engine | ⬜ Not started | — |
| ATLAS-003 | AR Placement Modes | ⬜ Not started | — |
| ATLAS-001 | Disease Cascade Simulation | ⬜ Not started | — |
| ATLAS-002 | AI Anatomy Q&A Assistant | ⬜ Not started | — |
| ATLAS-004 | Time Progression Slider | ⬜ Not started | — |
| ATLAS-005 | Interconnectivity Explorer | ⬜ Not started | — |
| PRISM-002 | AR Body Map Input | ⬜ Not started | — |
| PRISM-001 | Symptom Checker Dialogue | ⬜ Not started | — |
| PRISM-003 | Medical Scan Importer | ⬜ Not started | — |
| PRISM-004 | AI Scan Annotator | ⬜ Not started | — |
| PRISM-005 | Doctor Explanation Mode | ⬜ Not started | — |
| PRISM-006 | Patient Report Generator | ⬜ Not started | — |
| CADENCE-001 | Procedure Walkthrough | ⬜ Not started | — |
| CADENCE-002 | AI Movement Evaluator | ⬜ Not started | — |
| CADENCE-003 | Performance Scorer | ⬜ Not started | — |
| CADENCE-004 | Scenario Library | ⬜ Not started | — |
| ATLAS-006 | Body Pose Overlay | ⬜ Not started | — |

**Status legend:** ⬜ Not started · 🔄 In progress · 🧪 Built, not yet device-tested · ✅ Tested on device

---

## Architecture & Technical Decisions

*Record every significant technical decision here — what was decided, why, and what alternatives were considered.*

---

### [PLANNING PHASE] — Unity version and platform

**Decision:** Unity 6.3 LTS (6000.3.x) with URP, targeting Android first.
**Reason:** Unity 6.3 LTS released December 2025, supported until December 2027. Unity 6.0 LTS support ends October 2026 — too soon. URP is mobile-optimized.
**Alternatives considered:** Unity 6.0 LTS (rejected, support window). Unity 2022 LTS (rejected, lacks AR Foundation 6.x and Inference Engine).

---

### [PLANNING PHASE] — Hybrid AI architecture

**Decision:** Hybrid AI — Unity Inference Engine on-device for real-time tasks, external API for reasoning and language.
**Reason:** LLMs too large for mobile. Real-time AR cannot tolerate API latency. On-device handles pose tracking and body detection; API handles Q&A, cascade narration, symptom dialogue.
**Rule:** Real-time visual frame work → Inference Engine. Reasoning/medical knowledge → async API call, never blocks AR session.
**Note:** Revisit this when building CORE-006 — small on-device LLMs may be viable for some Q&A by then.

---

### [PLANNING PHASE] — Unity Sentis renaming

**Decision:** Use Unity Inference Engine (com.unity.ai.inference, namespace Unity.InferenceEngine).
**Context:** Sentis was renamed in 2025 as part of Unity AI suite (Unity 6.2+). Same product, different package name.
**Old:** com.unity.sentis, Unity.Sentis
**New:** com.unity.ai.inference, Unity.InferenceEngine

---

### [PLANNING PHASE] — ARCore body tracking limitation

**Decision:** ATLAS-006 implemented via Unity Inference Engine + ONNX pose model, NOT native ARCore body tracking.
**Reason:** ARCore on Android does not provide native body tracking — that's iOS/ARKit only.
**Recommendation:** MoveNet Thunder — slightly larger but more accurate, robust to partial body visibility.
**Performance constraint:** ≥15 FPS on Poco X5 Pro for usable overlay.

---

### [PLANNING PHASE] — No diagnostic claims policy

**Decision:** PRISM never makes definitive diagnostic statements.
**Reason:** Medical liability, regulatory risk, ethical responsibility.
**Implementation:** Enforced in every PRISM AI system prompt. Educational disclaimer appended. Emergency symptoms redirect to emergency services.

---

### [PLANNING PHASE] — Data architecture

**Decision:** All static content stored as Unity ScriptableObjects, authored as JSON, imported at build time.
**Reason:** Content separate from code. Adding diseases means adding JSON, not modifying scripts. JSON easier for medical reviewers.
**Migration path:** JSON → ScriptableObjects in academic build. Possible Neo4j graph database in production.
**Schema spec:** AnatomiQ_Data_Schemas.md.

---

### [PLANNING PHASE] — JSON library

**Decision:** Use Newtonsoft JSON (com.unity.nuget.newtonsoft-json).
**Reason:** Unity's built-in JsonUtility cannot handle lists, nullable types, or nested arrays cleanly. Unity 6 does not ship System.Text.Json.
**Alternatives considered:** JsonUtility (too limited), System.Text.Json (not available without manual NuGet integration).

---

### [PLANNING PHASE] — Fallback-first architecture

**Decision:** CORE-007 Fallback Manager built before any other system.
**Reason:** Every system needs somewhere to report failures. Building fallbacks after features leads to untested degradation paths.
**Core principle:** AR and AI are enhancements, not requirements.

---

### [PLANNING PHASE] — Medical content validation checkpoint

**Decision:** All disease cascade sequences and procedural steps must be reviewed by a medical professional before final demo.
**Checkpoint:** End of Phase 2.
**Reason:** Plausible-looking but medically incorrect cascades will be caught instantly by clinicians.
**Action:** Identify reviewer before Phase 1 ends — see Risk R1.

---

### [PLANNING PHASE] — 3D model source

**Decision:** Z-Anatomy / Open3DModel for academic build. BioDigital deferred to commercial phase.
**Reason:** Free, university-backed (Leiden, Utrecht, Maastricht, Leuven, Amsterdam UMC, Radboud UMC, Ghent), uses FMA ontology IDs, medically credible, CC BY-SA license suitable for academic use.
**Constraint:** Attribution required in credits screen. ShareAlike clause is a constraint for commercial use later but not academic.
**Asset preparation:** 8-step pipeline in Performance & Models doc. Estimated 2–3 weeks of Blender work.
**Backup:** Local archive of Z-Anatomy downloaded at project start.

---

### [PLANNING PHASE] — AI provider

**Decision:** Anthropic Claude as primary (Sonnet 4.6 + Haiku 4.5 routing). OpenAI GPT-5 Mini configured as fallback. Provider-agnostic IAIProvider interface in CORE-006.
**Reason:** Claude's system prompt adherence is decisive for medical safety in PRISM. Explicit prompt caching gives 90% discount on static system prompts. Cost difference at academic scale negligible (~$10–15 total).
**Architecture:** CORE-006 implements IAIProvider interface. Provider type is config-driven, not hardcoded.
**Routing:** ~70% to Haiku 4.5 (cascade narration, movement feedback). ~30% to Sonnet 4.6 (Q&A, symptom dialogue, scan analysis, reports).

---

### [PLANNING PHASE] — Performance budget locked

**Decision:** Hard performance limits set per Performance & Models doc. CORE-007 monitors all metrics and triggers degradation at thresholds.
**Key limits:** Soft RAM ceiling 1.4 GB. Frame budget 33ms at 30 FPS. Polygon budget 1M total across all layers, max 2 layers visible. 250 drawcalls per frame.
**URP settings:** Forward+, HDR off, MSAA 2x, render scale 0.9, SRP Batcher on, Native RenderPass on, depth and opaque textures off.

---

### [PLANNING PHASE] — API timeout policy refined

**Decision:** Type-specific API timeouts replacing the original flat 8-second limit.
**Limits:** Cascade narration 3s. Q&A 5s soft / 12s hard. Scan analysis 30s. Streaming preferred where supported.
**Reason:** Mobile users perceive 3+ seconds as broken. Cascade narration plays alongside animation and must feel responsive.
**Implementation:** AICallType enum and CallAIAsync pattern in Data Schemas doc Section 7.

---

### [PLANNING PHASE — gap-fill round] — Schema v1.1 additions

**Decision:** Add `nodeType` (Anatomical / PhysiologicalState) and `fmaId` fields to OrganAsset.
**Reason:** Some cascade nodes are physiological states (blood glucose, blood pressure, serum calcium/phosphate), not anatomical structures. Forcing them into the organ schema produced awkward null fields. FMA IDs align AnatomiQ data with Z-Anatomy mesh provenance.
**Backward compatibility:** Both fields default to backward-compatible values (Anatomical, null).
**Status:** Apply during scaffold chat.
**Spec:** AnatomiQ_Phase1_Medical_Content.md Part A.

---

### [PLANNING PHASE — gap-fill round] — Phase 1 organ list canonicalized

**Decision:** 24-node organ list (20 anatomical + 4 physiological states) covers all three Phase 1 cascades (T2D, Hypertension, CKD) with no missing references.
**Reason:** Without this, CORE-008 implementation would derive organs ad-hoc from cascade `targetOrganId` references. Error-prone.
**Cross-disease overlap:** kidney_left used by all three diseases. blood_vessels_systemic by two. Reinforces educational coherence.
**Spec:** AnatomiQ_Phase1_Medical_Content.md Part B.

---

### [PLANNING PHASE — gap-fill round] — Three diseases authored

**Decision:** Three Phase 1 cascades complete: T2 Diabetes (in Data Schemas doc), Hypertension, Chronic Kidney Disease (both in Phase1 Medical Content doc).
**Status:** All marked `0.9-pre-review`. Ship to medical reviewer in single batch.
**Source citations:** Each cascade includes ≥4 peer-reviewed sources.
**Length consistency:** All three cascades 11 steps, ~34–35 seconds runtime.
**Spec:** AnatomiQ_Phase1_Medical_Content.md Parts C and D.

---

### [PLANNING PHASE — gap-fill round] — Test fixtures defined

**Decision:** `minimal_organ_graph.json` (3 connected nodes A→B→C) and `minimal_disease.json` (3-step linear cascade, 9s total) authored as test fixtures.
**Reason:** Unit tests for CORE-005 graph traversal and cascade playback need predictable, isolated data. Using full Phase 1 content as test data would couple tests to medical content changes.
**Spec:** AnatomiQ_Phase1_Medical_Content.md Part E. Includes 14 specific test cases.

---

### [PLANNING PHASE — gap-fill round] — UX architecture set

**Decision:** Bottom navigation with three pillar tabs + "more" tab. Body-as-hero principle. AR is a mode not a wrapper.
**Reason:** Three pillars are parallel and equally important; bottom nav surfaces structure rather than hiding it. Matches Android convention.
**Other UX decisions:** One question per screen for symptom dialogue. Permissions in-context (camera at AR mode entry, not at app launch). Dark mode primary.
**Spec:** AnatomiQ_UX_And_Identity.md Parts A–D.

---

### [PLANNING PHASE — gap-fill round] — Visual identity deferred to iteration

**Decision:** Specific hex colors, typeface, iconography, and logo NOT specified at planning stage. Constraints set; specifics deferred.
**Reason:** Pre-specified visual design at planning stage produces decisions that don't survive contact with real screens.
**Constraints set:** Clinical not stylized aesthetic. Single neutral foundation + single accent. One sans-serif family, two weights, three sizes. Outline icons. Realistic-clinical body model rendering. Dark mode primary.
**Spec:** AnatomiQ_UX_And_Identity.md Part F.

---

### [PLANNING PHASE — gap-fill round] — Demo run-of-show v0

**Decision:** ~4:30 demo focused on signature feature (disease cascade simulation). Pre-recorded backup video required. Three demo formats prepared (live, video, screenshots).
**Reason:** A focused 4-minute demo of the differentiator beats a 12-minute feature tour. Demo doc shapes scope: every feature decision is checked against "does this serve the demo?"
**Spec:** AnatomiQ_Demo_Run_Of_Show.md.

---

### [PLANNING PHASE — gap-fill round] — Procedure and Symptom schemas explicitly deferred

**Decision:** Procedure schema (CADENCE-001 dependency) and Symptom Pattern schema (PRISM-001 dependency) are deferred to their respective phases.
**Reason:** Schemas designed months before they're needed tend to get redesigned anyway. Better to design when implementation context is clear.
**Phase 3 blocker flag:** CADENCE-001 cannot start without Procedure schema. PRISM-001 cannot start without Symptom Pattern schema. Schedule a half-day of schema design at the start of each.

---

### [PLANNING PHASE — build-environment round] — Workstation setup via Unity Hub

**Decision:** Use Unity Hub to install JDK 17 + NDK r27c (27.2.12479018) + SDK Build Tools 36.0.0 — the bundled versions for Unity 6.3 LTS.
**Reason:** Manual installation creates version mismatches that produce cryptic Gradle/IL2CPP build errors. Hub-bundled tools are the only officially supported configuration. Verified against Unity's official supported dependency matrix.
**IDE:** JetBrains Rider primary (free for non-commercial); VS or VS Code + C# Dev Kit acceptable. Don't install Android Studio for AnatomiQ — Unity Hub bundles everything needed.
**Spec:** AnatomiQ_Build_Environment.md Part A.

---

### [PLANNING PHASE — build-environment round] — Git LFS configured before first binary commit

**Decision:** Git LFS rules in `.gitattributes` are committed before any `.blend`, `.glb`, `.onnx`, `.png`, etc. enters the repo.
**Reason:** Once a binary file is committed without LFS, it stays in history forever — recovery requires `git filter-repo` or BFG, both of which break collaborator clones. Z-Anatomy `.blend` source files alone (200–500 MB each) exceed GitHub's 100 MB hard limit per file.
**Verification rule:** `git check-attr filter -- file.ext` must report `filter: lfs` before committing any new binary type.
**Coverage:** `.blend`, `.fbx`, `.glb`, `.gltf`, `.onnx`, `.tflite`, `.psd`, `.png`, `.jpg`, `.exr`, `.tif`, fonts, audio, video, plus Unity-specific large generated assets (`*LightingData.asset`, `*OcclusionCullingData.asset`).
**Important nuance:** Do NOT apply `merge=unityyamlmerge` blanket to `*.asset` — corrupts terrain/lighting data. Scope smart-merge to `*.unity` and `*.prefab` only.
**Spec:** AnatomiQ_Build_Environment.md Part B.

---

### [PLANNING PHASE — build-environment round] — Localization via Unity Localization package

**Decision:** Use `com.unity.localization` package from day one. English only ships in academic build but every UI string is keyed.
**Reason:** Externalizing strings later is a refactor pass touching every UI component. Doing it from day one is one-time setup. Unity Localization integrates with the existing ScriptableObject-everywhere architecture.
**Tables to create at scaffold:** `UIStrings`, `OrganNames`, `DiseaseContent`.
**Key naming convention:** `ui.<screen>.<element>.<state>` for UI; `organ.<organId>.display_name` for content; `disease.<diseaseId>.<field>` for disease data.
**Exception:** Cascade narration fallbacks (`narrationFallback` in cascade JSON) stay in JSON for medical-review-friendliness. Round-tripping medically-validated content through localization tables adds re-review risk.
**Spec:** AnatomiQ_Build_Environment.md Part C.

---

### [PLANNING PHASE — build-environment round] — Service access via ScriptableObject registry

**Decision:** Cross-cutting services accessed via `ServiceRegistry` ScriptableObject (`AnatomiQ.Core.ServiceRegistry`). No singletons. No `FindObjectOfType`. No direct scene references.
**Reason:** Aligns with existing ScriptableObject-everywhere decision. Testable (mock registry for unit tests). Works in Edit mode. No boilerplate per feature. Singletons fight Unity's serialization and produce lifecycle bugs.
**Pattern:** Each feature has `[SerializeField] ServiceRegistry _services`. Services exposed as interfaces (`IFallbackManager`, `IAIOrchestrator`, `IDataLayer`, `IBodyRenderer`) for testability and provider swapping.
**Enforcement:** Forbidden patterns — `FindObjectOfType<>()`, `static Instance`, direct scene references to service classes.
**Spec:** AnatomiQ_Build_Environment.md Part D.

---

### [PLANNING PHASE — build-environment round] — Schema versioning policy

**Decision:** Every ScriptableObject and JSON content file has a `schemaVersion: int` field, currently `1`. When breaking changes are needed (planned: branching cascades, recovery animations):
1. Increment to `schemaVersion: 2` only after a migration script exists
2. Migration scripts live in `Assets/_AnatomiQ/Scripts/Editor/Migrations/` and run from Editor menu (`AnatomiQ → Migrations → Migrate v1 to v2`)
3. Migrations are version-controlled — never delete an older one; future devs may need to migrate v1 → v3
4. Migrations preserve original data: write to `_v1_backup/` before transforming
5. Never auto-run migrations on app startup — Editor-only, deliberate

**Reason:** Prevents future-you from breaking existing diseases when adding schema features. Cheap to enforce now.

---

### [PLANNING PHASE — build-environment round] — ATLAS-006 model sourcing flagged for ATLAS-006 chat

**Decision:** Do NOT pre-research ONNX pose model availability. Flag as the first task of the ATLAS-006 chat instead.
**Reason:** Mobile ML model state changes faster than this project will run. MoveNet Thunder and BlazePose Full are recommended in Performance & Models, but ONNX availability, license terms, file size, and Inference Engine 2.4.x compatibility need verification at build time, not now. Researching 6+ months in advance produces stale information.
**Action:** Plan first hour of ATLAS-006 chat as model research and download. Acceptable substitutes exist (Google's MoveNet variants, MediaPipe-derived ONNX exports). Flexibility is fine.

---

### [PLANNING PHASE — build-environment round] — PRISM data handling statement drafted

**Decision:** Privacy/data handling text drafted and ready to paste into Settings screen and demo Q&A.
**Key claims:** No PII collected. Anatomy content lives entirely on device. Conversation content sent to Anthropic API for processing only; per Anthropic's policy, API inputs are not used for model training. Symptom dialogue not stored after session ends. Uploaded scans not retained beyond the analysis request. Cache reset available in Settings.
**Spec:** AnatomiQ_Build_Environment.md Part E.1.

---

### [EXECUTION — Step 2] — Git repository + LFS setup complete, pushed to GitHub

**Status:** ✅ DONE. Repo live at https://github.com/DevAyyub/AnatomiQ (private, branch `main`).
**What was done (in order):**
- `git lfs install` run (one-time, this machine). Git identity configured (DevAyyub).
- `.gitignore` + `.gitattributes` committed FIRST (commit `9ff97e7`), before any other file — preserves the LFS-before-binary ordering rule from Build Environment B.1.
- LFS armed and verified: `git check-attr filter -- test.blend` → reported `filter: lfs` before any binary was committed (B.8). Full pattern list confirmed via `git lfs track`.
- README + LICENSE (CC BY-SA 4.0 + Z-Anatomy/BodyParts3D attribution) + `docs/` structure committed (`0351936`).
- All 12 planning documents committed to `docs/` (`e4d52b9`), then pushed to GitHub.
**License note:** Repo-wide CC BY-SA 4.0 chosen to satisfy the ShareAlike obligation inherited from the Z-Anatomy source data (CC BY-SA 2.1 JP). 4.0 is a CC-sanctioned upgrade target for 2.x derivatives (verified against Creative Commons' published ShareAlike compatibility rules). Caveat recorded in LICENSE: CC licenses are not ideal for software — if the project goes commercial, revisit with a code/assets dual-license split (e.g. MIT/Apache-2.0 for C#, CC BY-SA retained for meshes) plus legal review. Not a concern for the academic build.
**Everyday workflow from here:** `git add .` → `git commit -m "..."` → `git push`.

---

### [EXECUTION — Step 2] — `.docx` files migrated to LFS (caveat found + resolved)

**Caveat:** The original `.gitattributes` had no `*.docx` rule, so the two Word planning docs (`AnatomiQ_Features_Document.docx`, `AnatomiQ_Project_Overview.docx`) first committed as regular Git binary (`e4d52b9`). Harmless at their size (~few hundred KB), but inconsistent with the all-binaries-to-LFS intent and wasteful if they're revised often.
**Fix:** Added `*.docx` to `.gitattributes` (`d46375a`), then re-staged + committed the two existing files to actually convert them (`1276608`). Confirmed via `git lfs ls-files` — both now listed and uploaded through the LFS channel on push.
**Lesson (forward rule):** Adding an LFS rule for a file type *already committed* takes TWO commits: (1) commit the `.gitattributes` rule, then (2) `git add` + commit the files themselves to convert them. `git lfs migrate import --no-rewrite` is the documented alternative but refuses on a dirty working tree — for a solo repo, a normal re-commit is simpler. When introducing ANY new binary type, add its `.gitattributes` rule and verify with `git check-attr` BEFORE the first file of that type is committed, and confirm afterward with `git lfs ls-files`.
**LFS quota reminder:** GitHub free tier ~1 GB storage / 1 GB bandwidth per month. Reserve it for `.blend`/`.glb`/`.onnx`; don't push Blender autosaves. Fallbacks if hit: $5/mo 50 GB data pack, or GitLab (10 GB free). (Build Environment B.7)

---


### [EXECUTION — Steps 1–8] Unity scaffold chat — overview

**Status:** ✅ DONE. Full project scaffold built across 8 gated sections, each compiled/tested in-editor and committed+pushed. Editor: Unity 6000.3.17f1 LTS. No feature logic written — structure, architecture, schemas, and settings only.
**Sections:** (1) URP 3D project + folder tree, (2) packages + version lock, (3) Android build settings + URP mobile asset, (4) ARCore + AndroidManifest, (5) ServiceRegistry + interfaces + event bus, (6) CORE-007 FallbackManager shell, (7) data schemas, (8) Localization.
**Gate policy:** every section ended in a compile/build/test gate before commit. Section 3 produced a clean APK (proving the IL2CPP/ARM64/Vulkan pipeline). Sections 5–8 verified via EditMode/PlayMode tests and editor checks (no device build needed).

---

### [EXECUTION — Step 4 scaffold] Verified package versions (web-checked June 2026)

**Decision:** Pin the AR pair and confirm the moved/renamed packages at scaffold time rather than trusting planning-era numbers.
**Verified:** AR Foundation **6.3.4** + Google ARCore XR Plugin **6.3.4** (matched pair — leaving version blank wrongly pulled 6.5.0, which targets Unity 6000.4/6000.5; downgraded to 6.3.4). Inference Engine `com.unity.ai.inference` **2.5.0** (namespace `Unity.InferenceEngine`; Package Manager display name may still read "Sentis"). Newtonsoft `com.unity.nuget.newtonsoft-json` **3.2.2** (auto-referenced; available to all asmdefs without explicit ref). Localization `com.unity.localization` **1.5.12**. Input System **1.19.0** (from template).
**Note:** AR Foundation 6.3 deprecated URP Compatibility Mode → Render Graph required (aligns with the URP setting below).

---

### [EXECUTION — Step 4 scaffold] Assembly definition graph (DAG)

**Decision:** One asmdef per module with name-based references, in a strict acyclic graph:
`AnatomiQ.Data` (no app refs) ← `AnatomiQ.Core` (refs Data) ← pillars: `AnatomiQ.AR` (Core + Unity.XR.ARFoundation/ARSubsystems/CoreUtils), `AnatomiQ.Anatomy` (Core+Data), `AnatomiQ.AI` (Core+Data+Unity.InferenceEngine), `AnatomiQ.UI` (Core+Data+Unity.Localization). Pillars never reference each other.
**Reason:** Enforces the "no tight coupling between pillar systems" rule at the compiler level and keeps build times incremental.

---

### [EXECUTION — Step 4 scaffold] Service registration is runtime, not serialized

**Decision:** Services self-register into the `ServiceRegistry` asset at runtime (`_services.Register(this)` in `Awake`); ordering guaranteed by `[DefaultExecutionOrder]` — FallbackManager `-1000` (registers first), AppBootstrap `-500` (verifies readiness in `Start`).
**Reason:** A ScriptableObject asset cannot persist serialized references to scene MonoBehaviours, so inspector-wiring services into the registry asset is impossible. Runtime registration + execution order is the clean alternative. Each service carries its own `[SerializeField] ServiceRegistry _services` (inspector-assigned). Registry clears on `OnEnable` so stale refs never leak between play sessions.
**Event bus:** ScriptableObject `EventChannel<T>` pattern (generic base + `VoidEventChannel` + `AppStateEventChannel`), not a global singleton bus.
**Initial AppState:** `AR_VIEWER_MODE` (safe 3D baseline) until monitoring logic promotes/demotes.

---

### [EXECUTION — Step 4 scaffold] URP wired to Android-Mobile pipeline asset

**Decision:** Android Quality level set to **Mobile** → `Mobile_RPAsset`; `Mobile_RPAsset` also set as the Graphics **Default Render Pipeline** (filled the empty "None" footgun so any code path reading the default still gets URP). Renderer = Forward+, Depth Priming off.
**Tuning (per performance budget):** HDR off, MSAA 2x, Render Scale 0.9, Depth Texture off, Opaque Texture off.
**Render Graph:** confirmed active. In Unity 6.3 the "Compatibility Mode (Render Graph disabled)" toggle is deprecated and stripped — its **absence** is the confirmation Render Graph is on, which AR Foundation 6.3 requires. (Would need a `URP_COMPATIBILITY_MODE` scripting define to turn off — never do this.)
**Build proof:** Section 3 produced a clean APK (IL2CPP, ARM64-only, Vulkan→OpenGLES3, Min API 29 / Target API 34).

---

### [EXECUTION — Step 4 scaffold] AndroidManifest scope — ARCore entries belong to the plug-in

**Decision:** The custom `Assets/Plugins/Android/AndroidManifest.xml` declares ONLY what the ARCore plug-in does not inject: `CAMERA`, `INTERNET`, `ACCESS_NETWORK_STATE` permissions (+ an empty `<application tools:node="merge"/>` hook). It must NOT declare the `com.google.ar.core` meta-data or the `android.hardware.camera.ar` feature.
**Reason:** With XR Plug-in Management ARCore = Required, the plug-in (`:arcore_client:`) auto-injects those at build time. Duplicating them fails the Gradle manifest merger (see Bugs). Source of truth for AR-required = XR Plug-in Management. Device-unsupported / permission-denied cases are handled in software by CORE-007 (3D Viewer Mode), not by weakening the manifest.

---

### [EXECUTION — Step 4 scaffold] IDataLayer decoupled from Core (cycle avoidance)

**Decision:** `IDataLayer` lives in the `AnatomiQ.Data` assembly and does NOT extend `IService` or reference Core. It is registered through a dedicated `ServiceRegistry.RegisterDataLayer(IDataLayer)` entry point, not the generic `Register(IService)` path.
**Reason:** Core already references Data (the registry exposes `IDataLayer DataLayer`). If `IDataLayer : IService` (IService lives in Core), Data would have to reference Core → circular assembly dependency, which Unity forbids. The compiler caught this during Section 5. The other service interfaces (`IFallbackManager`, `IAIOrchestrator`, `IBodyModelRenderer`) live in Core and do extend `IService`.

---

### [EXECUTION — Step 7 scaffold] Data schema implementation specifics

**Decision:** `OrganAsset` and `DiseaseAsset` use **public fields** (not `_camelCase` private + properties).
**Reason:** Deliberate exception for data ScriptableObjects — the planned JSON→SO importer and the inspector need to bind fields 1:1. The private-field/property coding standard applies to logic classes (MonoBehaviours), not data SOs (matches the C# in Data Schemas doc §2.6/§5).
**v1.1 applied:** `OrganAsset.NodeType` (enum `NodeType { Anatomical, PhysiologicalState }`, defaults Anatomical) and `OrganAsset.FmaId` (string, nullable) added right after `DisplayName`, per Phase1 Medical Content Part A.
**Added type:** `OrganMetadata { List<string> Sources; string LastReviewed; }` — implied by the JSON `metadata` object but not spelled out in the doc's C#.
**Scope:** Only `OrganAsset` + `DiseaseAsset` scaffolded. Procedure/Symptom schemas remain deferred to Phase 3.

---

### [EXECUTION — Step 8 scaffold] Localization setup complete

**Decision:** Localization Settings created; English (en) locale added and set as Project Locale Identifier; three String Table Collections created (`UIStrings`, `OrganNames`, `DiseaseContent`); `ui.app.name = "AnatomiQ"` entry added; **Android App Info** metadata configured (Display Name → `UIStrings/ui.app.name`).
**Outcome:** Clears the recurring *"Android App Info has not been configured"* warning.
**Confirmed rule (Build Env C.5):** cascade `narrationFallback` stays in JSON, NOT in localization tables — keeps medically-validated content editable by reviewers without re-review risk. `DiseaseContent` holds disease names/descriptions/stage labels, not cascade narrations.

---

### [EXECUTION — CORE-008] BodySystem enum renamed Vascular → Cardiovascular

**Decision:** Renamed `BodySystem.Vascular` → `BodySystem.Cardiovascular` (member only; ordinal/position unchanged).
**Why:** The carried-forward `metabolic` concern was a red herring — no authored node uses `system: "metabolic"` (blood glucose / serum calcium are `endocrine`). The real mismatch was `system: "cardiovascular"` on 7 nodes (heart, left ventricle, large arteries, systemic + coronary vessels, red cells, blood pressure), which had no matching enum member. Renaming makes the JSON↔enum mapping 1:1 (snake_case strategy: `cardiovascular` → `Cardiovascular`), matches the medically-correct term (the heart is not a "vessel"), and cleanly separates the *system* axis from `AnatomyLayer.Vascular` (the *layer* axis — e.g. `blood_vessels_systemic` is system=Cardiovascular, layer=Vascular).
**Safe:** No content asset or test referenced `BodySystem.Vascular` (confirmed against `DataSchemaTests`). `DiseaseCategory.Cardiovascular` already existed — the asymmetry is now gone.
**Follow-up:** Data Schemas §2.3 should change its `vascular` entry to `cardiovascular` to stay in sync (doc is read-only project knowledge; not edited from this chat).
**Resolves:** the deferred "physiological_state system vs BodySystem enum" item.

---

### [EXECUTION — CORE-008] JSON↔C# field bridges + importer approach

**Decision:** Three `[JsonProperty]` attributes bridge the doc's JSON keys to the C# field names that don't match them: `anatomicalRegion`→`Region`, `icd10`→`Icd10Code`, `visualEffect`→`Effect`. (The design docs' own C# disagrees with their own JSON on exactly these three.)
**Importer:** `ContentImporter` (Data assembly, pure logic) uses Newtonsoft `JsonConvert.Populate` into a `ScriptableObject.CreateInstance` (you can't `new` a ScriptableObject), with `StringEnumConverter(new SnakeCaseNamingStrategy())` so snake_case enum values (`highlight_pulse`, `supplies_blood`, `physiological_state`) map to PascalCase members. Everything else matches case-insensitively, so only the three bridges were needed. Accepts a bare object or an `{ "organs": [...] }` / `{ "diseases": [...] }` envelope. A malformed item is reported via out-param and skipped, never thrown.

---

### [EXECUTION — CORE-008] Drop-the-edge refined; uniqueness split

**Decision:** When a connection's `toOrganId` (or a `parentOrganId`) doesn't resolve, the DataLayer logs a warning and leaves the edge INERT — it does NOT remove it from the asset. Consumers resolve targets via `TryGetOrgan`, which returns false for a missing node, so a dangling edge can't break traversal.
**Why not mutate:** mutating a ScriptableObject asset in Play Mode persists the change back to the project (a known Unity gotcha). Logging + inert edge achieves the same "organ stays usable" outcome without touching content.
**Uniqueness split:** disease-id uniqueness is a §9 rule enforced in `ContentValidator` (via an existing-ids set); organ-id uniqueness is enforced at DataLayer dictionary-build time (first wins, duplicate logged + skipped).

---

### [EXECUTION — CORE-008] Content load source = build-time import → .asset + manifest

**Decision:** Runtime never parses JSON. An Editor tool (`AnatomiQ → Content → Import JSON`) reads `Content/{organs,diseases}/*.json`, validates, writes one `.asset` per item to `ScriptableObjects/{Organs,Diseases}/`, and rebuilds a `ContentManifest` SO. The `DataLayer` (`[SerializeField] ContentManifest`) loads from the manifest.
**Defense in depth:** the DataLayer RE-validates every item at load (not only at import), so a deleted asset or a hand-edited inspector value degrades gracefully (log + skip) instead of crashing — this is what actually delivers the CORE-008 fallback rule.
**GUID stability:** existing `.asset`s are updated via `EditorUtility.CopySerialized` (not delete-and-recreate), so manifest/scene references survive re-imports.
**Alternative considered:** ship raw JSON in StreamingAssets and parse at launch (simpler, no Editor tool, but no inspector visibility + a startup parse cost) — rejected.

---

### [EXECUTION — CORE-008] Editor tool isolated in its own nested asmdef

**Decision:** The import tool lives in `Scripts/Editor/Content/` under its own `AnatomiQ.Editor.Content` asmdef (references `AnatomiQ.Data`, Editor-only), NOT at the `Scripts/Editor/` root.
**Why:** `Scripts/Editor/` already hosts the migrations assembly (`Migrations/`). A second asmdef at the Editor root would risk a name collision or silently absorb `Migrations` into the new assembly. A nested asmdef governs only its own subtree, so this is safe regardless of how the existing Editor assembly is configured.

---

### [EXECUTION — CORE-007] Two-axis state model: AppState + PerformanceTier

**Decision:** CORE-007 now publishes TWO orthogonal signals, not one. Axis 1 = `AppState` (AR/connectivity context: AR_ACTIVE / AR_VIEWER_MODE / AR_LIMITED / OFFLINE_MODE) via the existing `OnAppStateChanged`. Axis 2 = a NEW `PerformanceTier` enum (Nominal → Reduced → Aggressive → Critical, ascending severity) via a new `OnPerformanceTierChanged` event on `IFallbackManager`, plus `CurrentTier` and a read-only `PerformanceMetrics` snapshot.
**Why:** Low FPS and thermal throttling are *quality reductions* (render scale, LOD, shadow distance, post-processing, inference frequency), not AR/connectivity modes. They don't map onto any AppState — `SetState(AR_ACTIVE)` can't express "drop render scale to 0.85". Overloading AppState (e.g. an `AR_ACTIVE_DEGRADED` member) would combinatorially explode the enum and force every AppState subscriber to care about render quality. A device can legitimately be AR_ACTIVE + Critical (tracking fine but hot) or OFFLINE_MODE + Nominal (no network, cool) — proof the axes are independent.
**Contract:** `PerformanceTier` is *severity*, not concrete levers. CORE-007 decides how bad it is; CORE-002's renderer (the pending consumer) translates tier → URP levers per Performance doc A.10/A.11. This keeps CORE-007 free of any rendering dependency and keeps the assembly DAG clean (Core never references the AR/Anatomy pillars).
**Alternatives considered:** a single combined signal, or CORE-007 emitting raw "set render scale 0.75" events — both rejected (false coupling; CORE-007 reaching into URP internals it doesn't own).
**Trade-off accepted:** two subscription surfaces to keep coherent. Justified because they're genuinely orthogonal.

---

### [EXECUTION — CORE-007] Fire the tier signal now, with no consumer yet

**Decision:** `OnPerformanceTierChanged` is published in the logic phase even though no system subscribes yet (CORE-002 subscribes when the renderer lands).
**Why:** The tier transitions are real and tested; absence of a consumer doesn't make the signal wrong. Mirrors how `OnAppStateChanged` already existed with no subscribers. The FPS/thermal *sources* exist now, so the only thing pending is the *consumer* — flagged as a carry-forward, not faked.

---

### [EXECUTION — CORE-007] Signal-check sources: which are live vs documented stubs

**Decision:** Of the six monitoring checks, three are implemented now (their sources exist): `CheckConnectivity`, `CheckFramerate`, `CheckThermal`. Three stay documented stubs with TODOs tied to their feature: `CheckArTracking` (needs CORE-001), `CheckApiAvailability` (needs CORE-006), `CheckInferenceState` (needs on-device inference). The monitoring-loop coroutine from the scaffold was NOT rebuilt — only the checks it already invokes were filled.
**Tested:** an "inert stubs" PlayMode test holds connectivity reachable + thermal cool + FPS good, then asserts no state/tier change across 5 passes — proving the unimplemented checks cause no transitions.

---

### [EXECUTION — CORE-007] Per-frame FPS collection, 1-second evaluation

**Decision:** FPS is collected EVERY frame in `Update()` into a 30-frame ring buffer (O(1) running sum, allocation-free); the 1-second `MonitorPass` only *evaluates* the smoothed average. Connectivity and thermal stay on the 1s tick.
**Why:** A 1 Hz sampler can't compute a meaningful 30-frame rolling average (at 30 fps that's ~1 s of frames sampled once per second). The split — per-frame collection, 1 s evaluation — is the fix. Confirmed the 1 s cadence is correct for connectivity/thermal (both slow-moving; thermal changes over minutes) but wrong for FPS.
**Hysteresis:** demote when rolling FPS < 30 sustained 3 s; promote only when > 40 sustained 5 s. Separate enter/exit thresholds (30/40 dead-band) + longer recovery dwell prevent flapping at the boundary. Stepwise — one tier level per qualifying interval, never a jump. A dip interrupted before 3 s resets the sustain timer.
**Scope note:** the global 30 fps floor (from the Fallback Rules) is used now; scenario-specific minimums (45/30/20 from Performance A.54) are settable later by CORE-002/ATLAS-006 per scenario.

---

### [EXECUTION — CORE-007] Thermal API is Adaptive Performance (now CORE in Unity 6.3), not Application.thermalState

**Decision/Finding:** There is NO `Application.thermalState` in Unity 6.3. The correct OS-backed thermal signal comes through **Adaptive Performance**, which in Unity 6.3 was **moved from a package into the engine core** (release note: "Moved Adaptive Performance 6 from a package to the Unity core. Bundled provider packages with the Unity Editor"). API surface (stable across AP 1.x–5.x, web-verified): `Holder.Instance.ThermalStatus.ThermalMetrics.WarningLevel` (`NoWarning` / `ThrottlingImminent` / `Throttling`) + `.TemperatureLevel` (0–1), guarded by `Holder.Instance.Active`. Requires the **Android (Google) provider subsystem** installed + enabled, or `ap.Active` is false and there's no data. Provides no data in the editor or PlayMode (device-only).
**Mapping (matches Unity's own AP LOD example):** `NoWarning`→Nominal; `ThrottlingImminent` ≤0.8→Reduced (Moderate); `ThrottlingImminent` >0.8→Aggressive (Severe); `Throttling`→Critical.
**Decoupling:** a Core-local `ThermalWarning` enum mirrors AP's `WarningLevel`, so the signal logic and its tests run package-free; only the deferred adapter references the AP namespace.

---

### [EXECUTION — CORE-007] Asymmetric thermal hysteresis (heat fast, cool slow)

**Decision:** Heating applies IMMEDIATELY (jump straight to the hotter tier); cooling is gated by a 3-pass dwell and steps DOWN one level at a time.
**Why:** Shedding load late risks the OS throttling/killing the app, so respond to heat at once (safety-first). But a momentary cool reading must not snap quality back up mid-cascade, so cooling is sustained + stepwise. A re-heat during cooldown jumps straight back to the hot tier and resets the cooldown streak.

---

### [EXECUTION — CORE-007] Cross-axis reconciliation: published tier = max(FPS, thermal)

**Decision:** FPS and thermal each produce an independent sub-tier (`_fpsTier`, `_thermalTier`); the published `CurrentTier` is `(PerformanceTier)Math.Max((int)fps, (int)thermal)` via `ReconcileTier()`.
**Why:** Whichever axis is worse should win — a hot device at good FPS still degrades; a cool device at bad FPS still degrades. This is why `PerformanceTier` members are ordered by ascending severity (do not reorder — `max` depends on it). Tested both single-axis directions and the both-active case.

---

### [EXECUTION — CORE-007] Thermal user strings: keys in code, values in UIStrings, display deferred

**Decision:** Two Localization keys are defined as public constants on `FallbackManager` — `ui.system.thermal.warning` (THERMAL_WARNING_STRING_KEY, shown at Aggressive) and `ui.system.thermal.critical` (THERMAL_CRITICAL_STRING_KEY, shown at Critical). Their English values were authored into the `UIStrings` table by a one-shot Editor tool (`AnatomiQ ▸ Localization ▸ Add CORE-007 Thermal Strings`, in `Scripts/Editor/Localization/ThermalStringsInstaller.cs`). CORE-007 does NOT display them.
**Why:** CORE-007 owns *signals*, not presentation. A UI layer (CORE-002/UI) subscribes to `OnPerformanceTierChanged` and shows the matching localized string at the relevant tier. Wiring display now would couple CORE-007 to a UI contract that doesn't exist yet — same reasoning as the deferred debug overlay. Keys are owned by code (constants); values live in the table; display is a deferred consumer. Strings go through Unity Localization per the day-1 rule (the cascade-narration JSON exception does not apply).
**Tooling note:** the installer lives in an `Editor/`-convention folder with NO asmdef — verified that `Scripts/Editor/` has no root asmdef (only the nested `AnatomiQ.Editor.Content` one), so Unity compiles the tool into the predefined `Assembly-CSharp-Editor` assembly, which already references the Localization editor API. Used the documented `GetStringTableCollection` → `GetTable("en")` → `AddEntry(key, value)` path; idempotent (skips existing keys).

---

### [EXECUTION — CORE-007] Debug overlay: sampling now, on-screen widget deferred

**Decision:** The A.12 performance overlay is split. The *sampling* + the read-only `PerformanceMetrics` snapshot (rolling FPS, tier, temperature level, RAM placeholder) ship now as the data contract. The on-screen overlay *widget* is deferred to CORE-002 (it needs renderer-reported GPU/drawcall/triangle stats and a UI host). Note the CORE-007 *feature spec* does not itself mention an overlay; A.12 in the Performance doc does — resolved by building the signal half now, the UI half later.

---

### [EXECUTION — CORE-007] Testability via provider seams + InternalsVisibleTo

**Decision:** Each live signal reads through a swappable interface — `IConnectivityProvider` (real: `UnityConnectivityProvider`), `IFrameClock` (real: `UnityFrameClock`), `IThermalProvider` (real: deferred `AdaptivePerformanceThermalProvider`, default: `NullThermalProvider`). Production defaults are the real Unity-backed impls; PlayMode tests inject fakes via `internal` `Configure*` hooks exposed through `[assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]` (new `AssemblyInfo.cs` in Core). A `TickForTest()` runs one monitor pass synchronously, bypassing the 1 s coroutine.
**Why:** static Unity APIs (`Application.internetReachability`, `Time.unscaledDeltaTime`, Adaptive Performance) can't be driven in a unit test. The seams make all threshold/hysteresis/reconciliation logic deterministically testable off-device. Matches the testing standard: mock the hard-to-test source, test the consuming logic. `InternalsVisibleTo` target verified against the PlayMode asmdef name (`AnatomiQ.Tests.PlayMode`).

## Bugs & Fixes

*Log every significant bug here — what it was, what caused it, how it was fixed. Format: Feature → Symptom → Cause → Fix.*

---

<!-- EXAMPLE FORMAT — delete this when you add your first real entry:

### [DATE] — CORE-001 — AR session not starting on device

**Symptom:** AR camera feed black on Poco X5 Pro, session never initializes.
**Cause:** ARCore set to Optional instead of Required in XR Plug-in Management settings. Device defaulted to non-AR mode.
**Fix:** Set ARCore Requirement to Required in Edit → Project Settings → XR Plug-in Management → ARCore.
**Prevention:** Always verify ARCore is set to Required after any fresh project setup.

-->

### [2026-06-18] — Scaffold/Build — Release APK fails: ARCore namespace used in multiple modules  ⚠️ DEFERRED → CORE-001

**Symptom:** `File → Build` (release APK) fails at `:launcher:processReleaseMainManifest` — *"Manifest merger failed with multiple errors."* Full error: *"Namespace 'com.google.ar.core' is used in multiple modules and/or libraries: :arcore_client:, :unityandroidpermissions:."*
**Cause (VERIFIED, not inferred):** Two of Unity's OWN bundled AAR modules (`:arcore_client:` and `:unityandroidpermissions:`) declare the same Gradle namespace. AGP under Unity 6.3.17 + Gradle 9.1.0 now enforces namespace uniqueness across modules; older AGP tolerated it. Confirmed by inspecting the generated manifests in `Library/Bee/Android/Prj/IL2CPP/Gradle/...` — our own `Assets/Plugins/Android/AndroidManifest.xml` is clean (camera+internet only) and merges fine; the clash is entirely between Unity's modules.
**Status:** DEFERRED to CORE-001 (the first feature that needs a real AR scene on device). NOT blocking: Editor + Play Mode unaffected, and Section 3 already proved the IL2CPP/ARM64/Vulkan release pipeline builds a clean APK (before ARCore was enabled). Sections 5–8 are all editor/Play-Mode verifiable.
**Planned fix:** Custom Main Gradle Template in Player Settings (Publishing Settings) to resolve the module namespace collision (assign a unique namespace / suppress the uniqueness check). Test against a live AR scene at CORE-001 so the fix is verified with real AR code, not an empty scene.
**Prevention:** When CORE-001 starts, expect to touch `gradleTemplate`/AGP config. Don't re-add ARCore entries to the custom manifest — that's a *different* failure (see below).

---

### [2026-06-18] — Scaffold/Section 4 — AndroidManifest duplicate ARCore entries broke the merger

**Symptom:** First custom `AndroidManifest.xml` (which declared `com.google.ar.core` meta-data + `android.hardware.camera.ar` feature with `tools:replace`) failed the manifest merge.
**Cause:** With XR Plug-in Management ARCore = Required, the ARCore plug-in already injects those exact entries. Declaring them again in our manifest collides — `tools:replace` on the meta-data isn't enough because it's a module-level namespace issue, not a simple attribute conflict.
**Fix:** Slimmed the custom manifest to ONLY camera + internet permissions; removed the ARCore feature and meta-data entirely. (Note: this did not fix the separate Unity-modules namespace clash above, which is upstream of our file.)
**Prevention:** Custom AndroidManifest must never declare `com.google.ar.core` meta-data or `android.hardware.camera.ar` — the plug-in owns those. Keep the custom manifest to permissions the plug-in doesn't inject.

---

### [2026-06-18] — Scaffold/Section 5 — Core fails to compile: "namespace 'Core' does not exist" / "IService not found"

**Symptom:** After adding the data schema/architecture files, `IDataLayer.cs` reported `CS0234 'Core' does not exist in namespace 'AnatomiQ'` and `CS0246 'IService' could not be found`, which cascaded into the test assembly being unable to see any `AnatomiQ.Core` types.
**Cause:** `IDataLayer` was written as `: IService` with `using AnatomiQ.Core;`. `IService` lives in Core; Core already references Data → adding a Data→Core reference would be a circular assembly dependency, which Unity forbids, so Data didn't compile and everything downstream broke.
**Fix:** Removed `: IService` and the Core using from `IDataLayer` (Data now depends on nothing). Added a dedicated `ServiceRegistry.RegisterDataLayer(IDataLayer)` so the data layer is registered without coupling Data back to Core. See the matching decision entry.
**Prevention:** Anything in the Data assembly must not reference Core. If a Data type needs to be registry-held, register it via a typed method on ServiceRegistry, not the generic `Register(IService)` path.

---

### [2026-06-18] — Scaffold — Folder-merge zip extraction on Windows wiped sibling files

**Symptom:** Repeatedly (Sections 1, 2, 5), extracting a delivered zip "merged" into `Assets/_AnatomiQ/` but silently REPLACED sibling folders, deleting their `.asmdef` and stub files (hit `Data`, then `AR/AI/UI`, then the whole `Anatomy` folder). Manifested as missing-assembly compile errors with no obvious cause.
**Cause:** Windows Explorer "merge folder" on extract replaces same-named folders rather than unioning their contents when the source folder is dropped over the destination.
**Fix:** Recovered by delivering one full authoritative `_AnatomiQ` folder (delete old, drop in new — safe at scaffold stage because nothing references scripts by GUID yet, so Unity regenerating `.meta` files is harmless). Then switched delivery to single-file drops / full-folder replacement only.
**Prevention:** Never extract a sibling-structured zip over an existing Assets subtree. Use individual file drops, or a full delete-and-replace of the whole folder. (This becomes UNSAFE once prefabs/scenes reference scripts by GUID — at that point preserve `.meta` files.)

---

### [2026-06-18] — Scaffold — `pinnedPackages` manifest property throws on resolve

**Symptom:** Adding the Unity 6.3 `pinnedPackages` property to `Packages/manifest.json` threw `Cannot read properties of null (reading 'severity')` during package resolution.
**Cause:** The `pinnedPackages` manifest feature misbehaves in this Unity 6.3.17 build.
**Fix:** Abandoned `pinnedPackages`; version-lock instead via the committed `Packages/packages-lock.json` (which records exact resolved versions).
**Prevention:** Don't use `pinnedPackages` on 6.3.17. Commit `packages-lock.json` for reproducible versions.

---

### [2026-06-18] — Tooling — Git Bash mangles pasted multi-line commands

**Symptom:** Pasting a block of git commands into Git Bash produced `bash: $'\E[200~git': command not found` and a cascade of `bash: deleted:: command not found` noise; commands appeared to "not run" (they were swallowed by bracketed-paste escape codes). Notably, a commit/push sometimes DID succeed amid the noise — look for the `[main <hash>]` and `<old>..<new> main -> main` lines to confirm.
**Cause:** Bracketed-paste mode wraps pasted text in `^[[200~ ... ^[[201~` escape sequences that Git Bash mis-tokenizes for multi-line input.
**Fix/Prevention:** Type git commands by hand, or paste ONE line at a time (right-click / Shift+Insert), never a multi-line block. Verify outcome with a hand-typed `git status` afterward.

---

### [2026-06-19] — CORE-008 — PlayMode test won't compile: `LogAssert` not found (CS0103)

**Symptom:** `DataLayerTests.cs` → `CS0103: The name 'LogAssert' does not exist in the current context`, blocking the PlayMode test assembly compile (caught at import, before any test ran).
**Cause:** `LogAssert` lives in `UnityEngine.TestTools`, which wasn't imported. (NUnit's `Assert` is `NUnit.Framework`, but `LogAssert` is Unity's, in a different namespace.)
**Fix:** Added `using UnityEngine.TestTools;`. No asmdef change — the test-framework reference was already present.
**Prevention:** Any PlayMode test that asserts on expected `Debug.LogError`/`LogWarning` via `LogAssert.Expect` needs `using UnityEngine.TestTools;`.

---

### [2026-06-20] — CORE-007 — Real thermal provider + Adaptive Performance enablement  ⚠️ DEFERRED → CORE-001

**Symptom:** None at runtime — by design. `CheckThermal` reads through `IThermalProvider`, which defaults to `NullThermalProvider` (`IsAvailable=false`), so the thermal tier never moves off Nominal in the editor/PlayMode.
**Cause:** Adaptive Performance returns no data without the Android (Google) provider subsystem installed + enabled, and that subsystem only functions on a physical device. Wiring it now would add Project-Settings/subsystem churn to a pure-logic chat and still be unverifiable until device.
**Status:** DEFERRED to CORE-001 (Decision B). The seam, the WarningLevel/TemperatureLevel→tier mapping, the hysteresis, and all tests landed in the logic phase. Only the concrete adapter + settings defer. The concrete `AdaptivePerformanceThermalProvider` is already written in the repo but compiled OUT behind `#if ANATOMIQ_ADAPTIVE_PERFORMANCE`.
**Planned fix (CORE-001 device pass, alongside the ARCore Gradle work):** (1) install the AP Android/Google provider (bundled with Unity 6.3); (2) enable it in Project Settings ▸ Adaptive Performance ▸ Android; (3) add `ANATOMIQ_ADAPTIVE_PERFORMANCE` to Android scripting define symbols; (4) configure `FallbackManager` to use `AdaptivePerformanceThermalProvider` instead of `NullThermalProvider`; (5) verify on the Poco X5 Pro against a real throttling scenario (sustained AR + cascade load).
**Prevention:** none needed — this is a planned staged rollout, not a defect.

---

### [2026-06-20] — CORE-007 — Extending IFallbackManager broke an unseen implementer (Safe Mode)

**Symptom:** After adding `CurrentTier` / `OnPerformanceTierChanged` / `Metrics` to `IFallbackManager`, Unity opened in Safe Mode with three CS0535 errors: `ServiceRegistryTests.MockFallbackManager does not implement interface member 'IFallbackManager.CurrentTier' / '.Metrics' / '.OnPerformanceTierChanged'`.
**Cause:** the EditMode `ServiceRegistryTests.cs` defines its own `MockFallbackManager : IFallbackManager`. Extending the interface broke that implementer. It was missed because the off-Unity compile-check used a different fake and never compiled the real `ServiceRegistryTests.cs`.
**Fix:** added the three members to `MockFallbackManager` (minimal stubs: `CurrentTier` auto-prop, a `FireTier` helper so the event is used, `Metrics` returning a snapshot). Cleared Safe Mode; all 5 EditMode + 14 PlayMode green.
**Prevention:** (1) Before changing any shared interface, enumerate ALL implementers first (here: `FallbackManager` + the test mock). (2) Compile EVERY real file together — including all test assemblies — before declaring a step done, not just the files touched.

---

## Performance Notes

*Log any performance discoveries, optimizations made, or device-specific behavior observed on the Poco X5 Pro.*

---

**[2026-06-18 · scaffold] Benign recurring editor warnings (safe to ignore — NOT performance problems, logged so they aren't re-investigated):**
- URP package shader notes: *`Shader warning in 'TraceVirtualOffset': conversion from larger type ... to smaller type 'min16float'`* — precision-conversion notes inside `com.unity.render-pipelines.core`'s own shader library. Cosmetic, every URP mobile project gets them.
- Inference Engine shader note: *`Shader warning in 'Hidden/Sentis/SliceSet': signed/unsigned mismatch`* — package-internal (`com.unity.ai.inference`), cosmetic.
- Test Framework writes `Assets/Resources/PerformanceTestRunInfo.json` + `PerformanceTestRunSettings.json` on build — junk, git-ignored (see `.gitignore`).

*(Real device performance notes will start at CORE-002 / on-device testing.)*

---

## Dependency Discoveries

*Log any unexpected behavior between features — cases where one feature's implementation affected another in a non-obvious way.*

---

*No dependency discoveries yet.*

---

## Patterns I Keep Hitting

*Cross-cutting lessons that span multiple bugs or features. When you notice the same kind of problem appearing repeatedly, log the pattern here so you can spot architectural smells early.*

> *Example pattern entries (delete when adding real ones):*
>
> *— "Forgetting to await async API calls in event handlers leads to silent failures. Pattern: every event handler that touches AI must be `async void` with full try/catch."*
>
> *— "AR raycasts against organ meshes return inconsistent results when meshes overlap. Pattern: use raycast layer masks per anatomy layer to disambiguate."*

**— Windows zip-extraction folder merges silently delete sibling files.** Three separate breakages in the scaffold came from extracting a folder-structured zip over `Assets/_AnatomiQ/`. Pattern: prefer single-file drops or full delete-and-replace; never let Explorer "merge" a folder over an existing Assets subtree. Once GUIDs are referenced (prefabs/scenes), always preserve `.meta` files.

**— Assembly cycles surface as "namespace/type not found," not as an explicit cycle error.** When a low-layer assembly suddenly can't see a type it should, check for an accidental back-reference creating a Core↔Data style cycle before suspecting the type itself. Keep the DAG strictly one-directional; register cross-layer types via typed methods rather than shared marker interfaces that force a back-ref.

**— "Build failed" ≠ "code broken."** Distinguish compile errors (block editor + Play Mode, must fix now) from release-packaging failures (Gradle/manifest, only block the APK). The latter can be deferred without blocking editor-verifiable work — but only after the cause is *verified* from the generated artifacts, not inferred.

**— Verify package/setting state from the generated artifact, not from assumption.** The ARCore namespace cause was only pinned down by reading the generated manifests under `Library/Bee/...`. When a build/setting issue is ambiguous, inspect what Unity actually generated before deciding a fix.

**— Check the actual authored data, not the flagged assumption.** The carried-forward "physiological_state uses `metabolic`" concern was false on contact with the real JSON (those nodes are `endocrine`); the genuine problem was `cardiovascular` on 7 nodes — found only by auditing every enum string in the content against the enum members. A flagged item is a hypothesis: verify it against the data before "fixing" it, and the same audit usually surfaces the real issue. (Pairs with "verify from the generated artifact.")

---

## Project Risks (live tracking)

*Cross-reference: full risk analysis in AnatomiQ_Operations_And_Planning.md Part B.*

| ID | Risk | Status | Action needed |
|---|---|---|---|
| R1 | Medical reviewer unavailable | 🟡 Open | Identify reviewer before Phase 1 ends |
| R2 | Hardware failure | 🟢 Mitigated | Backup strategy + git backups (GitHub repo now live) |
| R3 | API disruption | 🟢 Mitigated | Provider-agnostic architecture |
| R4 | Package breakage | 🟢 Mitigated | Lock versions in manifest.json |
| R5 | Scope creep | 🔴 **Active vigilance** | Demo run-of-show defines scope. No exceptions. |
| R6 | Technical blocker | 🟢 Strategy ready | Build simple first, time-box exploration |
| R7 | Time overrun | 🟢 Mitigated | Phased milestones at 50/75/90% |
| R8 | 3D model issue | 🟢 Mitigated | Local archive of Z-Anatomy downloaded |
| R9 | Health disruption | 🟢 Mitigated | Front-load progress in early phases |
| R10 | Demo day failure | 🟢 Mitigated | Pre-recorded backup video + demo script |

🔴 = Active monitoring · 🟡 = Open, action needed · 🟢 = Mitigated through architecture/process

---

## Deferred & Out of Scope

*Track anything explicitly decided to defer or cut, so future chats don't re-suggest it.*

| Item | Reason deferred | Revisit when |
|---|---|---|
| RehabAR pillar | Out of scope for academic build | Post-academic |
| ATLAS-006 Body Pose Overlay | Most complex AR feature, Phase 4 only, may be cut | Phase 4 |
| Voice input (Whisper) | Adds complexity, not core to demo | Post-academic |
| iOS build | No iOS test device | After Android is stable |
| Backend server / user accounts | Not needed for academic project | Future product phase |
| Haptic feedback | Hardware dependency, out of scope | Future product phase |
| Multi-user features | Out of scope | Future product phase |
| Branching cascades (multiple paths per disease) | Schema v2 feature | After v1 is shipping |
| Cascade reversal (recovery animations) | Schema v2 feature | After v1 is shipping |
| BioDigital 3D models | Cost prohibitive for academic build | Commercial product phase |
| Procedure schema | Phase 3 dependency, premature now | Start of Phase 3 (CADENCE-001) |
| Symptom pattern schema | Phase 3 dependency, premature now | Start of Phase 3 (PRISM-001) |
| Localization beyond English | Externalize strings from day 1, but ship English only | Post-academic |
| Final logo, app icon, full visual identity | Iterate against real screens | Mid-Phase 3 or commission later |
| ARCore release-APK Gradle namespace fix (`:arcore_client:` vs `:unityandroidpermissions:`) | Needs a real AR scene to verify the gradleTemplate fix against | CORE-001 (first AR feature) |
| Content + test-fixture JSON packing into player builds as TextAssets | Runtime loads `.asset`s via the manifest, not JSON; harmless few-KB until then | When build packaging is wired (exclude `Content/` + `Tests/.../Fixtures/` from the build) |
| CORE-007 thermal provider enablement (`AdaptivePerformanceThermalProvider` + AP Android subsystem + `ANATOMIQ_ADAPTIVE_PERFORMANCE` define) | AP only returns data on a physical device; adapter already written but compiled out | CORE-001 device pass (alongside the ARCore Gradle fix) |
| CORE-007 `OnPerformanceTierChanged` consumer: translate tier → URP levers + show thermal strings + build the A.12 overlay widget | No renderer/UI host exists yet; CORE-007 publishes the signal + the `PerformanceMetrics` contract now | CORE-002 (3D Body Model Renderer) / UI |
| CORE-007 signal stubs `CheckArTracking` / `CheckApiAvailability` / `CheckInferenceState` | Their sources don't exist yet; left as documented stubs | CORE-001 / CORE-006 / on-device inference respectively |

---

## Open Questions

| Question | Status | Action |
|---|---|---|
| Which 3D model source | ✅ RESOLVED | Z-Anatomy / Open3DModel chosen |
| Which medical professional will review cascade content | 🟡 OPEN | Identify before Phase 1 ends — Risk R1 |
| External AI provider | ✅ RESOLVED | Claude primary + OpenAI fallback, provider-agnostic |
| Phase 1 organ list | ✅ RESOLVED | 24 nodes specified in Phase1 Medical Content doc |
| Hypertension and CKD cascades | ✅ RESOLVED | Both authored, awaiting medical review |
| Test fixtures for unit tests | ✅ RESOLVED | minimal_organ_graph.json + minimal_disease.json specified |
| UX navigation model | ✅ RESOLVED | Bottom nav with three pillar tabs + more |
| Visual identity specifics (colors, typeface, logo) | 🟢 INTENTIONALLY DEFERRED | Iterate against real screens |
| Demo content and structure | ✅ RESOLVED v0 | Run-of-show drafted; refine after rehearsals |
| Procedure schema (CADENCE-001) | 🟡 DEFERRED to Phase 3 start | Half-day schema design at Phase 3 start |
| Symptom pattern schema (PRISM-001) | 🟡 DEFERRED to Phase 3 start | Half-day schema design at Phase 3 start |
| Workstation setup (JDK/NDK/SDK versions) | ✅ RESOLVED | JDK 17, NDK r27c, SDK 36.0.0 — install via Unity Hub |
| Git LFS strategy | ✅ DONE | Repo live on GitHub; LFS armed + verified before first binary; `.docx` added to LFS. See [EXECUTION — Step 2] entries |
| Localization technical approach | ✅ RESOLVED | Unity Localization package from day one |
| Cross-cutting service access pattern | ✅ RESOLVED | ServiceRegistry ScriptableObject with interfaces |
| ATLAS-006 ONNX model sourcing | 🟢 INTENTIONALLY DEFERRED | Research at start of ATLAS-006 chat (state changes) |
| Schema v2 migration path | ✅ RESOLVED | Migration script policy specified |
| Package versions at scaffold time | ✅ RESOLVED | AR 6.3.4 pair, Inference 2.5.0, Newtonsoft 3.2.2, Localization 1.5.12, Input 1.19.0; locked via packages-lock.json |
| ARCore release-APK manifest-merge failure | 🟡 DEFERRED → CORE-001 | Custom Main Gradle Template; verified cause = Unity's own module namespace clash under Gradle 9.1.0 |
| physiological_state `system` vs BodySystem enum | ✅ RESOLVED | `metabolic` was a red herring; renamed `BodySystem.Vascular`→`Cardiovascular` (7 nodes). Update Data Schemas §2.3 to match |
| CORE-008 runtime content load mechanism | ✅ RESOLVED | Build-time Editor import → `.asset` + `ContentManifest`; DataLayer re-validates at load |
| CORE-007 degradation-vs-AppState model | ✅ RESOLVED | Two orthogonal axes: `AppState` (AR/connectivity) + new `PerformanceTier` (quality). Published separately; `CurrentTier` = max(fps, thermal) |
| CORE-007 thermal API on Unity 6.3 | ✅ RESOLVED | Adaptive Performance (now CORE in 6.3), NOT `Application.thermalState`. Mapping via `WarningLevel`+`TemperatureLevel`. Concrete provider deferred → CORE-001 |
| CORE-007 monitoring cadence | ✅ RESOLVED | 1s for connectivity/thermal; per-frame collection + 1s evaluation for FPS (a 1Hz sampler can't compute a rolling average) |

---

*Last updated: CORE-007 (Fallback & State Manager) LOGIC PHASE DONE — two-axis model (`AppState` + new `PerformanceTier`); live signals `CheckConnectivity` (debounced → OFFLINE_MODE), `CheckFramerate` (per-frame ring buffer, 1s eval, 30/40 hysteresis), `CheckThermal` (Adaptive Performance mapping, asymmetric heat-fast/cool-slow hysteresis); `max(fps,thermal)` reconciliation; provider seams (`IConnectivityProvider`/`IFrameClock`/`IThermalProvider`) + `InternalsVisibleTo` for tests; thermal localization keys (code) + values (UIStrings via Editor tool); 14 PlayMode + 5 EditMode green; committed + pushed. AR/API/inference checks left as documented stubs. Deferred to CORE-001: real thermal provider enablement (`AdaptivePerformanceThermalProvider` + AP Android subsystem + define). Deferred to CORE-002: tier→URP-lever consumer, thermal-string display, A.12 overlay widget, RAM metric. Next: CORE-001 (AR Session Manager) — also fixes the deferred ARCore release-APK Gradle namespace clash and enables the thermal provider on device.*

*Prior — CORE-008 (Data Layer) DONE — JSON→SO importer + §9 `ContentValidator` + `DataLayer` service (self-registers, re-validates on load) + Editor import tool + Phase 1 content (24 organs, 3 cascades) all green (32 EditMode + 5 PlayMode); committed + pushed. Resolved the physiological_state/BodySystem enum item (→ Cardiovascular). Open: medical review of the 3 cascades (Phase 2 checkpoint), content-JSON build packing.*
