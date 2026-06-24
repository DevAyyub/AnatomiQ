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
| CORE-007 | Fallback & State Manager | ✅ Device-verified via CORE-001 — thermal provider live on Poco (ADPF, `Temp` real not −1); FPS calibration fixed (threshold = fraction of target; device caps AR at 30fps); connectivity moved to its own axis (OFFLINE_MODE removed from AppState); `CheckArTracking` now live (pulls `IArTrackingProvider`); API/inference still documented stubs; 20 PlayMode + 5 EditMode green | 2026-06-23 |
| CORE-008 | Data Layer & ScriptableObjects | 🧪 Built — service + importer + §9 validator + Phase 1 content (32 EditMode + 5 PlayMode green); not device-tested | 2026-06-19 |
| CORE-001 | AR Session Manager | ✅ Device-verified on Poco — session→tracking→`AppState` promotion (`AR_VIEWER_MODE`→`AR_ACTIVE`), camera passthrough rendering, `IArTrackingProvider` pull into CORE-007; camera/launcher/Gradle/AP all resolved on device; 61 tests green (37 EditMode + 24 PlayMode) | 2026-06-23 |
| CORE-002 | 3D Body Model Renderer | ✅ Device-verified on Poco — colored organs + custom URP Fresnel ghost shell render cleanly over the live camera feed (no muddying, organs read through); A.12 overlay live (real RAM via Profiler, FPS ~29.9 at the ARCore 30 cap, thermal 0.00, tier Nominal); tier→URP levers confirmed engaging on device via a dev tier-forcer (render scale 0.9→0.75 softening); +4 PlayMode this session (RAM, 2× overlay, tier-override), full suite green; committed + pushed | 2026-06-24 |
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

---

### [EXECUTION — CORE-001] AR scene composition: XR Origin (Mobile AR) + single AppCore host

**Decision:** `AR_Main.unity` built from GameObject ▸ XR ▸ **XR Origin (Mobile AR)** (brings ARSession + XR Origin + AR Camera with ARCameraManager/ARCameraBackground/TrackedPoseDriver). A single **AppCore** GameObject hosts `FallbackManager` + `AppBootstrap` + `ARSessionManager`, all wired to the same `ServiceRegistry`. The stray top-level Main Camera is deleted (the XR Origin owns the camera); XR Origin Camera Y Offset set to 0.
**Reason:** One service host keeps registration order deterministic (`AppBootstrap` at -500, `ARSessionManager` at -900) and matches the ServiceRegistry pattern. Editor "No active XR subsystem" warnings are expected (no AR in editor).
**Alternatives considered:** Separate hosts per service (rejected — ordering fragility, more wiring).

---

### [EXECUTION — CORE-001] ARSessionManager implements IArTrackingProvider — pull, not push

**Decision:** `ARSessionManager` (MonoBehaviour, `[DefaultExecutionOrder(-900)]`) implements `IArTrackingProvider : IService` and self-registers with the ServiceRegistry. CORE-007's `FallbackManager.CheckArTracking` **pulls** `Status` each ~1s tick; tracking changes also broadcast via a change-only `OnTrackingStateChanged` event.
**Reason:** Keeps the dependency one-directional (Core never references AR); CORE-007 stays the single writer of `AppState`. `ArTrackingStatus` {Unavailable, PermissionDenied, Initializing, Tracking, Limited, NotTracking} maps → AppState: Tracking→`AR_ACTIVE`; Limited/NotTracking→`AR_LIMITED`; else→`AR_VIEWER_MODE`.
**Device-verified:** session→`Tracking`→`AR_ACTIVE` promotion fires once, on hardware.

---

### [EXECUTION — CORE-001] Connectivity moved to its own axis (CORE-007 contract change)

**Decision:** Removed `OFFLINE_MODE` from `AppState`; added a separate `Connectivity` enum {Online, Offline} with `CurrentConnectivity` + `OnConnectivityChanged` on `IFallbackManager`. `AppState` is now AR-context-only {AR_ACTIVE, AR_VIEWER_MODE, AR_LIMITED} with a single writer (`CheckArTracking`).
**Reason:** AR-active and offline are orthogonal; folding them into one enum caused an "AR-active-while-offline" flap. Two axes eliminate it. (Supersedes the CORE-007 `CheckConnectivity → OFFLINE_MODE` note in the footer.)
**Alternatives considered:** Keep OFFLINE_MODE in AppState (rejected — flap, ambiguous single-writer).

---

### [EXECUTION — CORE-001] FPS tier thresholds are fractions of target; AR target = 30 on this device

**Decision:** `FallbackManager` exposes a serialized `_targetFrameRate` (set via `Application.targetFrameRate` in Awake) and derives the FPS demote/promote thresholds as **fractions of it** (0.75 / 0.87), not absolute numbers. On the Poco, AR target is **30**, not 60.
**Reason (device-measured):** ARCore drives the AR render at the 30fps camera stream on this device — AP reports `Bottleneck TargetFrameRate` with GPU ~3.6ms / CPU ~5ms frametimes (huge headroom), proving 30 is a deliberate cap, not a perf wall. Fractional thresholds mean a 30-capped device targeting 30 reads as on-target (29.9 > 22.5), so the tier holds Nominal. The old bug: demote threshold sat *at* the cap (both 30) → permanent false-degrade to Critical.
**Alternatives considered:** Target 60 (tested, rejected — unreachable in AR session here); absolute thresholds (rejected — the original bug).

---

### [EXECUTION — CORE-001] Forked/embedded com.unity.xr.arcore to fix Gradle namespace clash

**Decision:** Embedded the `com.unity.xr.arcore` package (copied from `Library/PackageCache` into `Packages/`) and patched `Runtime/Android/unityandroidpermissions.aar`'s manifest `package` from the legacy `com.google.ar.core` → `com.unity.arcore.permissions`.
**Reason:** AGP 9 / Gradle 9.1.0 enforce namespace uniqueness; Unity's own `unityandroidpermissions.aar` carries a legacy `package="com.google.ar.core"` that collides with `:arcore_client:`. This is the only edit that resolved the release-APK manifest merge.
**Trade-off (must remember):** the ARCore package is now forked/embedded — **re-apply the AAR edit on any ARCore package update.** An earlier `allprojects{afterEvaluate}` gradle-hook attempt failed (these are jetified AARs, not Gradle modules).

---

### [EXECUTION — CORE-001] GameActivity launcher + AppCompat theme in custom manifest

**Decision:** `Assets/Plugins/Android/AndroidManifest.xml` explicitly declares the launcher activity `com.unity3d.player.UnityPlayerGameActivity` (MAIN/LAUNCHER intent-filter + `unityplayer.UnityActivity=true`), with `android:theme="@style/BaseUnityGameActivityTheme"`. Application Entry Point = **GameActivity**.
**Reason:** Without the explicit activity the APK installed with no launcher icon ("No activity with MAIN/LAUNCHER"); GameActivity then requires an AppCompat-derived theme (`UnityThemeSelector` crashes with "You need to use a Theme.AppCompat theme").
**Note:** Keep this manifest to launcher + permissions only — never re-add ARCore meta-data/feature (the plug-in owns those).

---

### [EXECUTION — Asset Prep] Demo-scoped Blender pipeline: 4 steps, not the doc's 8

**Decision:** For the 15-min supervisor demo, ran a reduced asset pipeline — (1) isolate body shell + the 8 meshes the T2D cascade touches, (2) decimate, (3) group into `AQ_` collections, (4) glTF export into Unity `Models/`. LOD generation, FMA tagging, and material baking from the doc's 8-step pipeline deferred to the academic build.
**Reason:** 8 low-poly meshes run fine on the Poco without LODs; the demo only needs the T2 Diabetes cascade, not the full 25-node set. Supervisor is non-medical → no medical-reviewer gate for the demo (R1 still open for the academic build).
**Scope (8 meshes):** body shell, pancreas, kidneys (both, fused into one), heart, eye/retina, coronary vessels, lower-limb nerves. Blood glucose = physiological state, no mesh (label only). Glomerulus + systemic microvasculature have NO Z-Anatomy mesh — deferred as label/primitive stand-ins.
**Blender version:** 4.2 LTS — NOT 4.5+. The Z-Anatomy `Startup.blend` (~600 MB) breaks on 4.5. Source CC BY-SA → attribution owed in credits (already in repo LICENSE, R8).

---

### [EXECUTION — Asset Prep] Z-Anatomy mesh structure: fragmented meshes + curves + label objects

**Discovery:** Z-Anatomy organs are not single meshes. Each is a `.g` group mixing solid meshes (triangle icon), curve objects (vessels/nerves are CURVES, not meshes), and text-label annotation objects (`.t` / `.j` suffixes). The `.g` itself is an empty/group.
**Cleanup recipe (per organ):** outliner right-click group → Select Hierarchy → Local View (`/`) → Select → Select All by Type → Mesh (drops labels/curves) → left-click one mesh to make it ACTIVE (this is what un-greys Join) → Ctrl+J → rename `AQ_*` → M into the AQ_ collection → delete leftover label/curve children in outliner.
**Curve gotcha:** vessels and nerves are CURVE objects — Ctrl+J can't fuse curves and they don't export to glTF as geometry. Must run Object → Convert → Mesh FIRST, then join. `.g` empties are skipped by Convert (safe — verified). Label-to-3D-text trap: if a `.t` label gets converted+joined by accident, delete the text island in Edit Mode (Tab → `3` face-select → hover label → `L` → `X` → Faces).

---

### [EXECUTION — Asset Prep] Per-mesh decimation ratios (~498k → ~110k tris, −78%)

**Decision:** Per-mesh Decimate (Collapse) ratios via Blender Python script, not one blanket value. Pancreas/Kidney kept at 1.0 (already ~5k each). Eye 0.30, Heart 0.30, BodyShell 0.40, Coronary 0.20, Nerves 0.10.
**Reason:** converted curve-tubes (coronary, nerves) carry massive redundant ring geometry — nerves were 228k, decimated to ~23k with no visible change at AR scale. Balanced ~110k total fits the Poco (778G) comfortably alongside camera feed + inference (well under the doc's 1M polygon budget).

---

### [EXECUTION — Asset Prep] Remove (don't apply) leftover Subdivision modifiers before export

**Bug:** after decimating, Heart and Eye still carried a Z-Anatomy **Subdivision** modifier. The poly-count script reads the BASE mesh, but glTF export bakes the subdivided result — would have silently 4–16× the count past budget ("Applied modifier was not first" warning was the tell).
**Fix:** REMOVE the subsurf modifier (script), don't apply it — applying re-inflates. After removal, base counts held (Heart 18,840 / Eye 15,196).
**Forward rule:** after decimate, verify every export mesh reports zero leftover modifiers before exporting.

---

### [EXECUTION — Asset Prep] glTF export crashes on Z-Anatomy materials → export with materials='NONE'

**Bug:** `export_scene.gltf(... export_materials='EXPORT')` crashes — `IndexError: list index out of range` in `gltf2_blender_search_node_tree.py`. Cause: a malformed socket in a Z-Anatomy material node-group the glTF material exporter can't parse.
**Fix:** export with `export_materials='NONE'`. Geometry exports clean. Materials don't carry usefully to Unity anyway — URP materials get assigned in CORE-002 (translucent body shell + colored organs). The grey-in-Blender / magenta-in-Unity look is just "no material," expected.

---

### [EXECUTION — Asset Prep] glTFast required for .glb import in Unity 6 (not native)

**Discovery:** Unity 6.3 does NOT import `.glb` natively — files show as "Default Asset" with no model import settings. Install `com.unity.cloud.gltfast` (Package Manager → + → **Install package by name** — the Registry browse-search does NOT find it by ID). Package name web-verified current (June 2026). After install, glTFast auto-registers as the `.glb`/`.gltf` importer and re-imports files as proper models (with a default glTF material, so they render grey not magenta).

---

### [EXECUTION — Asset Prep] Single-file export — multi-file split broke alignment

**Bug:** exporting four separate `.glb`s (Body / Organs / Vessels / Nerves) imported with mismatched origins — glTFast distributed each file's baked world-offset differently across container vs child transforms, so the four pieces landed in different places. Zeroing container transforms made it WORSE (threw away the offsets holding alignment).
**Fix:** re-export ALL eight meshes as ONE `AnatomiQ_Full.glb` (select all AQ_ meshes, single `use_selection=True` export). One file preserves the whole relative layout; drop it in at origin and everything assembles (organs in torso, vessels on heart, nerves in legs) with no per-mesh nudging.
**Forward rule:** for co-located multi-mesh sets, export as a single glb. Zero only the single top parent if you need it at origin — never zero children/sub-containers.

## Bugs & Fixes

*Log every significant bug here — what it was, what caused it, how it was fixed. Format: Feature → Symptom → Cause → Fix.*

---

### [2026-06-18→23] — Scaffold/Build → CORE-001 — Release APK fails: ARCore namespace used in multiple modules  ✅ RESOLVED

**Symptom:** `File → Build` (release APK) fails at `:launcher:processReleaseMainManifest` — *"Manifest merger failed with multiple errors."* Full error: *"Namespace 'com.google.ar.core' is used in multiple modules and/or libraries: :arcore_client:, :unityandroidpermissions:."*
**Cause (VERIFIED, not inferred):** Two of Unity's OWN bundled AAR modules (`:arcore_client:` and `:unityandroidpermissions:`) declare the same Gradle namespace. AGP under Unity 6.3.17 + Gradle 9.1.0 now enforces namespace uniqueness across modules; older AGP tolerated it. Confirmed by inspecting the generated manifests in `Library/Bee/Android/Prj/IL2CPP/Gradle/...` — our own `Assets/Plugins/Android/AndroidManifest.xml` is clean (camera+internet only) and merges fine; the clash is entirely between Unity's modules.
**Fix (VERIFIED on device, CORE-001):** The planned gradleTemplate route did **not** work — the colliding modules are jetified AAR libraries, not Gradle modules, so an `allprojects{afterEvaluate}` namespace hook had no effect. Real root cause: Unity's own `Packages/com.unity.xr.arcore/Runtime/Android/unityandroidpermissions.aar` ships a legacy manifest `package="com.google.ar.core"` (2014-era, no namespace), from which AGP 9 derives a namespace colliding with `:arcore_client:`. Resolved by **embedding** `com.unity.xr.arcore` (copy `Library/PackageCache` → `Packages/`) and patching the AAR manifest `package` → `com.unity.arcore.permissions` (7-Zip in-place). Build then succeeds. Removed the dead allprojects hook.
**Trade-off:** ARCore package is now forked/embedded — re-apply the AAR edit on any ARCore update. (See matching decision entry.)
**Prevention:** Don't re-add ARCore entries to the custom manifest — that's a *different* failure (see below). When updating ARCore, expect to redo the AAR patch.

---

### [2026-06-23] — CORE-001 — URP AR camera renders a solid color; camera passthrough missing (tracking works)

**Symptom:** On device the screen showed a solid clear color (black/yellow depending on clear), even though tracking reached `Tracking` and `AppState` promoted to `AR_ACTIVE`. Persisted under both Vulkan and GLES3, with `ARCameraManager` + `ARCameraBackground` present and the correct `Mobile_RPAsset`/`Mobile_Renderer` active.
**Cause:** The URP renderer was missing the **AR Background Renderer Feature** — the pass that actually *draws* the camera image. We had only added the **AR Command Buffer Support Renderer Feature** earlier, which enables Vulkan AR command-buffer support but is **not** the background-draw pass. With no draw pass, you see the camera's clear color. (Graphics API was irrelevant — confirmed by GLES3 test also failing.)
**Fix:** `Mobile_Renderer` ▸ Renderer Features ▸ Add Renderer Feature ▸ **AR Background Renderer Feature** (keep both AR features). Camera feed renders immediately once tracking is up. Reverted Graphics APIs to Vulkan-first.
**Prevention:** A URP AR renderer needs BOTH features; the command-buffer feature alone is insufficient. Solid-color screen + working tracking = missing AR Background Renderer Feature.

---

### [2026-06-23] — CORE-001 — APK installs but no launcher icon ("No activity with MAIN/LAUNCHER")

**Symptom:** App installed via Build And Run but had no home-screen icon and couldn't be launched; logcat noted no MAIN/LAUNCHER activity.
**Cause:** The custom `AndroidManifest.xml` had an empty `<application tools:node="merge"/>` and didn't declare a launcher activity; with GameActivity entry point, nothing wired MAIN/LAUNCHER.
**Fix:** Declared `com.unity3d.player.UnityPlayerGameActivity` explicitly with a MAIN/LAUNCHER intent-filter + `meta-data unityplayer.UnityActivity=true`.
**Prevention:** With GameActivity, the custom manifest must declare the launcher activity explicitly.

---

### [2026-06-23] — CORE-001 — Launch crash: "You need to use a Theme.AppCompat theme"

**Symptom:** After adding the launcher activity, the app crashed on launch with `IllegalStateException: You need to use a Theme.AppCompat theme (or descendant) with this activity`.
**Cause:** `UnityPlayerGameActivity` (GameActivity) requires an AppCompat-derived theme; the activity was set to `@style/UnityThemeSelector`.
**Fix:** Changed the activity `android:theme` to **`@style/BaseUnityGameActivityTheme`**.
**Prevention:** GameActivity ⇒ AppCompat theme (`BaseUnityGameActivityTheme`), not `UnityThemeSelector`.

---

### [2026-06-23] — CORE-001 — Install blocked: INSTALL_FAILED_USER_RESTRICTED (MIUI)

**Symptom:** `adb`/Build And Run install failed with `INSTALL_FAILED_USER_RESTRICTED`.
**Cause:** MIUI (Xiaomi/Poco) ships with "Install via USB" disabled in Developer Options.
**Fix:** Enable Developer Options ▸ **Install via USB** on the device.
**Prevention:** Poco device-setup checklist item — enable Install via USB once per device.

---

### [2026-06-23] — CORE-007/CORE-001 — FPS tier false-degrades Nominal→Critical and sticks

**Symptom:** On device the `PerformanceTier` walked Nominal→Reduced→Aggressive→Critical within ~10s and stuck, at a steady `FPS=29.9`, with thermal inert.
**Cause:** `FPS_DEMOTE_THRESHOLD` was a hard `30f` while the device's real frame rate is capped at ~30 (ARCore camera). The rolling average sat a hair under (29.9 < 30) every tick → permanent demote; the 40f promote threshold was never reachable → never recovered. A threshold sitting *at* the cap.
**Fix:** Thresholds are now **fractions of `_targetFrameRate`** (0.75 demote / 0.87 promote), set in `FallbackManager.Awake`; AR target set to **30** for this device. At target 30 the demote threshold is 22.5, so 29.9 reads as on-target → tier holds Nominal (device-verified, ~35s steady). One dead-band PlayMode test re-primed 35→48 accordingly.
**Prevention:** A demote threshold must always sit meaningfully below the *reachable* cap, never at it. Tie tier thresholds to the target frame rate, and make the target an explicit, reachable value.

---

### [2026-06-23] — CORE-007/CORE-001 — Adaptive Performance: "Initialization of Provider was not successful"; thermal stuck at −1

**Symptom:** Logcat: `[Adaptive Performance] Initialization of Provider was not successful... select your loader`; `Temp=-1.00` in every snapshot. "Indexer Active" was already checked.
**Cause:** Two separate toggles. "Initialize Adaptive Performance on Startup" was on, but **no provider was selected** in Project Settings ▸ Adaptive Performance ▸ Providers — so the loader started with nothing to load. Indexer-active ≠ provider-selected.
**Fix:** Tick **Android Provider** (generic ADPF) in the Providers list. Leave Samsung (deprecated, Samsung-only) and Basic (stub) off. On next run: `Subsystem version=34.0.0`, ADPF thermal events flow, `Temp=0.00` (real reading; a cool idle device reads 0 thermal-used, not a failure).
**Prevention:** AP needs BOTH "Initialize on Startup" AND a provider checked. On non-Samsung devices use Android Provider (ADPF, API 29+).

---

### [2026-06-23] — CORE-001 — ARSessionManager: obsolete PermissionDeniedAndDontAskAgain (CS0618)

**Symptom:** Warning compiling `ARSessionManager` subscribing to `PermissionCallbacks.PermissionDeniedAndDontAskAgain`.
**Cause:** That callback is deprecated in AR Foundation 6.3.
**Fix:** Removed the subscription; permission-denied is handled via the standard denied path → `PermissionDenied` status → `AR_VIEWER_MODE`.
**Prevention:** On AF 6.x use the current PermissionCallbacks surface; don't subscribe to the deprecated event.

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

### [2026-06-20→23] — CORE-007 — Real thermal provider + Adaptive Performance enablement  ✅ RESOLVED (CORE-001 device pass)

**Symptom:** None at runtime — by design. `CheckThermal` reads through `IThermalProvider`, which defaults to `NullThermalProvider` (`IsAvailable=false`), so the thermal tier never moves off Nominal in the editor/PlayMode.
**Cause:** Adaptive Performance returns no data without the Android (Google) provider subsystem installed + enabled, and that subsystem only functions on a physical device. Wiring it now would add Project-Settings/subsystem churn to a pure-logic chat and still be unverifiable until device.
**Status:** DEFERRED to CORE-001 (Decision B). The seam, the WarningLevel/TemperatureLevel→tier mapping, the hysteresis, and all tests landed in the logic phase. Only the concrete adapter + settings defer. The concrete `AdaptivePerformanceThermalProvider` is already written in the repo but compiled OUT behind `#if ANATOMIQ_ADAPTIVE_PERFORMANCE`.
**Fix (DONE on device):** (1) installed AP + AP Android 6.0.0; (2) `ANATOMIQ_ADAPTIVE_PERFORMANCE` added to Android defines; (3) `AdaptivePerformanceThermalProvider` active behind the define; (4) **the non-obvious step** — selecting the **Android Provider** in Project Settings ▸ Adaptive Performance ▸ Providers (Indexer-active and "Initialize on Startup" alone are NOT enough; see the dedicated bug entry). Result on Poco: ADPF subsystem v34.0.0 initialized, thermal events flow, `Temp` reads real (0.00 on a cool device, `NoWarning`) instead of −1.
**Still to verify later:** behavior under a real throttling scenario (sustained AR + cascade load) — deferred to when there's actual GPU load (CORE-002+); the wiring itself is confirmed live.
**Prevention:** AP needs a provider *selected*, not just installed + indexer-on.

---

### [2026-06-20] — CORE-007 — Extending IFallbackManager broke an unseen implementer (Safe Mode)

**Symptom:** After adding `CurrentTier` / `OnPerformanceTierChanged` / `Metrics` to `IFallbackManager`, Unity opened in Safe Mode with three CS0535 errors: `ServiceRegistryTests.MockFallbackManager does not implement interface member 'IFallbackManager.CurrentTier' / '.Metrics' / '.OnPerformanceTierChanged'`.
**Cause:** the EditMode `ServiceRegistryTests.cs` defines its own `MockFallbackManager : IFallbackManager`. Extending the interface broke that implementer. It was missed because the off-Unity compile-check used a different fake and never compiled the real `ServiceRegistryTests.cs`.
**Fix:** added the three members to `MockFallbackManager` (minimal stubs: `CurrentTier` auto-prop, a `FireTier` helper so the event is used, `Metrics` returning a snapshot). Cleared Safe Mode; all 5 EditMode + 14 PlayMode green.
**Prevention:** (1) Before changing any shared interface, enumerate ALL implementers first (here: `FallbackManager` + the test mock). (2) Compile EVERY real file together — including all test assemblies — before declaring a step done, not just the files touched.

---

### [EXECUTION — CORE-002] 3D Body Model Renderer — chunks 1–4 (renderer, materials, ghost shell, tier consumer)

**Decision A — minimal interface.** `IBodyModelRenderer` exposes only `ModelRoot` + `IsModelReady`. The show/hide/recolor/highlight organ API in the spec is deferred until CORE-004 / ATLAS-001 actually consume it, so the renderer never grows an API surface with no caller.
**Decision B — organ materials owned at runtime, not editor-baked.** A serialized name-token → material map applied in `Awake`, re-finding meshes by name each run. Chosen over editor-baked `sharedMaterial`s because a glTFast re-import of the `.glb` wipes baked assignments; the runtime map is re-import-resilient.
**Decision C — ghost shell = custom URP Fresnel shader.** `AnatomiQ/GhostShell`, unlit/transparent, `ZWrite Off` + `ZTest LEqual` + `Cull Back`, alpha driven by Fresnel so it's near-invisible face-on and rims only at the silhouette. Chosen over flat URP-Lit-transparent (tints the whole feed grey) and over Shader Graph (graph assets don't transfer cleanly as text). Do NOT rely on glTFast's URP transmission for translucency — it needs Opaque Texture ON, which the A.11 mobile budget forbids.
**Decision D — model scene-embedded under a BodyRoot owned by BodyRenderer.** No runtime glTFast load; the imported hierarchy stays in the scene. BodyRoot is a clean, reparentable transform so ATLAS-003 can anchor it later without fighting baked offsets.
**Decision E — tier levers cache + restore the URP asset.** `renderScale`/`shadowDistance` are mutated on the active `UniversalRenderPipelineAsset` (a project asset), so originals are cached on Awake and restored on OnDestroy — otherwise play-mode mutations persist into the asset. If render scale ever looks stuck at 0.75 after a session, a teardown didn't run (editor crash mid-play); reset to 0.9. Mapping: Nominal 0.90/15m/post-on, Reduced 0.85/15m/post-off, Aggressive 0.75/8m/post-off, Critical 0.75/0m/post-off. No LOD-mesh lever yet (model has no LODGroups).

**Boundary held throughout:** CORE-002 renders the body whole, assigns materials, responds to tier. It does NOT do layer toggling (CORE-003) or organ selection/highlight (CORE-004), and exposes no organ-manipulation API. AR anchoring stays ATLAS-003 (BodyRoot at origin).

**Fact established — AnatomiQ_Full = 7 child meshes, not 8:** `AQ_Heart`, `AQ_Coronary`, `AQ_Pancreas`, `AQ_Kidney` (L+R fused), `AQ_Eye`, `AQ_Nerves_Leg`, `AQ_BodyShell`. The "8 meshes" in earlier notes counted kidneys pre-fusion. This 7-name list seeds CORE-004's organ-ID → mesh-name map.

**Test-seam pattern (reused):** `BodyRenderer` follows CORE-007's `internal Configure*ForTest` + `[assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]` pattern (via `AnatomiQ.Anatomy.AssemblyInfo.cs`), guarded with `#if UNITY_INCLUDE_TESTS`. Tests build the host GameObject INACTIVE, inject wiring, then `SetActive(true)` so `Awake` runs against populated fields.

---

### [EXECUTION — CORE-002] Chunk 5 — RAM metric populated + A.12 debug overlay

**Decision F realized — RAM sampled in CORE-007's monitor loop.** `PerformanceMetrics.RamMegabytes` is now populated via a new `IMemoryProbe` seam (real: `UnityMemoryProbe`) added alongside CORE-007's other signal seams, read each `MonitorPass` via `CheckRam`. It is **display-only** — it does NOT drive a tier (RAM-driven degradation waits on the deferred mesh/LOD lever). The figure is a **Unity-heap proxy** (`Profiler.GetTotalAllocatedMemoryLong`): it EXCLUDES ARCore's native ~100–150 MB and graphics-driver memory, so it reads LOWER than Android's reported app memory (on device ~120 MB) — not a leak. Memory Profiler methods need no Development Build, so it reads on a release APK.
**Decision G realized — A.12 overlay built in `AnatomiQ.UI`.** Toggleable on-screen FPS / frame-time / RAM / thermal / tier, sourcing everything from `IFallbackManager.Metrics` so **UI never references Anatomy** (pillar isolation holds). Drawcalls/triangles are editor-only (`UnityStats`, `#if UNITY_EDITOR`); device shows the available subset. GPU-mem / inference / API-queue / battery-temp render as `n/a` placeholders (no source yet) so the layout matches the full A.12 intent. Overlay strings are plain non-localized constants (the dev-tool exception to the day-1 localization rule).
**Decision H (new) — overlay render tech = UGUI + TMP, throttled.** Chosen over IMGUI specifically to honor the allocation-free `PerformanceMetrics` struct: the overlay refreshes at 4 Hz and writes through a cached `StringBuilder` via TMP's `SetText(StringBuilder)`, so while it's on screen during the load test it adds no per-frame GC against the A.2 budget. Self-contained — builds its own top-most Canvas + corner "PERF" toggle + panel in code (and an EventSystem if the scene lacks one), so the only scene step is dropping the component and assigning the ServiceRegistry. Toggle: corner button on device, F1 in editor.

---

### [EXECUTION — CORE-002] Chunk 6 — device gate on Poco + dev tier-forcer

**Device gate PASSED (2026-06-24).** On hardware: (1) the Fresnel ghost shell renders cleanly over the live camera feed — near-invisible face-on, faint silhouette rim, organs (heart, fused kidneys/adrenals, leg nerves) read clearly through it, surrounding feed untouched (the deferred "no muddying" check, now confirmed); (2) the A.12 overlay reads live — RAM real (~120/1400 MB), FPS 29.9 / frame 33.4 ms (both shown red — correctly at the ARCore 30 fps cap, NOT a fault), thermal 0.00, tier Nominal, draws/tris `n/a (device)`; (3) tier→URP levers visibly engage (image softens as render scale drops 0.9→0.75). Temporary stopgap for the test: `BodyRoot` nudged ~+2 m forward so you're not standing inside the model — a scene transform tweak, NOT renderer code; placement stays ATLAS-003's.

**Dev tier-forcer (new, gated `#if UNITY_EDITOR || DEVELOPMENT_BUILD`).** Real thermal demotion was NOT exercised — the Poco stayed cool on this scene (the thermal plumbing is already proven live from CORE-001). To verify the lever path without real heat: added `FallbackManager.DebugSetTierOverride(PerformanceTier?)` — pins the published tier (honored inside `ReconcileTier`, survives the monitor loop until cleared) — and a Nom/Red/Agg/Crit/Auto button strip in the overlay that drives it. This exercises the REAL publish→subscribe→lever chain (CORE-007 publishes → BodyRenderer's subscriber applies levers), not a poked-in value. Both strip from release. (Same temporary-verification-aid spirit as CORE-001's `ARStatusLogger`; removable once trusted.)

---

### [2026-06-24] — CORE-002 — Monitor coroutine runs one MonitorPass synchronously inside OnEnable

**Symptom:** A new RAM test asserting `Metrics.RamMegabytes == -1` *before* the first explicit `TickForTest()` failed — the field already held a real value.
**Cause:** `StartCoroutine` executes a coroutine up to its first `yield` synchronously, and `MonitorLoop` calls `MonitorPass()` BEFORE its first `WaitForSeconds`. So one full pass runs inside `OnEnable` (during `SetActive(true)` in the test harness), and `CheckRam`'s default `UnityMemoryProbe` is LIVE — so RAM is populated before the test's first line. Thermal keeps its `-1` only because its default (`NullThermalProvider`) is inert.
**Fix:** dropped the racy pre-tick assertion; keep the post-tick equality (inject fake probe → tick → assert value).
**Pattern:** in a CORE-007 test, any metric backed by a *live* default provider is already populated before your first assertion. Only fields defaulting to an inert provider retain their sentinel.

---

### [2026-06-24] — CORE-002 — TMP `enableWordWrapping` is obsolete in Unity 6; use `textWrappingMode`

**Issue:** `TMP_Text.enableWordWrapping` is `[Obsolete]` in the Unity 6 TextMeshPro. Use `textWrappingMode = TextWrappingModes.NoWrap` instead (web-verified against the 6.3 TMP API). The old property still compiles but warns; the new one is clean.

---

### [2026-06-24] — CORE-002 — AnatomiQ.UI assembly references for the overlay

**Added to `AnatomiQ.UI.asmdef`:** `Unity.TextMeshPro`, `UnityEngine.UI`, `Unity.InputSystem` (TMP text, UGUI canvas/button, the editor F1 key + the auto-EventSystem's `InputSystemUIInputModule`). New `AnatomiQ.UI.AssemblyInfo.cs` adds `[assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]` for the overlay's test seams (`ConfigureServicesForTest` / `ComposeMetricsText`, both `#if UNITY_INCLUDE_TESTS`). The PlayMode test asmdef needed an explicit `AnatomiQ.UI` reference (same CS0234 class as the earlier `AnatomiQ.Anatomy` one). Safe way to add the refs: the asmdef Inspector's reference picker (lists only real assembly names — no typos).

---

## Performance Notes

*Log any performance discoveries, optimizations made, or device-specific behavior observed on the Poco X5 Pro.*

---

**[2026-06-18 · scaffold] Benign recurring editor warnings (safe to ignore — NOT performance problems, logged so they aren't re-investigated):**
- URP package shader notes: *`Shader warning in 'TraceVirtualOffset': conversion from larger type ... to smaller type 'min16float'`* — precision-conversion notes inside `com.unity.render-pipelines.core`'s own shader library. Cosmetic, every URP mobile project gets them.
- Inference Engine shader note: *`Shader warning in 'Hidden/Sentis/SliceSet': signed/unsigned mismatch`* — package-internal (`com.unity.ai.inference`), cosmetic.
- Test Framework writes `Assets/Resources/PerformanceTestRunInfo.json` + `PerformanceTestRunSettings.json` on build — junk, git-ignored (see `.gitignore`).

**[2026-06-23 · CORE-001 · Poco X5 Pro] First real on-device AR measurements (empty AR scene + camera passthrough):**
- **AR render is capped at ~29.93 fps by ARCore** (the camera stream). AP reports `Bottleneck TargetFrameRate`. `Application.targetFrameRate = 60` does NOT lift it in an AR session — the cap is upstream. So the AR frame-rate target for this device is **30**.
- Massive headroom under that cap: **GPU frametime ~3.1–3.9 ms, CPU ~5 ms** against a 33 ms budget on an empty scene. Plenty of room for CORE-002 geometry + CORE-005 cascade animation before 30 fps is at risk.
- Thermal idle baseline: `SkinTemp=0`, `thermal level=0.00`, `NoWarning`, `ThermalTrend 0` — device cool and untaxed; confirms the thermal axis is reading real data, not throttling.
- `Cluster Info = Big/Medium/Little 0/0/0` and `CPU=-1/-1 GPU=-1/-1` performance levels — ADPF on this device doesn't expose per-cluster core counts or boost levels; only thermal + bottleneck + frametimes are populated. Don't rely on cluster/boost fields on the Poco.

*(Loaded-scene performance notes continue at CORE-002 / on-device testing.)*

**[2026-06-24 · CORE-002 · Poco X5 Pro] First loaded-scene measurements (`AnatomiQ_Full` ~110k tris + Fresnel ghost shell, AR passthrough):**
- Held **FPS 29.9 / frame 33.4 ms** — the same ARCore 30-fps cap as the empty scene; the ~110k-tri body + transparent shell did NOT push it below the cap. Tier stayed **Nominal**, thermal **0.00** over a several-minute hold — the Poco does not thermally throttle on this load.
- RAM (Unity heap, `GetTotalAllocatedMemoryLong`) ~**120 MB** of the 1400 MB ceiling — comfortable. Reads lower than Android's app memory (excludes ARCore native + gfx driver) — expected, not a leak.
- Tier→URP levers confirmed working via the dev tier-forcer: forcing Critical visibly softens the image (render scale 0.9→0.75). Real thermal-DRIVEN demotion remains unseen — needs heavier/sustained load (CORE-005 cascade) to actually heat the Adreno.

---

## Dependency Discoveries

*Log any unexpected behavior between features — cases where one feature's implementation affected another in a non-obvious way.*

---

**[2026-06-23 · CORE-001 ↔ CORE-007] ARCore's 30 fps camera cap drives the CORE-007 FPS tier.** The AR camera stream rate (ARCore) sets the whole app's frame rate in an AR session, which is what the CORE-007 framerate tier measures. The two features are coupled through the frame rate: CORE-007's thresholds must be set against the rate ARCore actually delivers (30 here), not Unity's `targetFrameRate` request. Any future change to the AR camera config (e.g. a 60 fps mode on better hardware) must be reflected in `_targetFrameRate`.

**[2026-06-23 · CORE-001 ↔ CORE-007] CORE-007's thermal axis only goes live once an AR/AP-configured build runs on device.** The thermal provider (`AdaptivePerformanceThermalProvider`) needs the AP Android provider selected + the define set + a physical device — all of which first happened during the CORE-001 device pass. Before that, the thermal tier silently stayed Nominal (`NullThermalProvider`). Anyone testing CORE-007 thermal behavior must do it in a device build, not the editor.

**[2026-06-23 · CORE-001] URP renderer features are an ordered set the AR camera depends on implicitly.** `ARCameraBackground` does nothing visible unless the active renderer carries the **AR Background Renderer Feature**. The dependency isn't expressed in the component — it's a separate asset (the renderer) that must be configured, and it's easy to have the components right and the renderer wrong.

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

**— A threshold must never sit at the cap it's measuring against.** The FPS tier false-degraded forever because the demote threshold (30) equalled the device's real frame cap (30), so the rolling average (29.9) was always "below." Any threshold compared against a capped/clamped signal has to sit meaningfully below the cap — and better, be expressed *relative* to the cap/target so it can't drift into equality. (Generalizes beyond FPS: any "below X = bad" check where X can also be the ceiling.)

**— Test the unreachable setting to learn the real ceiling.** Asking for 60 fps and watching the device still pin at 30 (with frametimes proving idle headroom) is what proved ARCore hard-caps AR render at 30 — you can't infer a cap from a value that's already at it. When a limit is suspected, deliberately request *past* it once; the gap between request and result is the answer. Read AP's `Bottleneck` field and GPU/CPU frametimes, not just the fps number.

**— "Enabled/installed" ≠ "selected/active": watch for two-toggle features.** Adaptive Performance failed to init because a provider wasn't *selected*, even though the package was installed, the Indexer was active, and "Initialize on Startup" was on. Several Unity subsystems gate behind two independent switches (install/enable + select-for-platform). When something "should be on" but reports not-initialized, look for the second toggle before assuming a deeper fault. (Same shape as XR Plug-in Management: package installed vs provider checked per platform.)

**— Solid-color screen + working logic = a missing *render pass*, not a missing component.** Tracking worked, components were present, pipeline was correct — but the camera didn't draw because the renderer lacked the AR Background feature. When the system's data/logic is provably running but nothing shows, suspect the render pipeline configuration (renderer features, active asset) before re-checking components. Confirm which renderer/asset is *actually active* on the platform, not just which one you edited.

**— Diagnose by ruling causes out with cheap tests before changing code.** The Vulkan→GLES3 swap (one build) cleanly eliminated the graphics-API theory; the target-60 build cleanly proved the 30 cap; the GLES3 + screenshot of the renderer dropdown pinned the real fix. Each was a single deliberate build that *removed* a hypothesis. Cheaper than speculative code edits, and it kept the search honest — including catching my own wrong calls (the gradleTemplate route, the command-buffer feature) by testing rather than assuming.

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
| Content + test-fixture JSON packing into player builds as TextAssets | Runtime loads `.asset`s via the manifest, not JSON; harmless few-KB until then | When build packaging is wired (exclude `Content/` + `Tests/.../Fixtures/` from the build) |
| ~~CORE-007 `OnPerformanceTierChanged` consumer: tier → URP levers + A.12 overlay~~ — 🟡 **PARTIAL/DONE** | tier→URP levers ✅ (chunk 4) + A.12 overlay ✅ (chunk 5), both device-verified 2026-06-24 | ✅ done — **except** the thermal user-warning string display (UIStrings `ui.system.thermal.warning/.critical` shown at Aggressive/Critical) is still pending — a small UI consumer; do at CORE-003/UI |
| CORE-007 signal stubs `CheckApiAvailability` / `CheckInferenceState` | Their sources don't exist yet; left as documented stubs (`CheckArTracking` now LIVE via CORE-001's `IArTrackingProvider`) | CORE-006 / on-device inference respectively |
| CORE-007 thermal behavior under real throttling load (sustained AR + cascade) | Provider live on device, but the Poco stays cool under CORE-002's load (110k tris + shell held Nominal/0.00) | 🟡 Demotion LEVERS verified on device via the dev tier-forcer (CORE-002, 2026-06-24); real-HEAT-driven demotion still unobserved — exercise under CORE-005 cascade load |
| Remove temporary `ARStatusLogger` device probe | Used for CORE-001 device verification; delete the script + the AppCore component | Immediately, now CORE-001 is verified |
| Mesh LOD generation + FMA tagging | 8 low-poly meshes run fine on Poco without them for the demo | Academic build |
| ~~Material baking from Z-Anatomy~~ ✅ DONE | Exporter crashes on their node-groups; URP materials assigned in Unity instead | ✅ CORE-002 (2026-06-24) — runtime name-token map assigns colored organ materials + the `AnatomiQ/GhostShell` Fresnel shell |
| Glomerulus + systemic microvasculature meshes | No Z-Anatomy mesh exists; demo uses label/primitive stand-ins | Academic build |
| Full 25-node mesh set + other-disease meshes (Hypertension, CKD) | Demo only needs the T2D cascade | Academic build |

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
| ARCore release-APK manifest-merge failure | ✅ RESOLVED (CORE-001) | Embedded `com.unity.xr.arcore` + patched `unityandroidpermissions.aar` package→`com.unity.arcore.permissions`. gradleTemplate route was wrong (jetified AARs, not modules). Forked package — re-patch on ARCore update |
| physiological_state `system` vs BodySystem enum | ✅ RESOLVED | `metabolic` was a red herring; renamed `BodySystem.Vascular`→`Cardiovascular` (7 nodes). Update Data Schemas §2.3 to match |
| CORE-008 runtime content load mechanism | ✅ RESOLVED | Build-time Editor import → `.asset` + `ContentManifest`; DataLayer re-validates at load |
| CORE-007 degradation-vs-AppState model | ✅ RESOLVED | Two orthogonal axes: `AppState` (AR/connectivity) + new `PerformanceTier` (quality). Published separately; `CurrentTier` = max(fps, thermal) |
| CORE-007 thermal API on Unity 6.3 | ✅ RESOLVED | Adaptive Performance (now CORE in 6.3), NOT `Application.thermalState`. Mapping via `WarningLevel`+`TemperatureLevel`. Concrete provider deferred → CORE-001 |
| CORE-007 monitoring cadence | ✅ RESOLVED | 1s for connectivity/thermal; per-frame collection + 1s evaluation for FPS (a 1Hz sampler can't compute a rolling average) |
| AR frame-rate target on Poco (30 vs 60) | ✅ RESOLVED (CORE-001) | 30 — ARCore caps AR render at the camera's 30fps stream; `targetFrameRate=60` doesn't lift it. FPS tier thresholds now fractions of target |
| URP AR camera passthrough setup | ✅ RESOLVED (CORE-001) | Active renderer needs BOTH AR Background Renderer Feature (draw pass) AND AR Command Buffer Support feature; background feature was the missing piece |
| Adaptive Performance provider on non-Samsung device | ✅ RESOLVED (CORE-001) | Android Provider (ADPF) selected in Providers list; works on Poco (API 34). Samsung provider is Samsung-only; Basic is a stub |
| Android entry point / launcher activity / theme | ✅ RESOLVED (CORE-001) | GameActivity entry point; custom manifest declares `UnityPlayerGameActivity` MAIN/LAUNCHER with `@style/BaseUnityGameActivityTheme` (AppCompat) |
| Z-Anatomy Blender compatibility | ✅ RESOLVED (Asset Prep) | 4.2 LTS only — `Startup.blend` breaks on 4.5+ |
| T2D demo mesh set | ✅ RESOLVED (Asset Prep) | 8 meshes isolated, decimated ~110k, exported, assembled at origin in `Models/AnatomiQ_Full.glb` |
| .glb import in Unity 6 | ✅ RESOLVED (Asset Prep) | glTFast (`com.unity.cloud.gltfast`) required; not native |
| Z-Anatomy material → glTF export | ✅ RESOLVED (Asset Prep) | Exporter crashes on their node-groups; export `materials='NONE'`, assign URP materials in CORE-002 |

---

*Last updated: CORE-002 (3D Body Model Renderer) ✅ DONE + DEVICE-VERIFIED on Poco X5 Pro (2026-06-24) — chunks 1–6 complete. Colored organs + custom URP Fresnel ghost shell (`AnatomiQ/GhostShell`) render cleanly over the live camera feed (no muddying; organs read through); materials owned at runtime by `BodyRenderer` via a re-import-resilient name-token map; model scene-embedded under a reparentable `BodyRoot` (anchoring left to ATLAS-003). Tier→URP-lever consumer live (render scale/shadow/post mapping Nominal→Critical, cache+restore on the active URP asset). Chunk 5: `PerformanceMetrics.RamMegabytes` populated via a new `IMemoryProbe` seam in CORE-007's monitor loop (Unity-heap proxy via `GetTotalAllocatedMemoryLong`, display-only — no RAM→tier demotion yet) + the A.12 debug overlay in `AnatomiQ.UI` (UGUI+TMP, 4 Hz, allocation-free `SetText(StringBuilder)`, sources only `IFallbackManager.Metrics` so UI never references Anatomy; non-localized dev strings). Chunk 6 device gate passed: ghost-over-feed clean, overlay live (RAM ~120/1400 MB, FPS 29.9 at the ARCore 30 cap, thermal 0.00, tier Nominal), tier→URP levers visibly engaging via a dev-only tier-forcer (`DebugSetTierOverride` + overlay button strip, gated to editor/development builds) — real thermal demotion NOT exercised (Poco stays cool; plumbing already proven at CORE-001). +4 PlayMode this session (RAM, 2× overlay, tier-override), full suite green; committed + pushed. Interface kept minimal (`ModelRoot` + `IsModelReady`); 7-mesh fact established (kidneys fused). Open: thermal user-warning STRING display (small UI consumer) still pending; real-heat tier demotion to exercise at CORE-005; the `BodyRoot` +2 m nudge is a temporary test stopgap to undo at ATLAS-003. Next per build order: CORE-003 (Layer Toggle System).*

*Prior — Asset Prep (Blender → Unity, T2D demo mesh set) ✅ DONE — demo-scoped 4-step pipeline (isolate → decimate → group → export) on Z-Anatomy in Blender 4.2 LTS. 8 meshes (body shell, pancreas, both kidneys fused, heart, eye/retina, coronary vessels, leg nerves) isolated via the Select-Hierarchy → Local-View → Select-by-Type-Mesh → Convert(curves)→Mesh → Join → M-to-AQ-collection recipe; decimated per-mesh (~498k→~110k tris, −78%) via bpy script; leftover Subdivision modifiers REMOVED (not applied) on Heart/Eye; exported `materials='NONE'` to dodge the Z-Anatomy node-group exporter crash (IndexError). Unity 6 needs glTFast (`com.unity.cloud.gltfast`, web-verified) — `.glb` not native. Four-file export broke alignment (glTFast scatters baked offsets); fixed by single `AnatomiQ_Full.glb` — all 8 meshes assemble at origin, organs nested in torso. Sitting in `Models/`, grey (no materials). Next: CORE-002 (3D Body Model Renderer) — wire into renderer, assign URP materials (translucent body shell + colored organs), per build order.*

*Prior — CORE-001 (AR Session Manager) ✅ DEVICE-VERIFIED on Poco X5 Pro — full success path on hardware: session init → `Tracking` → `AppState` promotes `AR_VIEWER_MODE`→`AR_ACTIVE` (change-only event) → camera passthrough renders → tier holds Nominal at 29.9fps, thermal live (`Temp=0.00`). `ARSessionManager` implements `IArTrackingProvider` (pull model into CORE-007); connectivity split to its own axis (OFFLINE_MODE removed from AppState); 61 tests green (37 EditMode + 24 PlayMode). Device bring-up resolved end-to-end: ARCore Gradle namespace clash (embed `com.unity.xr.arcore` + patch `unityandroidpermissions.aar` — forked package, re-patch on update); GameActivity launcher + `BaseUnityGameActivityTheme`; MIUI Install-via-USB; URP **AR Background Renderer Feature** (the camera-draw pass — was the solid-color-screen cause); FPS threshold = fraction of target with AR target **30** (ARCore caps render at 30, GPU ~3.6ms/CPU ~5ms headroom proves it's a cap not a wall); Adaptive Performance **provider selection** (ADPF Android — the two-toggle gotcha) → thermal now live. TODO: delete the temporary `ARStatusLogger` probe + its AppCore component. Next per build order: CORE-002 (3D Body Model Renderer) — first real GPU load, will exercise the thermal/tier demotion path and the tier→URP-lever consumer.*

*Prior — CORE-007 (Fallback & State Manager) LOGIC PHASE DONE — two-axis model (`AppState` + new `PerformanceTier`); live signals `CheckConnectivity` (debounced → OFFLINE_MODE), `CheckFramerate` (per-frame ring buffer, 1s eval, 30/40 hysteresis), `CheckThermal` (Adaptive Performance mapping, asymmetric heat-fast/cool-slow hysteresis); `max(fps,thermal)` reconciliation; provider seams (`IConnectivityProvider`/`IFrameClock`/`IThermalProvider`) + `InternalsVisibleTo` for tests; thermal localization keys (code) + values (UIStrings via Editor tool); 14 PlayMode + 5 EditMode green; committed + pushed. AR/API/inference checks left as documented stubs. Deferred to CORE-001: real thermal provider enablement (`AdaptivePerformanceThermalProvider` + AP Android subsystem + define). Deferred to CORE-002: tier→URP-lever consumer, thermal-string display, A.12 overlay widget, RAM metric. Next: CORE-001 (AR Session Manager) — also fixes the deferred ARCore release-APK Gradle namespace clash and enables the thermal provider on device.*

*Prior — CORE-008 (Data Layer) DONE — JSON→SO importer + §9 `ContentValidator` + `DataLayer` service (self-registers, re-validates on load) + Editor import tool + Phase 1 content (24 organs, 3 cascades) all green (32 EditMode + 5 PlayMode); committed + pushed. Resolved the physiological_state/BodySystem enum item (→ Cardiovascular). Open: medical review of the 3 cascades (Phase 2 checkpoint), content-JSON build packing.*
