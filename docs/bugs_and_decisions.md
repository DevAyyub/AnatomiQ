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
| CORE-007 | Fallback & State Manager | ⬜ Not started | — |
| CORE-008 | Data Layer & ScriptableObjects | ⬜ Not started | — |
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

*No bugs logged yet — project not started.*

---

## Performance Notes

*Log any performance discoveries, optimizations made, or device-specific behavior observed on the Poco X5 Pro.*

---

*No performance notes yet.*

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

*No patterns logged yet.*

---

## Project Risks (live tracking)

*Cross-reference: full risk analysis in AnatomiQ_Operations_And_Planning.md Part B.*

| ID | Risk | Status | Action needed |
|---|---|---|---|
| R1 | Medical reviewer unavailable | 🟡 Open | Identify reviewer before Phase 1 ends |
| R2 | Hardware failure | 🟢 Mitigated | Backup strategy + git backups |
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
| Git LFS strategy | ✅ RESOLVED | Configured before first binary commit; full .gitattributes ready |
| Localization technical approach | ✅ RESOLVED | Unity Localization package from day one |
| Cross-cutting service access pattern | ✅ RESOLVED | ServiceRegistry ScriptableObject with interfaces |
| ATLAS-006 ONNX model sourcing | 🟢 INTENTIONALLY DEFERRED | Research at start of ATLAS-006 chat (state changes) |
| Schema v2 migration path | ✅ RESOLVED | Migration script policy specified |

---

*Last updated: planning phase complete + gap-fill rounds complete + build-environment round complete · ready for scaffold chat*
