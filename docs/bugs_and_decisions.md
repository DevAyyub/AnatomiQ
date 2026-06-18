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
| CORE-007 | Fallback & State Manager | 🧪 Shell built (scaffold), not device-tested | 2026-06-18 |
| CORE-008 | Data Layer & ScriptableObjects | 🧪 Schemas built (scaffold), not device-tested | 2026-06-18 |
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
| `physiological_state` node `system` value vs `BodySystem` enum (no `Metabolic` member) | Schema/importer concern; no importer or medical content loaded yet | CORE-008 (Data Layer) |

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
| physiological_state `system` vs BodySystem enum | 🟡 DEFERRED → CORE-008 | Reconcile when JSON importer + medical content land |

---

*Last updated: Unity scaffold chat (Steps 1–8) DONE — project compiles, all gates passed (EditMode 5 tests + PlayMode 1 test green; Section 3 clean APK), committed + pushed. Two items deferred forward: ARCore release-APK Gradle fix → CORE-001; physiological_state/BodySystem enum → CORE-008. Ready for first feature chat (suggested: CORE-001, or CORE-008 for AR-independent progress).*
