# AnatomiQ — Scaffold Completion Summary

> **Purpose:** Hand-off document at the end of the Unity scaffold chat (Steps 1–8).
> Upload this (with the latest `bugs_and_decisions.md`) at the start of each feature chat
> so the new chat knows exactly what already exists and what's deferred.
> Pair with `bugs_and_decisions.md` for full decision/bug history.

**Status:** Foundation complete. All 8 scaffold sections built, gated, committed, pushed.
**Date:** 2026-06-18 · **Editor:** Unity 6000.3.17f1 LTS (Unity 6.3) · **Repo:** DevAyyub/AnatomiQ (private, `main`)
**Nature of work:** Structure, architecture, schemas, and settings only — NO feature logic written.

---

## What's built

### Environment
- Unity **6000.3.17f1 LTS** (Unity 6.3), URP 3D template, Android platform active.
- Working tree clean; all sections committed + pushed.

### Build configuration (verified by a clean APK in Section 3)
- IL2CPP, ARM64-only, Vulkan→OpenGLES3 (Auto Graphics OFF).
- Min API 29 / Target API 34.
- URP: Android → **Mobile** quality level → `Mobile_RPAsset` (also set as Graphics default).
  Forward+, HDR off, MSAA 2x, render scale 0.9, depth/opaque textures off.
- Render Graph active (the Compatibility Mode toggle is deprecated/removed in 6.3 — its
  absence confirms Render Graph, which AR Foundation 6.3 requires).

### AR
- ARCore enabled + **Required** in XR Plug-in Management.
- Custom `Assets/Plugins/Android/AndroidManifest.xml` — camera + internet permissions only.
  ARCore feature/metadata are intentionally NOT declared (the plug-in injects them).

### Architecture (the spine — EditMode/PlayMode verified)
- **Assembly DAG:** `Data` (no app refs) ← `Core` (refs Data) ← pillars
  `AR` / `Anatomy` / `AI` / `UI`. Pillars never reference each other.
- **ServiceRegistry** (ScriptableObject) with **runtime self-registration**: services call
  `_services.Register(this)` in Awake; ordering via `[DefaultExecutionOrder]`
  (FallbackManager −1000 registers first, AppBootstrap −500 verifies). Services exposed
  as interfaces. No singletons / FindObjectOfType / direct scene refs.
- **IDataLayer** lives in the Data assembly, does NOT extend IService / reference Core
  (cycle avoidance); registered via `ServiceRegistry.RegisterDataLayer(IDataLayer)`.
- **Event bus:** ScriptableObject `EventChannel<T>` (generic base + VoidEventChannel +
  AppStateEventChannel).
- **CORE-007 FallbackManager SHELL:** owns `AppState`, self-registers first, monitoring-loop
  coroutine scaffold with six EMPTY signal-check stubs (CheckArTracking, CheckConnectivity,
  CheckThermal, CheckFramerate, CheckInferenceState, CheckApiAvailability) + a private
  `SetState()` mechanism. No logic yet. `AppBootstrap` verifies startup.
- **Data schemas:** `OrganAsset` (incl. v1.1 `NodeType` {Anatomical, PhysiologicalState} +
  `FmaId`), `DiseaseAsset`, `DiseaseStage`, `CascadeStep`, `VisualEffect`, `AiContext`,
  `OrganMetadata`, `DiseaseMetadata`, and all enums. Public-field style (for JSON importer +
  inspector binding). Procedure/Symptom schemas deferred to Phase 3.

### Localization
- English (en) locale, set as Project Locale Identifier.
- String tables: `UIStrings`, `OrganNames`, `DiseaseContent`.
- `ui.app.name = "AnatomiQ"` entry; **Android App Info** metadata configured (clears the
  recurring "Android App Info not configured" warning).
- Cascade `narrationFallback` stays in JSON, NOT localization (Build Env C.5).

---

## Verified package versions (web-checked June 2026, locked via committed packages-lock.json)
| Package | Version | Notes |
|---|---|---|
| AR Foundation | **6.3.4** | Matched pair — blank version wrongly pulled 6.5.0 (targets 6000.4/6000.5) |
| Google ARCore XR Plugin | **6.3.4** | Must match AR Foundation |
| Inference Engine (`com.unity.ai.inference`) | **2.5.0** | Namespace `Unity.InferenceEngine`; PM may still display "Sentis" |
| Newtonsoft (`com.unity.nuget.newtonsoft-json`) | **3.2.2** | Auto-referenced (all asmdefs) |
| Localization (`com.unity.localization`) | **1.5.12** | |
| Input System | **1.19.0** | From template |

---

## Test coverage (all green)
- **EditMode:** `ServiceRegistryTests` (3) + `DataSchemaTests` (2).
- **PlayMode:** `FallbackManagerTests` (1).
- Standard: aim for high-risk paths, not 100%. CORE-008 needs load+validate tests per schema;
  CORE-005 needs graph-traversal tests; CORE-006 needs queue/retry/cache tests.

---

## ⚠️ Two deferred items carried forward

1. **ARCore release-APK Gradle namespace clash → fix at CORE-001.**
   Release build fails at `:launcher:processReleaseMainManifest`: *"Namespace 'com.google.ar.core'
   used in multiple modules: :arcore_client:, :unityandroidpermissions:."*
   **Verified cause:** two of Unity's OWN bundled AAR modules declare the same Gradle namespace;
   AGP under Gradle 9.1.0 enforces uniqueness. NOT our manifest (confirmed clean). Editor +
   Play Mode unaffected; Section 3 proved the IL2CPP/ARM64/Vulkan pipeline builds.
   **Fix:** Custom Main Gradle Template in Player Settings, tested against a live AR scene at CORE-001.

2. **`physiological_state` `system` value vs `BodySystem` enum → resolve at CORE-008.**
   Medical Content doc suggests `system: "metabolic"` for physiological-state nodes, but
   `BodySystem` has only anatomical systems (no `Metabolic`). Resolve when the importer +
   medical content land (extend the enum or map it).

---

## Benign recurring warnings (safe to ignore)
- URP shader: `TraceVirtualOffset ... min16float` conversion notes (package-internal).
- Inference Engine shader: `Hidden/Sentis/SliceSet signed/unsigned mismatch` (package-internal).
- Test Framework writes `Assets/Resources/PerformanceTestRun*.json` (git-ignored).

---

## Process gotchas (full detail in bugs_and_decisions.md)
- Git Bash mangles pasted multi-line commands (`^[[200~`) — type git commands by hand.
- `pinnedPackages` manifest property throws on resolve in 6.3 — lock via committed packages-lock.json.
- Windows zip "merge folder" silently replaces sibling folders and wipes asmdefs — use single-file
  drops or full delete-and-replace (preserve .meta once GUIDs are referenced by prefabs/scenes).

---

## Build order & where to go next
**CORE → ATLAS → PRISM → CADENCE.** Within CORE:
CORE-007 → CORE-008 → CORE-001 → CORE-002 → CORE-003 → CORE-004 → CORE-006 → CORE-005.

**State of the first two (already partly built by the scaffold):**
- **CORE-007** — shell done; remaining work is the **logic phase** (fill the six signal-check
  stubs + state transitions). Note: ArTracking/Api/Inference checks have no real source until
  CORE-001/CORE-006/inference exist — implement only Connectivity/Framerate/Thermal now.
- **CORE-008** — schemas done; remaining work is the **Data Layer service** (IDataLayer impl,
  JSON→SO importer, §9 validation, load+validate tests, load Phase 1 content).

**Per feature chat:** start a FRESH chat, attach this summary + the latest `bugs_and_decisions.md`,
name the feature ID, and paste any specific existing file the chat needs to extend (e.g.
`OrganAsset.cs`/`DiseaseAsset.cs` for CORE-008, `FallbackManager.cs` for CORE-007). Design docs
are already in the project knowledge — no need to re-upload them.

---

*Generated at end of Unity scaffold chat (Steps 1–8). Pair with bugs_and_decisions.md.*
