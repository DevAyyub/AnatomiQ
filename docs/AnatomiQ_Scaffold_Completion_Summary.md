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
> FPS tiers live on hardware). CORE-008 remains editor-green (not device-tested). The scaffold sections
> below are left as the historical scaffold record; see the **Build order & where to go next** section
> at the bottom and `bugs_and_decisions.md` for the authoritative current state.
> **Next feature: CORE-002 (3D Body Model Renderer).**

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

5. **CORE-007 tier consumer + thermal-string display + A.12 overlay → at CORE-002.** *(STILL OPEN)*
   CORE-007 publishes `OnPerformanceTierChanged` and the `PerformanceMetrics` snapshot, plus defines
   the thermal-string keys (`ui.system.thermal.warning/.critical`, values authored in `UIStrings`).
   No consumer exists yet: CORE-002's renderer translates tier → URP levers, a UI layer shows the
   strings at Aggressive/Critical, and the on-screen A.12 debug overlay is built then. RAM field in
   `PerformanceMetrics` is also populated at CORE-002. **CORE-002 is also where the now-live thermal
   axis first gets real GPU load to exercise demotion.**

6. **CORE-007 `CheckApiAvailability` / `CheckInferenceState` → at CORE-006 / on-device inference.** *(STILL OPEN)*
   Documented stubs; implemented when the AI Orchestrator and on-device inference exist.

7. **Delete the temporary `ARStatusLogger` device probe.** *(IMMEDIATE)*
   `Scripts/AR/ARStatusLogger.cs` + its component on AppCore were only for CORE-001 device verification
   (logged tracking/state/tier/FPS/temp to logcat). Remove both now that the pass is complete — it was
   never meant to ship.

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

**Progress (updated 2026-06-23):**
- **CORE-007** — ✅ DONE + **device-verified** (via CORE-001). Two-axis `AppState` + `PerformanceTier`;
  Connectivity (own axis) / Framerate (fraction-of-target thresholds) / Thermal (ADPF live) all working
  on the Poco; `CheckArTracking` LIVE; API/Inference left as documented stubs; 20 PlayMode + 5 EditMode
  green; committed + pushed. (Tier→URP-lever consumer still at CORE-002.)
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
- **CORE-002 (3D Body Model Renderer) — ⬅ NEXT.** First feature with real geometry/GPU load, so it's
  where CORE-007's `OnPerformanceTierChanged` finally gets a consumer (tier → URP render-scale/LOD/cull
  levers), the thermal axis gets exercised under real load, the thermal UI strings get displayed, the
  A.12 debug overlay is built, and `PerformanceMetrics.RAM` is populated (deferred #5). Verify current
  AR Foundation / URP / model-import package versions at the start of that chat (Unity moves fast).

**Per feature chat:** start a FRESH chat, attach this summary + the latest `bugs_and_decisions.md`,
name the feature ID, and paste any specific existing file the chat needs to extend (e.g.
`FallbackManager.cs` + `IFallbackManager.cs` for CORE-001's AR-tracking + thermal-provider wiring).
Design docs are already in the project knowledge — no need to re-upload them.

---

*Generated at end of Unity scaffold chat (Steps 1–8). Pair with bugs_and_decisions.md.*
