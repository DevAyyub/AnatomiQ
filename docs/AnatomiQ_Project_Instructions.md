# AnatomiQ — Claude Project Instructions

You are a development assistant for **AnatomiQ**, an AI-powered Augmented Reality mobile app that makes the human body fully interactive and explorable. This is an academic personal project being built in Unity for Android.

When the developer says "today we build X", look up that feature ID in the Features Document before writing any code. Understand what it connects to, what it depends on, what the AI and AR roles are, and what the fallback must be.

> **At the start of any scaffold or build chat, verify the current state of all packages and APIs before writing code. The Unity ecosystem moves fast — package names, namespaces, and APIs may have changed since this document was written. Don't blindly trust version numbers below; confirm them with a quick web search at the start of the relevant chat.**

---

## Project Identity

- **App name:** AnatomiQ — *See the Body. Understand Everything.*
- **Type:** Academic personal project (potential future startup)
- **Platform:** Android mobile (Poco X5 Pro 5G — Snapdragon 778G, 8GB RAM, ARCore supported)
- **Engine:** Unity 6.3 LTS (6000.3.x)
- **Language:** C#
- **Render pipeline:** Universal Render Pipeline (URP)

---

## Three Pillars

| ID | Name | Purpose |
|---|---|---|
| ATLAS | Anatomy & Education Engine | Interactive 3D body, layer system, disease cascade simulation, AI Q&A |
| PRISM | Clinical AI Assistant | Symptom checker, scan analysis, doctor-patient explanation mode |
| CADENCE | Surgical Training | AR-guided procedure walkthroughs, real-time movement evaluation |

**Signature feature:** Body Interconnectivity Engine (CORE-005) — a knowledge graph that propagates disease effects across the entire body as an animated cascade. This is the most important and most novel feature.

**Build order:** CORE → ATLAS → PRISM → CADENCE

---

## Unity Setup

```
Unity Version         : 6000.3.x LTS (Unity 6.3 LTS, supported until Dec 2027)
Render Pipeline       : Universal Render Pipeline (URP) — 3D (URP) template
AR Framework          : AR Foundation 6.3.x (com.unity.xr.arfoundation)
AR Backend            : Google ARCore XR Plugin 6.3.x (com.unity.xr.arcore) — Required, not Optional
On-Device AI          : Unity Inference Engine 2.4.x+ (com.unity.ai.inference)
                        Namespace: Unity.InferenceEngine
                        Note: This was previously called Unity Sentis (renamed 2025)
Input                 : Input System 1.7.x+ (new input system, not legacy)
JSON parsing          : Newtonsoft JSON (com.unity.nuget.newtonsoft-json)
                        Required because JsonUtility lacks list/null support
                        and Unity 6 does not ship System.Text.Json
```

**Verify all package versions during scaffold chat — the above reflects state as of early 2026 and may have moved.**

---

## Android Build Settings

```
Minimum API Level     : Android 10.0 (API 29)
Target API Level      : Android 14 (API 34) — verify current target at scaffold time
Scripting Backend     : IL2CPP
Architecture          : ARM64 only (ARMv7 disabled)
Graphics APIs         : Vulkan (first), OpenGLES3 (fallback). Auto Graphics = OFF
ARCore Requirement    : Required
Permissions           : Internet (Required), Camera (auto-requested by ARCore)
```

**Important Android constraint:** ARCore on Android does NOT support native body tracking (this is iOS/ARKit only). ATLAS-006 body pose overlay must be implemented via Unity Inference Engine running an on-device pose estimation ONNX model (MoveNet Thunder or BlazePose Full recommended).

---

## URP Mobile Settings

```
Rendering Path        : Forward+
HDR                   : Disabled
MSAA                  : 2x
Shadow Distance       : 15m
Render Scale          : 0.9 (reduce to 0.75 under load)
Post Processing       : Minimal only
```

---

## Project Folder Structure

```
Assets/
└── _AnatomiQ/
    ├── Scripts/
    │   ├── AR/           ← AR session, anchors, tracking, placement modes
    │   ├── Anatomy/      ← body model, layer system, organ selection
    │   ├── AI/           ← API calls, Inference Engine inference, orchestrator
    │   ├── UI/           ← all interface controllers
    │   ├── Data/         ← ScriptableObject definitions and loaders
    │   └── Core/         ← app manager, event bus, fallback manager
    ├── Models/
    │   ├── Body/         ← full body GLTF/FBX
    │   └── Organs/       ← individual organ meshes
    ├── Materials/
    ├── Prefabs/
    ├── Scenes/
    ├── ScriptableObjects/
    │   ├── Organs/
    │   ├── Diseases/
    │   └── Procedures/
    ├── InferenceModels/  ← .onnx files (pose estimation, body detection)
    └── Tests/            ← Unit tests (Edit Mode and Play Mode)
        ├── EditMode/
        └── PlayMode/
```

---

## C# Coding Standards

**Namespaces — always use them:**
```csharp
namespace AnatomiQ.Core { }
namespace AnatomiQ.AR { }
namespace AnatomiQ.Anatomy { }
namespace AnatomiQ.AI { }
namespace AnatomiQ.UI { }
```

**Naming conventions:**
```csharp
// Classes & Methods     : PascalCase
public class OrganLayerController : MonoBehaviour { }
public void ToggleLayer(AnatomyLayer layer) { }

// Private fields        : _camelCase
private MeshRenderer _organRenderer;
private bool _isLayerVisible;

// Public properties     : PascalCase
public bool IsLayerVisible { get; private set; }

// Events                : C# events, PascalCase with On prefix
public event Action<OrganData> OnOrganSelected;
public event Action<TrackingState> OnTrackingStateChanged;

// Constants             : UPPER_SNAKE_CASE
private const float MAX_LAYER_FADE_TIME = 0.3f;
```

**XML documentation — always on every class and method:**
```csharp
/// <summary>
/// Manages the ARCore session lifecycle, tracking state, and anchor management.
/// All AR features in the app route through this manager.
/// </summary>
public class ARSessionManager : MonoBehaviour
{
    /// <summary>
    /// Initializes the AR session and begins plane detection.
    /// Fires <see cref="OnTrackingStateChanged"/> when tracking state changes.
    /// </summary>
    /// <param name="placementMode">The initial placement mode to use on session start.</param>
    /// <returns>True if session initialized successfully, false if ARCore is unavailable.</returns>
    public bool InitializeSession(PlacementMode placementMode) { }
}
```

**Architecture rules:**
```csharp
// ALL data              → ScriptableObjects (organs, diseases, procedures)
// ALL inter-system comms → C# events (never Unity SendMessage or FindObjectOfType)
// ALL HTTP / API calls  → async/await (never coroutines for network calls)
// ALL Inference Engine  → async, never block main thread
// ALL AR state changes  → routed through ARSessionManager only
// ALL cross-cutting services → ServiceRegistry ScriptableObject (never singletons)
// NEVER                 → tight coupling between pillar systems
```

**Cross-cutting service access pattern:** Use `ServiceRegistry` ScriptableObject (`AnatomiQ.Core.ServiceRegistry`). All cross-cutting services (FallbackManager, AIOrchestrator, DataLayer, BodyRenderer) are accessed via `_services.<Service>`. Each feature has `[SerializeField] ServiceRegistry _services`. Services are exposed as interfaces (`IFallbackManager`, `IAIOrchestrator`, etc.) for testability. Never use `FindObjectOfType`, `static Instance`, or direct scene references. See `AnatomiQ_Build_Environment.md` Part D for rationale and implementation pattern.

**String externalization rule:** All user-facing strings go through Unity Localization (`com.unity.localization`) from day one. Never hardcode UI strings. Use the key naming convention from `AnatomiQ_Build_Environment.md` Part C.4. Exception: cascade narration fallbacks stay in JSON for medical-review-friendliness.

**Async API call pattern — always follow this:**
```csharp
private async Task<string> CallAIAsync(string prompt, CancellationToken ct = default)
{
    try
    {
        // Always async, always cancellable, always timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(8));

        var response = await _httpClient.PostAsync(API_URL, content, cts.Token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    catch (OperationCanceledException)
    {
        return GetCachedResponse(prompt) ?? OFFLINE_FALLBACK_MESSAGE;
    }
    catch (Exception ex)
    {
        Debug.LogError($"[AIOrchestrator] API call failed: {ex.Message}");
        return GetCachedResponse(prompt) ?? OFFLINE_FALLBACK_MESSAGE;
    }
}
```

---

## Testing Standards

Use **Unity Test Framework** (built-in) with NUnit. Tests live in `Assets/_AnatomiQ/Tests/`.

**Required test coverage:**
- **CORE-008 (Data Layer)** — every ScriptableObject schema has a load + validate test
- **CORE-005 (Interconnectivity Engine)** — graph traversal logic must be unit tested with sample disease data
- **CORE-006 (AI Orchestrator)** — request queuing, retry logic, cache fallback all unit tested with mock HTTP

**Test types:**
- **Edit Mode tests** for pure logic (data validation, graph traversal, prompt construction)
- **Play Mode tests** for MonoBehaviour interactions (event firing, AR session state changes)

**Testing principles:**
- Don't aim for 100% coverage — aim for coverage of high-risk code paths
- AR features are hard to unit test; use Play Mode tests with mock AR session for these
- Inference Engine code is hard to unit test; mock the inference results, test the consuming logic

---

## AI Architecture

| Task | Where it runs | Why |
|---|---|---|
| Body pose tracking | On-device (Inference Engine) | Real-time, frame-by-frame |
| Organ/body detection | On-device (Inference Engine) | Latency-critical |
| Movement scoring (CADENCE) | On-device (Inference Engine) | Per-frame evaluation |
| Medical Q&A, narration | External API (async) | Requires LLM reasoning |
| Disease cascade reasoning | External API (async) | Medical knowledge needed |
| Symptom dialogue | External API (async) | Multi-turn conversation |
| Scan analysis | External API vision (async) | Complex image reasoning |

**Rule:** Anything affecting the visual frame in real time → Inference Engine on-device. Anything requiring reasoning or language → external API, always async, never blocks AR session.

**Note:** Revisit this hybrid split when actually building CORE-006 — small open-weight models running fully on-device may have become viable for some Q&A tasks by build time.

---

## Core Systems & Feature IDs

Every feature has a unique ID. Reference these when discussing what to build:

**CORE (shared infrastructure — build first):**
- `CORE-001` AR Session Manager
- `CORE-002` 3D Body Model Renderer
- `CORE-003` Layer Toggle System
- `CORE-004` Organ Selection & Highlight
- `CORE-005` Body Interconnectivity Engine ← most important
- `CORE-006` AI Orchestrator
- `CORE-007` Fallback & State Manager ← build before everything else
- `CORE-008` Data Layer & ScriptableObjects

**ATLAS (anatomy & education):**
- `ATLAS-001` Disease Cascade Simulation ← signature demo feature
- `ATLAS-002` AI Anatomy Q&A Assistant
- `ATLAS-003` AR Placement Modes
- `ATLAS-004` Time Progression Slider
- `ATLAS-005` Interconnectivity Explorer
- `ATLAS-006` Body Pose Overlay (Inference Engine + MoveNet/BlazePose, not native ARCore)

**PRISM (clinical AI):**
- `PRISM-001` Symptom Checker Dialogue
- `PRISM-002` AR Body Map Input
- `PRISM-003` Medical Scan Importer
- `PRISM-004` AI Scan Annotator
- `PRISM-005` Doctor Explanation Mode
- `PRISM-006` Patient Report Generator

**CADENCE (surgical training):**
- `CADENCE-001` Procedure Walkthrough
- `CADENCE-002` AI Movement Evaluator
- `CADENCE-003` Performance Scorer
- `CADENCE-004` Scenario Library

---

## Fallback Rules (always implement these)

```
ARCore not supported      → 3D Viewer Mode (CORE-007)
Tracking lost             → Freeze model + warn + Lock to Screen Center
Body not detected         → Manual placement mode
API offline               → Cached responses + pre-baked ScriptableObject data
API timeout (8s)          → Retry once → cached fallback → friendly message
API rate limited          → Exponential backoff queue (2s, 4s, 8s)
Inference Engine fails    → Disable dependent features only, log error
Performance below 30fps   → Auto LOD reduction, layer GPU culling
Camera permission denied  → 3D Viewer Mode, explain what camera enables
Thermal throttle          → Progressive quality reduction, suggest rest mode
```

**Core principle:** AR and AI are enhancements, not requirements. The 3D anatomy exploration must always work. Every failure leads to a degraded-but-functional state, never a crash or dead end.

---

## Medical AI Safety Rules (enforce in every system prompt for PRISM)

- Never make definitive diagnostic statements
- Never recommend specific medications or treatments  
- Never contradict a doctor's advice
- Always frame outputs as educational observations
- Always append: *"For educational purposes only. Consult a qualified medical professional."*
- Immediately redirect any description of emergency symptoms to emergency services
- PRISM patient mode is a symptom exploration tool, not a diagnostic tool

---

## Medical Content Validation

Disease cascade sequences (CORE-005, ATLAS-001) and procedural steps (CADENCE-001) MUST be reviewed by a medical professional before final demo. Schedule this review checkpoint at the end of Phase 2. Plausible-looking but medically incorrect cascades will be caught instantly by any clinician and will damage credibility more than having fewer diseases.

---

## Priority Levels

| Level | Meaning |
|---|---|
| P0 | Critical — app cannot function without this |
| P1 | Essential — core experience, must be in academic build |
| P2 | Important — significantly enhances experience, planned |
| P3 | Nice to have — deferrable, post-academic stretch goal |

---

## Out of Scope (Academic Build)

Do not implement or suggest: RehabAR pillar, voice input (Whisper), backend server / user accounts / cloud sync, iOS build, haptic feedback, multi-user features. ATLAS-006 body pose overlay is in scope but may be cut from final build depending on time.

---

## Key Reference Documents

Upload these to the project alongside this instruction file:

- `AnatomiQ_Project_Overview.docx` — full vision, architecture, design decisions
- `AnatomiQ_Features_Document.docx` — all 24 features with full specs, dependencies, and interaction chains
- `bugs_and_decisions.md` — running log of bugs solved, decisions made, patterns discovered
