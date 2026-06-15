# AnatomiQ — Performance Budget & 3D Model Sources

> **Purpose:** This document defines hard performance limits for AnatomiQ on the Poco X5 Pro 5G (Snapdragon 778G) and answers the open question of which 3D anatomy model source to use. Both are needed before CORE-002 (3D Body Model Renderer) starts.
>
> **Why this exists:** Mobile AR + AI is constrained. Without explicit budgets, scope creep from features that "look fine in editor" produces builds that drop frames, crash, or thermal throttle on the actual target device. Setting limits now turns optimization into compliance work rather than firefighting.

---

## Part A — Performance Budget

### A.1 Target device profile

```
Device         : Poco X5 Pro 5G
SoC            : Qualcomm Snapdragon 778G (6nm, May 2021)
CPU            : 1× Cortex-A78 @ 2.4 GHz  (Prime)
                 3× Cortex-A78 @ 2.2 GHz  (Performance)
                 4× Cortex-A55 @ 1.9 GHz  (Efficiency)
GPU            : Adreno 642L @ 550 MHz, ~622 GFLOPS
RAM            : 8 GB LPDDR5-3200
NPU            : Hexagon 770, ~12 TOPS
Display        : 1080 × 2400 AMOLED, 120 Hz capable
AnTuTu 9       : ~525,000 (solid mid-range)
Vulkan         : Yes (preferred), GLES 3.2 (fallback)
```

This is **mid-range**, not flagship. The budget below assumes Vulkan rendering, IL2CPP scripting backend, and ARM64 architecture. ARCore is active in the background for most of the app's runtime, consuming ~5-7ms of per-frame CPU and ~100-150 MB of RAM.

### A.2 Frame time budget at 30 FPS (33.3 ms total)

| Subsystem | Budget | Notes |
|---|---|---|
| ARCore camera + tracking | 5 ms | Plane detection, pose tracking, anchor updates |
| Inference Engine (when active) | 8 ms | Pose model inference (ATLAS-006, CADENCE-002) |
| Application logic | 3 ms | Event dispatch, state management, UI updates |
| Rendering — body model | 12 ms | Mesh draw + skinning if applicable |
| Rendering — UI overlay | 2 ms | Text, panels, icons |
| GC + overhead | 3 ms | Mono GC, frame pacing |
| **Total** | **33 ms** | hard ceiling for 30 FPS |

When Inference Engine is **not** running (most of the app's runtime — pose models only run during ATLAS-006 and CADENCE training), that 8 ms returns to the rendering budget, easily allowing 60 FPS for static viewing.

### A.3 FPS targets per scenario

| Scenario | Target FPS | Minimum FPS | If below minimum |
|---|---|---|---|
| 3D viewer mode (no AR) | 60 | 45 | Reduce render scale to 0.75 |
| AR body model, static | 60 | 45 | Reduce render scale to 0.85 |
| AR body model, cascade animation playing | 45 | 30 | Disable particles, reduce LOD |
| Body pose overlay (ATLAS-006) | 30 | 20 | Skip frames in pose inference (run every 2nd frame) |
| Surgical procedure with AI evaluation | 30 | 25 | Reduce hand pose inference frequency |
| Layer transition animation | 60 | 30 | Skip transition animation, snap to new state |

CORE-007 (Fallback Manager) monitors the 30-frame rolling FPS average. When it dips below the minimum for a given scenario for more than 3 consecutive seconds, automatic degradation kicks in.

### A.4 Memory budget (1.5 GB total app heap soft ceiling)

Android will start killing background apps once your foreground app exceeds ~1.5 GB on an 8 GB device. Beyond ~1.6 GB you risk being killed yourself. Hard ceiling is therefore 1.4 GB with safety margin.

| Category | Soft limit | Notes |
|---|---|---|
| 3D mesh data (all loaded) | 250 MB | All anatomical layers, multi-LOD |
| Compressed textures | 200 MB | ASTC 6x6 for diffuse, ASTC 8x8 for normal maps |
| Inference Engine models | 100 MB | Pose model + classifier + buffer for streaming |
| AR Foundation runtime | 150 MB | Camera buffers, plane meshes, anchor data |
| Unity engine + scripting | 300 MB | IL2CPP runtime, Mono heap, asset loaders |
| AI response cache | 50 MB | JSON cache for offline fallback narration + Q&A |
| UI textures, fonts | 50 MB | Atlas-packed UI, TextMeshPro fonts |
| Working memory & misc | 200 MB | Audio, particle systems, runtime allocations |
| Reserve buffer | 100 MB | Safety margin for spikes |
| **Total soft ceiling** | **1400 MB** | beyond this CORE-007 takes action |

Beyond 1.2 GB → log warning, start unloading inactive disease cascades from cache.
Beyond 1.4 GB → force aggressive LOD reduction, unload non-visible layer meshes from GPU.
Beyond 1.5 GB → the OS may kill the app — should never reach this in practice.

### A.5 Mesh polygon budget

Adreno 642L can push ~3-5 million triangles per frame at sustained 30 FPS under typical mobile conditions. AR camera background occupies a non-trivial slice. Conservative budget for AnatomiQ:

| Layer | Polygon count (LOD 0) | Visible at once |
|---|---|---|
| Skeletal | 200,000 | yes |
| Muscular | 300,000 | yes |
| Vascular | 150,000 | yes |
| Nervous | 100,000 | yes |
| Lymphatic | 50,000 | yes |
| Organs | 200,000 | yes |
| **Total all layers** | **1,000,000** | **maximum 2 layers simultaneously visible** |

CORE-003 (Layer Toggle System) enforces the 2-layer rule. Trying to enable a 3rd layer prompts the user to deselect another first, OR auto-deselects the layer that was activated longest ago.

LOD tiers per mesh:
- **LOD 0 (high)** — full poly count, used when within 1m of camera in AR or when zoomed in viewer mode
- **LOD 1 (medium)** — 35% of LOD 0, used at 1-3m or moderate zoom
- **LOD 2 (low)** — 12% of LOD 0, used at >3m or fallback under thermal throttle

LODs are baked at import time, not generated at runtime. Use Unity's LOD Group component.

### A.6 Drawcall budget

Adreno 642L handles ~300 drawcalls per frame at 30 FPS comfortably with SRP Batcher enabled. AR overlay UI and runtime spawned objects (cascade highlight effects, connection lines) take some of this.

| Source | Drawcall budget |
|---|---|
| Body model (after batching) | 80 |
| AR overlay primitives | 30 |
| Cascade visual effects | 50 |
| UI canvas (TextMeshPro batched) | 40 |
| Particle systems | 30 |
| Misc / overhead | 20 |
| **Total budget per frame** | **250** |

**Required URP settings to stay in budget:**
- SRP Batcher enabled (free CPU savings)
- Static batching enabled for body parts that don't move
- GPU instancing for repeated decorative elements
- Single material per layer (variant atlas) — do not use one material per organ

### A.7 Texture budget

ASTC compression is the right call for Adreno (Vulkan-supported). Texture sizes constrained:

| Asset | Max size | Format | Memory per texture |
|---|---|---|---|
| Body diffuse (per layer) | 2048×2048 | ASTC 6×6 | ~2.7 MB |
| Body normal (per layer) | 2048×2048 | ASTC 8×8 | ~1.4 MB |
| Organ icon (UI) | 256×256 | ASTC 6×6 | ~50 KB |
| Background panel | 1024×1024 | ASTC 6×6 | ~700 KB |
| Inference Engine input buffers | 256×256×3 RGB | uncompressed | ~200 KB transient |

Total texture memory at full load: ~50 MB. Comfortably under the 200 MB texture budget.

Hard rule: **no uncompressed RGBA32 textures in builds**. Set every imported texture's Android override to ASTC and verify in build report.

### A.8 Inference Engine model budget

| Model | Purpose | Size (INT8) | Inference time (target) |
|---|---|---|---|
| MoveNet Thunder | Body pose for ATLAS-006 | ~12 MB | < 25 ms per frame |
| BlazePose Full | Alternative body pose | ~7 MB | < 20 ms per frame |
| YOLOv8n (optional) | Body detection pre-screen | ~10 MB | < 30 ms (not every frame) |
| Hand pose | CADENCE-002 movement eval | ~8 MB | < 20 ms per frame |

**Choose one body pose model, not both.** Recommendation: **MoveNet Thunder** — slightly larger but more accurate, and robust to partial body visibility (anatomy overlay use case often shows torso only).

Models load asynchronously at app start and stay resident in GPU memory. Total model memory: ~30 MB.

### A.9 Network budget

| Operation | Typical size | Frequency |
|---|---|---|
| Cascade narration (one step) | ~200 bytes (50 tokens) | ~1 per 3 seconds during cascade |
| Q&A response | ~2 KB (500 tokens) | user-driven, ~1 per 30 sec typical |
| Scan analysis JSON | ~3 KB | rare, ~1 per session |
| Symptom dialogue turn | ~1 KB | ~1 per 10-15 sec during dialogue |

**Daily data per active user:** typically under 5 MB. Network is not a meaningful constraint unless rate limiting becomes an issue.

**Rate limit defenses:** queue API calls with 100ms minimum spacing; never burst more than 5 calls in 1 second; use exponential backoff (2s, 4s, 8s) on rate limit responses.

### A.10 Thermal budget

Snapdragon 778G is rated ~5W TDP but mobile thermal envelope is shared across the whole device (display, modem, screen). Sustained load at >3W heats the device noticeably within 5-10 minutes, triggering OS thermal throttling that drops CPU clocks 20-40%.

**Detection signals (CORE-007 monitors):**
- 30-frame avg FPS dropping despite no obvious workload increase
- `ThermalState.ThermalStatus` API returning `Moderate` or worse on Android 11+
- Battery level dropping >2% per minute (heat correlates with power draw)

**Response stages:**
1. **Moderate** — reduce render scale to 0.85, disable post-processing
2. **Severe** — pause Inference Engine inference (skip frames or reduce frequency by 50%), reduce shadow distance to 8m
3. **Critical** — show "Device is warm — taking a quick break" overlay, freeze AR session for 30s, reduce all rendering to LOD 2

User-visible warning at "severe": *"Your device is getting warm. AR features may slow down briefly to keep things stable."*

### A.11 URP-specific settings (required for budget)

Locked-in settings for the URP asset:

```
Rendering Path           : Forward+
HDR                      : Disabled
MSAA                     : 2x (Off if FPS struggles)
Depth Texture            : Disabled (enable per-camera if shader needs it)
Opaque Texture           : Disabled
Render Scale             : 0.9 (auto-reduces to 0.75 under load)
Shadow Distance          : 15m
Cascade Count            : 1
Soft Shadows             : Off
Additional Lights        : Per Vertex
SRP Batcher              : Enabled
Native RenderPass        : Enabled (Vulkan)
LOD Cross Fade           : Disabled
Decal Renderer Feature   : Not used
Post-Processing          : Bloom only, low quality, mobile-optimized
Volume Update Mode       : Via Scripting (not every frame)
Store Actions            : Auto
Intermediate Texture     : Auto
```

Rationale based on Unity's official URP mobile guidance: every disabled feature recovers GPU bandwidth and avoids unnecessary render pass overhead on tile-based mobile GPUs.

### A.12 Performance monitoring built into CORE-007

Continuously sample and expose to a debug overlay (toggleable):

```
FPS               : 30-frame rolling avg
Frame time        : ms, with breakdown if Profiler attached
RAM usage         : MB current vs soft ceiling
GPU memory        : MB textures + meshes
Drawcalls         : current frame count
Triangle count    : current frame count
Inference time    : ms per frame (when active)
API queue depth   : pending requests
Thermal state     : OS-reported
Battery temp      : °C if available
```

If any metric exceeds budget for 3+ consecutive seconds, log a warning to bugs_and_decisions.md (auto-appended) for review.

---

## Part B — 3D Model Source Recommendation

### B.1 The decision

**Primary source for academic build: Z-Anatomy / Open3DModel** (BodyParts3D-derived, CC BY-SA 2.1 JP).

**Future commercial source: BioDigital** (when product phase begins).

### B.2 Why Z-Anatomy / Open3DModel

The Open3DModel project (Leiden University, Utrecht, Maastricht, Leuven, Amsterdam — Dutch government grant) builds on Z-Anatomy, which builds on BodyParts3D from the Database Center for Life Science at the University of Tokyo. Three reasons this is the right call:

**1. Genuinely medical-grade, not generic 3D**
Each anatomical structure has a Foundational Model of Anatomy ontology ID (FMA ID) — a standard medical anatomical identifier. This means organs are identified by formal medical convention, not arbitrary names. Maps directly to AnatomiQ's `organId` schema design.

**2. University-backed and peer-reviewed**
Original BodyParts3D paper published in Nucleic Acids Research (2009). Z-Anatomy/Open3DModel actively maintained by anatomists at Leiden, Utrecht, Maastricht, Leuven, Amsterdam UMC, Radboud UMC, and Ghent University. The entire model has been reviewed by working anatomists. AnatomiQ inherits this credibility for free.

**3. Open license suitable for academic use**
CC BY-SA 2.1 JP (Creative Commons Attribution-ShareAlike). Free to use, modify, redistribute. Academic project: zero cost. Commercial product later: more complex (see B.5).

**Specific properties:**
- 5000+ anatomical structures in Z-Anatomy
- 1500+ in original BodyParts3D
- All systems covered: skeletal, muscular, vascular, nervous, organs
- Available in Blender source format (editable for mobile optimization)
- Already retopologized (cleaner meshes than raw BodyParts3D)
- Material-coded by anatomical system (matches AnatomiQ's layer design)

### B.3 Where to get it

| Source | URL | Notes |
|---|---|---|
| Z-Anatomy (Blender) | https://lluisv.itch.io/z-anatomy | The most user-friendly entry point; free download, Blender source |
| Open3DModel (Leiden) | https://anatomytool.org/open3dmodel | Most actively maintained; teacher-focused submodels available |
| BodyParts3D (original) | https://lifesciencedb.jp/bp3d | Source data; OBJ files; FMA IDs |
| BodyParts3D mirror (GitHub) | https://github.com/Kevin-Mattheus-Moerman/BodyParts3D | Cleaner repo, STL format |

**Recommended path:** start with Z-Anatomy in Blender. It already has the materials, anatomical labels, and system separation set up. Download → open in Blender → export selected layers as glTF for Unity.

### B.4 Asset preparation pipeline

The raw Z-Anatomy meshes are too high-poly and too numerous for direct mobile use. They need preparation:

```
Raw Z-Anatomy (Blender)
        │
        ▼
Step 1: Filter to scope
  Keep only structures referenced in AnatomiQ organ data.
  ~150-300 structures for academic build, not all 5000.
        │
        ▼
Step 2: Group by layer
  Skeletal collection, muscular collection, etc.
  Match AnatomiQ's 6-layer schema.
        │
        ▼
Step 3: Decimate per layer
  Use Blender's Decimate modifier.
  Target poly counts from A.5.
  Preserve UVs and material IDs.
        │
        ▼
Step 4: Generate LODs
  LOD 1 = 35% of LOD 0
  LOD 2 = 12% of LOD 0
        │
        ▼
Step 5: Bake materials
  Convert per-structure materials to per-layer atlas materials.
  Reduces draw calls dramatically.
        │
        ▼
Step 6: Set FMA ID as custom property per object
  This becomes the meshId in the OrganAsset.
        │
        ▼
Step 7: Export glTF 2.0 per layer
  One .gltf per layer (skeletal.gltf, muscular.gltf, etc.)
  Allows Unity to load layers independently.
        │
        ▼
Step 8: Import to Unity
  Set platform to Android.
  Compress meshes (Mesh Compression: Medium).
  Attach LOD Group component.
        │
        ▼
Ready for CORE-002
```

**Time estimate for first-pass preparation:** 2-3 weeks of Blender work for someone familiar with the tool. If unfamiliar, factor in another week for learning. Worth doing carefully — the asset pipeline pays back across the entire project.

**Alternative if Blender expertise is limited:** the Open3DModel project provides "selection submodels" — pre-built smaller models showing specific topics (cardiovascular system, musculoskeletal, etc.). These are already lighter and may serve as a starting point.

### B.5 License compliance

CC BY-SA 2.1 JP requires:

1. **Attribution** — credit the source in a visible location in the app
2. **ShareAlike** — derivative works must use the same license
3. **No restriction** — free to use commercially in principle

Required attribution text (paste verbatim into AnatomiQ credits screen):

```
3D anatomy data based on:
  • BodyParts3D, ©2008 Life Science Integrated Database Center
  • Z-Anatomy by Lluís Vinent, 2021
  • Open3DModel by Leiden UMC, Utrecht UMC, Maastricht UM, KU Leuven,
    Amsterdam UMC, Radboud UMC, and Ghent University, 2022-

Licensed under Creative Commons Attribution-ShareAlike 2.1 Japan.
```

**The ShareAlike clause is the constraint for commercial use later.** Any modifications AnatomiQ makes to the meshes (decimation, retopology, material baking) become CC BY-SA derivatives, meaning if AnatomiQ ships the modified meshes commercially, they must remain CC BY-SA. This means competitors could legally reuse AnatomiQ's optimized meshes.

For a real commercial product, two paths:

- **Path A:** Stay CC BY-SA. Compete on the AI / Interconnectivity Engine / UX layer. The meshes themselves are commodity; the value is what AnatomiQ does with them.
- **Path B:** License BioDigital's library (proprietary, ~$10-50k/year for commercial license depending on scale). Higher visual fidelity, fully owned by BioDigital. AnatomiQ uses their Unity SDK.

For the academic build: Path A. Decide between A and B during the startup phase, not now.

### B.6 BioDigital alternative (deferred)

BioDigital Human is the industry-standard medical 3D content library used by Complete Anatomy and others. Properties:

- Highest visual fidelity in the consumer anatomy app space
- Web SDK, Unity SDK, and licensable model files
- Pricing not public; reportedly $10-50k/year for commercial licensing
- Their content cannot be modified beyond what their SDK exposes
- Their licensing terms are typically narrower than CC BY-SA

**Worth contacting BioDigital during the startup phase for an enterprise quote**, but **not for the academic build**. Use Z-Anatomy now and revisit when there's revenue or institutional funding to support the cost.

### B.7 Other sources considered and why not

| Source | Why not |
|---|---|
| Sketchfab anatomy models | Variable quality, inconsistent licensing per model, no anatomical ontology IDs |
| TurboSquid / CGTrader paid models | Per-model purchase, no medical review, expensive in aggregate |
| MakeHuman | Surface anatomy only, not internal structures |
| CC0 generic medical illustrations | Visual quality fine, but no FMA IDs and no system organization |
| Custom commission | $20-50k for medical-grade quality. Out of scope for academic build. |

### B.8 Open organ ID question — resolved

This connects to the data schema document. The `organId` naming convention can be **directly aligned with FMA IDs** since Z-Anatomy already uses them. Recommended adjustment:

Rather than:
```
organId: "pancreas_beta_cells"
meshId: "Body_Pancreas_BetaCells_LOD0"
```

Use:
```
organId: "pancreas_beta_cells"        # human-readable for prompts and code
fmaId: "FMA62630"                     # canonical medical anatomical ID
meshId: "Body_Pancreas_BetaCells_LOD0" # Unity mesh component
```

The FMA ID becomes the bridge between AnatomiQ's data and Z-Anatomy's mesh names. It also future-proofs the data — any other anatomy database that uses FMA can be cross-referenced.

**Recommendation:** add `fmaId` as an optional field in the OrganAsset schema (data schemas v1.1 update, deferred to next iteration).

---

## Part C — Performance Testing Protocol

### C.1 Pre-build checklist (before each device build)

```
[ ] No texture larger than 2048×2048 in Android override
[ ] All textures use ASTC compression
[ ] Mesh compression set to Medium for all body meshes
[ ] LOD groups configured for all body meshes
[ ] Inference Engine model size total < 30 MB
[ ] No lights other than 1 directional light + ambient
[ ] HDR disabled in URP asset
[ ] Post-processing limited to mobile-optimized bloom
[ ] Build report shows < 250 MB compressed APK size
```

### C.2 On-device test scenarios

Each scenario runs for 5 minutes minimum. FPS, RAM, and thermal logged every 5 seconds. Run after every major feature addition, not at the end.

| Scenario | Pass criteria |
|---|---|
| 1. Idle 3D viewer mode, body model visible, no AR | Stable 60 FPS, RAM stable, no thermal warnings |
| 2. AR placement mode, model on a flat surface, no other features | Stable 45+ FPS, RAM under 1 GB |
| 3. Disease cascade simulation playing (T2 Diabetes), AR active | Stable 30+ FPS, AI calls succeed, narration plays |
| 4. AI Q&A with 10 consecutive questions during cascade | No frame drops below 25 FPS, all responses arrive |
| 5. Body pose overlay (ATLAS-006), Inference Engine active | 20+ FPS, pose detection visible, no crashes |
| 6. 30-minute soak test on cascade replay loop | No memory growth >100 MB from start, thermal stable |

If any scenario fails, the failure goes in bugs_and_decisions.md as a P0 before continuing development.

### C.3 What to log per build

Save these metrics per build to track regressions over time:

```
Build version
Build date
APK size (compressed and uncompressed)
Cold start time (app launch to AR session ready)
Cascade time (start to last narration finished)
Peak RAM during cascade
Average FPS per scenario (1-6 above)
Thermal events count
Crash count
```

Trend visualization helps catch regressions: if peak RAM jumps 200 MB between builds, something's wrong with the new feature.

---

*Performance budget v1 · 2026 · Pair with AnatomiQ_Project_Instructions.md and AnatomiQ_Data_Schemas.md*
