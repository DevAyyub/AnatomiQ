# AnatomiQ — Scaffold Completion Summary

> **Purpose:** Hand-off document at the end of the Unity scaffold chat (Steps 1–8).
> Upload this (with the latest `bugs_and_decisions.md`) at the start of each feature chat
> so the new chat knows exactly what already exists and what's deferred.
> Pair with `bugs_and_decisions.md` for full decision/bug history.

**Status:** Foundation complete. All 8 scaffold sections built, gated, committed, pushed.
**Date:** 2026-06-18 · **Editor:** Unity 6000.3.17f1 LTS (Unity 6.3) · **Repo:** DevAyyub/AnatomiQ (private, `main`)
**Nature of work:** Structure, architecture, schemas, and settings only — NO feature logic written.

> **Current state (updated 2026-06-23):** This document records the *scaffold* milestone (Steps 1–8).
> Since then, **CORE-008 (Data Layer)**, **CORE-007 (Fallback & State Manager)**, and now
> **CORE-001 (AR Session Manager)** are all BUILT — and **CORE-001 + CORE-007 are now DEVICE-VERIFIED
> on the Poco X5 Pro** (full AR session → tracking → state promotion → camera passthrough; thermal +
> FPS tiers live on hardware). CORE-008 remains editor-green (not device-tested). Separately, the
> **T2 Diabetes demo asset set is now PREPARED and imported** — 8 Z-Anatomy meshes isolated, decimated
> (~110k tris total), and assembled at the origin in `Assets/_AnatomiQ/Models/AnatomiQ_Full.glb` (no
> materials yet). This is the geometry CORE-002 will render. The scaffold sections below are left as the
> historical scaffold record; see the **Build order & where to go next** section at the bottom and
> `bugs_and_decisions.md` for the authoritative current state.
> **Next feature: CORE-002 (3D Body Model Renderer)** — assets are now ready for it.

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
  *(Updated 2026-06-23 at CORE-001: the same manifest now also declares the launcher activity
  `com.unity3d.player.UnityPlayerGameActivity` (MAIN/LAUNCHER + `unityplayer.UnityActivity=true`)
  with `android:theme="@style/BaseUnityGameActivityTheme"` — required because the GameActivity entry
  point needs an AppCompat theme. Still no ARCore meta-data/feature here.)*

### CORE-001 AR Session Manager (built + DEVICE-VERIFIED 2026-06-23)
- **Scene:** `Assets/_AnatomiQ/Scenes/AR_Main.unity`, built from GameObject ▸ XR ▸ **XR Origin (Mobile AR)**
  (ARSession + XR Origin + AR Camera with ARCameraManager/ARCameraBackground/TrackedPoseDriver). Single
  **AppCore** GameObject hosts FallbackManager + AppBootstrap + ARSessionManager on one ServiceRegistry.
- **`ARSessionManager`** (`Scripts/AR/`, MonoBehaviour `[DefaultExecutionOrder(-900)]`) implements
  **`IArTrackingProvider : IService`**, self-registers, exposes `Status` + change-only
  `OnTrackingStateChanged`. New Core types: `ArTrackingStatus` enum, `IArTrackingProvider`,
  `NullArTrackingProvider`, `Connectivity` enum. CORE-007 `CheckArTracking` pulls `Status` each tick.
- **Device-verified path on Poco:** session init → `Tracking` → `AppState` promotes
  `AR_VIEWER_MODE`→`AR_ACTIVE` (event fires once) → **camera passthrough renders** → tier holds Nominal
  at 29.9 fps, thermal live (`Temp=0.00`).
- **URP renderer:** `Mobile_Renderer` carries BOTH **AR Background Renderer Feature** (the camera-draw
  pass — its absence was the solid-color-screen bug) and **AR Command Buffer Support Renderer Feature**
  (Vulkan AR support). Graphics APIs Vulkan-first (GLES3 fallback confirmed irrelevant to the bug).
- **Frame rate:** `FallbackManager._targetFrameRate = 30` (serialized). ARCore caps AR render at the
  ~30 fps camera stream on this device (`targetFrameRate=60` does not lift it). FPS tier thresholds are
  now **fractions of target** (0.75/0.87), fixing the false-degrade where the demote threshold equalled
  the cap.
- **Temporary probe:** `ARStatusLogger.cs` (on AppCore) logged tracking/state/tier/FPS/temp to logcat
  for device verification. **Scheduled for deletion** now that the pass is complete (see deferred #7).

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
  *(SUPERSEDED 2026-06-23: CORE-007 fully built + device-verified. Two-axis `AppState`+`PerformanceTier`;
  Connectivity/Framerate/Thermal live; `CheckArTracking` now LIVE (pulls CORE-001's `IArTrackingProvider`).
  Connectivity moved to its OWN axis — `OFFLINE_MODE` removed from `AppState`; added `Connectivity` enum +
  `CurrentConnectivity`/`OnConnectivityChanged` on `IFallbackManager`. Only API/Inference remain documented
  stubs. See bottom section + `bugs_and_decisions.md`.)*
- **Data schemas:** `OrganAsset` (incl. v1.1 `NodeType` {Anatomical, PhysiologicalState} +
  `FmaId`), `DiseaseAsset`, `DiseaseStage`, `CascadeStep`, `VisualEffect`, `AiContext`,
  `OrganMetadata`, `DiseaseMetadata`, and all enums. Public-field style (for JSON importer +
  inspector binding). Procedure/Symptom schemas deferred to Phase 3.
- **CORE-008 Data Layer (built 2026-06-19):** `ContentImporter` (JSON→SO via Newtonsoft `Populate` +
  `StringEnumConverter`/snake_case; three `[JsonProperty]` bridges anatomicalRegion/icd10/visualEffect),
  `ContentValidator` (Data Schemas §9 rules), `DataLayer` service (self-registers via
  `RegisterDataLayer`, re-validates content on load, drop-the-edge-without-mutation), `ContentManifest`
  SO, and the `AnatomiQ → Content → Import JSON` Editor tool (own nested `AnatomiQ.Editor.Content`
  asmdef). Phase 1 content imported to `.asset`s: 24 organs + 3 cascades (T2D/HTN/CKD). `BodySystem`
  `Vascular`→`Cardiovascular`. Full decision/bug detail in `bugs_and_decisions.md`.

### Localization
- English (en) locale, set as Project Locale Identifier.
- String tables: `UIStrings`, `OrganNames`, `DiseaseContent`.
- `ui.app.name = "AnatomiQ"` entry; **Android App Info** metadata configured (clears the
  recurring "Android App Info not configured" warning).
  *(Updated 2026-06-20: CORE-007 added `ui.system.thermal.warning` + `ui.system.thermal.critical`
  to `UIStrings` via an Editor tool. Keys owned by code constants on `FallbackManager`.)*
- Cascade `narrationFallback` stays in JSON, NOT localization (Build Env C.5).

### Asset Prep — T2 Diabetes demo mesh set (Blender → Unity, 2026-06-23)
- **Scope (demo, not academic):** the 8 meshes the T2D cascade touches — body shell, pancreas,
  kidneys (both, fused), heart, eye/retina, coronary vessels, lower-limb nerves. Blood glucose is a
  physiological state (label, no mesh). Glomerulus + systemic microvasculature have no Z-Anatomy mesh →
  label/primitive stand-ins, deferred. Full 25-node set + HTN/CKD meshes deferred to the academic build.
- **Pipeline (demo-scoped 4 steps, not the doc's 8):** isolate → decimate → group into `AQ_` collections
  → glTF export. LOD generation, FMA tagging, and material baking deferred (8 low-poly meshes run fine on
  the Poco without them).
- **Source:** Z-Anatomy `Startup.blend` in **Blender 4.2 LTS** (NOT 4.5+ — the file breaks on 4.5).
  CC BY-SA → attribution owed in credits (already in repo LICENSE).
- **Decimation:** per-mesh Decimate (Collapse) via bpy script — Pancreas/Kidney kept 1.0; Eye/Heart 0.30,
  BodyShell 0.40, Coronary 0.20, Nerves 0.10. **~498k → ~110k tris (−78%)**, well under the 1M budget.
  Leftover Z-Anatomy **Subdivision** modifiers on Heart/Eye were REMOVED (not applied — applying re-inflates).
- **Export:** single `AnatomiQ_Full.glb` with all 8 meshes (one file preserves shared world positions;
  four separate files imported mis-aligned). Exported `materials='NONE'` — the glTF material exporter
  crashes on Z-Anatomy node-groups (`IndexError`). Y-up, modifiers applied.
- **Unity import:** Unity 6 does NOT read `.glb` natively — **glTFast (`com.unity.cloud.gltfast`)** required
  (Package Manager → Install package by name). Imports as a model with a default glTF material → renders
  grey. Sits assembled at origin in `Models/`; URP materials (translucent body shell + colored organs)
  assigned in CORE-002.
- **Repo note:** `AnatomiQ_Full.glb` is a binary → must go through Git LFS (`.gitattributes` already tracks
  `.glb`; `git check-attr` before first push, `git lfs ls-files` after).

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
| glTFast (`com.unity.cloud.gltfast`) | *(added 2026-06-23, Asset Prep)* | Required for `.glb` import — Unity 6 has no native glTF importer. Install via "Install package by name". Re-confirm latest at CORE-002. |

> **⚠️ ARCore package is now FORKED/EMBEDDED (since CORE-001, 2026-06-23).** `com.unity.xr.arcore`
> was copied from `Library/PackageCache` into `Packages/` and its `Runtime/Android/unityandroidpermissions.aar`
> manifest `package` patched `com.google.ar.core` → `com.unity.arcore.permissions` to resolve the AGP 9 /
> Gradle 9.1.0 namespace clash. **Re-apply this AAR edit on any ARCore version bump.** Both `Packages/com.unity.xr.arcore/`
> and `Adaptive Performance` (+ `Adaptive Performance Android` 6.0.0) are now in the project; AP Android Provider
> is selected in Project Settings ▸ Adaptive Performance ▸ Providers, and `ANATOMIQ_ADAPTIVE_PERFORMANCE` is set
> for Android.

---

## Test coverage (all green)
**At scaffold (Steps 1–8):**
- **EditMode:** `ServiceRegistryTests` (3) + `DataSchemaTests` (2).
- **PlayMode:** `FallbackManagerTests` (1).

**Current (updated 2026-06-23, after CORE-008 + CORE-007 + CORE-001):**
- **Total: 61 green (37 EditMode + 24 PlayMode).** *(Check the Test Runner for the live grand total.)*
- **EditMode:** `ServiceRegistryTests` (extended for the AR provider + two-axis interface) + `DataSchemaTests` + CORE-008 suite (`ContentImporterTests` 7 + `ContentValidatorTests` 18 + `Phase1ContentTests` 5).
- **PlayMode:** `FallbackManagerTests` (now 20: connectivity + framerate + thermal + reconciliation + **AR-tracking** mapping; the FPS dead-band test re-primed 35→48 for the new fraction-based thresholds) + CORE-008 PlayMode (5).
- CORE-001's session/tracking/state-mapping logic is additionally **verified on hardware** via logcat (not just unit tests) — see `bugs_and_decisions.md`.
- Standard: aim for high-risk paths, not 100%. CORE-005 needs graph-traversal tests; CORE-006 needs queue/retry/cache tests.

---

## ⚠️ Deferred items carried forward

1. **ARCore release-APK Gradle namespace clash.** ✅ **RESOLVED at CORE-001 (2026-06-23).**
   The planned gradleTemplate route did NOT work — the colliding modules are jetified AAR libraries,
   not Gradle modules. Real cause: Unity's own `unityandroidpermissions.aar` carries a legacy manifest
   `package="com.google.ar.core"` that collides with `:arcore_client:` under AGP 9 / Gradle 9.1.0.
   **Fix:** embedded `com.unity.xr.arcore` into `Packages/` and patched the AAR manifest package →
   `com.unity.arcore.permissions`. Build succeeds. **Trade-off:** forked package — re-patch on any
   ARCore update (see the package-table warning above).

2. **`physiological_state` `system` value vs `BodySystem` enum.** ✅ **RESOLVED at CORE-008.**
   `metabolic` was a red herring (no node uses it); the real mismatch was `cardiovascular` on 7 nodes.
   Renamed `BodySystem.Vascular` → `Cardiovascular`. (Follow-up: Data Schemas §2.3 doc should change
   its `vascular` entry to `cardiovascular` to stay in sync.)

3. **CORE-007 thermal provider enablement.** ✅ **RESOLVED at CORE-001 (2026-06-23).**
   Installed AP + AP Android 6.0.0, set the `ANATOMIQ_ADAPTIVE_PERFORMANCE` define, and
   `AdaptivePerformanceThermalProvider` is active. **The non-obvious step:** a provider must be
   *selected* in Project Settings ▸ Adaptive Performance ▸ **Providers** (tick **Android Provider** /
   ADPF) — "Indexer Active" and "Initialize on Startup" alone are NOT enough. On the Poco: ADPF
   subsystem v34.0.0 init, thermal events flow, `Temp` reads real (0.00 cool) instead of −1.
   *Still to exercise under real throttling load → see item 5 / CORE-002+ (empty scene stays cool).*

4. **CORE-007 `CheckArTracking`.** ✅ **RESOLVED at CORE-001 (2026-06-23).**
   Now LIVE — `FallbackManager.CheckArTracking` pulls `ARSessionManager`'s `IArTrackingProvider.Status`
   each tick and maps it to `AppState` (Tracking→AR_ACTIVE; Limited/NotTracking→AR_LIMITED;
   else→AR_VIEWER_MODE). Connectivity was also split to its own axis (OFFLINE_MODE removed from
   AppState). Confirmed on device. Thermal/perf still does NOT drive AppState (orthogonal `PerformanceTier`).

5. **CORE-007 tier consumer + thermal-string display + A.12 overlay → at CORE-002.** *(MOSTLY RESOLVED at CORE-002, 2026-06-24)*
   ✅ **Tier→URP-lever consumer** live in `BodyRenderer` (render scale / shadow distance / post-processing,
   cache+restore on the active URP asset). ✅ **A.12 debug overlay** built in `AnatomiQ.UI` (UGUI+TMP, 4 Hz,
   sources only `IFallbackManager.Metrics`). ✅ **`PerformanceMetrics.RamMegabytes`** populated via a new
   `IMemoryProbe` seam in CORE-007's monitor loop (Unity-heap proxy, display-only). ✅ **Tier→levers
   confirmed on device** via a dev-only tier-forcer (`DebugSetTierOverride` + overlay buttons, gated to
   editor/dev builds). 🟡 **Still open:** the **thermal user-warning STRING display** (showing the UIStrings
   `ui.system.thermal.warning/.critical` values at Aggressive/Critical) — a small UI consumer, deferred to
   CORE-003/UI. 🟡 **Real-HEAT-driven demotion still unseen** — the Poco stays cool under CORE-002's load
   (110k tris + shell held Nominal/0.00); exercise under CORE-005 cascade load.

6. **CORE-007 `CheckApiAvailability` / `CheckInferenceState` → at CORE-006 / on-device inference.** *(STILL OPEN)*
   Documented stubs; implemented when the AI Orchestrator and on-device inference exist.

7. **Delete the temporary `ARStatusLogger` device probe.** *(BATCHED → ATLAS-003 chunk 6)*
   `Scripts/AR/ARStatusLogger.cs` + its component on AppCore were only for CORE-001 device verification
   (logged tracking/state/tier/FPS/temp to logcat). Now folded into the ATLAS-003 chunk-6 cleanup pass
   alongside undoing the CORE-002 `BodyRoot` +2 m test nudge and stripping the `DEV —` ContextMenu triggers
   (PlacementController + BodyManipulator) — do them together before the chunk-6 device gate.

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
CORE-008 → CORE-007 → CORE-001 → CORE-002 → CORE-003 → CORE-004 → CORE-006 → CORE-005.

**Progress (updated 2026-06-24):**
- **CORE-007** — ✅ DONE + **device-verified** (via CORE-001). Two-axis `AppState` + `PerformanceTier`;
  Connectivity (own axis) / Framerate (fraction-of-target thresholds) / Thermal (ADPF live) all working
  on the Poco; `CheckArTracking` LIVE; API/Inference left as documented stubs; 20 PlayMode + 5 EditMode
  green; committed + pushed. (Tier→URP-lever consumer now built — CORE-002.)
- **CORE-008** — ✅ DONE. Data Layer service + JSON→SO importer + §9 validator + Phase 1 content
  (24 organs, 3 cascades); green; committed + pushed. Not device-tested. Open: medical review of the
  3 cascades (Phase 2 checkpoint); content + test-fixture JSON still packs into player builds as
  TextAssets (harmless few-KB) — exclude `Content/` + fixtures when build packaging is wired.
- **CORE-001** — ✅ DONE + **device-verified on Poco X5 Pro (2026-06-23).** Full path on hardware:
  session → `Tracking` → `AppState` AR_VIEWER_MODE→AR_ACTIVE → camera passthrough renders → tier Nominal
  at 29.9 fps, thermal live. `ARSessionManager : IArTrackingProvider` (pull into CORE-007). Resolved on
  device: ARCore Gradle namespace (embedded fork + AAR patch), GameActivity launcher + AppCompat theme,
  MIUI Install-via-USB, URP AR Background Renderer Feature, FPS calibration, AP provider selection.
  61 tests green; committed + pushed. TODO: delete the `ARStatusLogger` probe (deferred #7).
- **CORE-002 (3D Body Model Renderer) — ✅ DONE + device-verified on Poco (2026-06-24).** Chunks 1–6.
  `BodyRenderer` self-registers as the single `IBodyModelRenderer` (minimal: `ModelRoot` + `IsModelReady`),
  owns a reparentable `BodyRoot` (anchoring left to ATLAS-003), assigns colored organ materials + a custom
  URP Fresnel ghost shell (`AnatomiQ/GhostShell`) via a re-import-resilient runtime name-token map, and
  consumes CORE-007's tier signal → URP levers (render scale / shadow / post, cache+restore on the active
  URP asset). Chunk 5 added the RAM metric (`IMemoryProbe` seam in CORE-007's loop, Unity-heap proxy,
  display-only) and the A.12 debug overlay (`AnatomiQ.UI`, UGUI+TMP, 4 Hz, reads only
  `IFallbackManager.Metrics`). Chunk 6 device gate passed: ghost shell clean over the live feed (no muddying),
  overlay live (RAM ~120/1400 MB, FPS 29.9 at the ARCore 30 cap, thermal 0.00, tier Nominal), tier→URP
  levers visibly engaging via a dev-only tier-forcer (`DebugSetTierOverride` + overlay buttons, gated to
  editor/dev builds). +4 PlayMode this session (RAM, 2× overlay, tier-override), full suite green; committed
  + pushed. **Fact:** `AnatomiQ_Full` = **7** child meshes, not 8 (kidneys fused) — seeds CORE-004's
  organ-ID → mesh-name map. **Open carry-forwards:** thermal user-warning string display (small UI consumer,
  → CORE-003/UI); real-heat tier demotion (→ CORE-005 cascade load); the `BodyRoot` +2 m test nudge to undo
  at ATLAS-003.
- **ATLAS-003 (AR Placement Modes) — 🟡 chunks 1–5 delivered (chunk 5 = 2026-06-24).** `PlacementController :
  IPlacementProvider` owns Surface/Space/Viewer + the placement policy; `BodyManipulator` adds pinch/two-finger
  gestures; geometry is pure Core (`PlacementMath`/`ManipulationMath`). Chunk 5 added AppState reconciliation
  (subscribes to CORE-007's `OnAppStateChanged`, never writes it: AR_VIEWER_MODE→Viewer, AR_LIMITED→screen-pin
  body + tracking-lost toast C.6, AR_ACTIVE→consume pending / re-anchor) arbitrated by the pure EditMode-tested
  `PlacementPolicy`. **D.4 honored:** opens in 3D Viewer with NO launch camera prompt — session gating +
  Viewer↔AR camera presentation moved into CORE-001 (`ARSessionManager.EnterAr()`/`ExitToViewer()`,
  `_enterArOnStart=false`), OS prompt on the AR-Mode tap, placement deferred behind AR bring-up + 12 s watchdog.
  UI in `AnatomiQ.UI` (house style): `PlacementModeSwitcher` (segmented control + D.4 rationale +
  `IArTrackingProvider.Status` explainer) + `TrackingLostToast` (non-modal fade); 11 UIStrings keys authored via
  `PlacementStringsInstaller`. Chunks 1–2 device-verified; **chunks 3–5 await developer ratification of the
  CORE-001 edit (decisions 15–16) + a device pass.** Asmdef TODO: `Unity.Localization`→`AnatomiQ.UI`,
  `Unity.InputSystem`→`AnatomiQ.AR`. Chunk 6 cleanup pending (undo CORE-002 `BodyRoot` nudge, delete
  `ARStatusLogger`, strip `DEV —` triggers, PlayMode tests, device gate).
- **CORE-003 (Layer Toggle System) — ⬅ NEXT in build order.** Per build order; depends on CORE-002 rendering (now done).
  New surface needed on `IBodyModelRenderer` (show/hide layers) — the minimal-interface decision (A) means
  it grows here, with CORE-003 as the first caller. The 7-mesh name list above is the seed.

**Per feature chat:** start a FRESH chat, attach this summary + the latest `bugs_and_decisions.md`,
name the feature ID, and paste any specific existing file the chat needs to extend (e.g.
`FallbackManager.cs` + `IFallbackManager.cs` for CORE-001's AR-tracking + thermal-provider wiring).
Design docs are already in the project knowledge — no need to re-upload them.

---

*Generated at end of Unity scaffold chat (Steps 1–8). Pair with bugs_and_decisions.md.*
