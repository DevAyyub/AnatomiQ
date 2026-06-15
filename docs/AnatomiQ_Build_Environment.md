# AnatomiQ — Build Environment

> **Purpose:** Resolves environmental and architectural decisions that the first build chats will hit immediately. Without these, the scaffold chat improvises answers in the moment that may not be ideal and won't be consistent.
>
> **Scope:** This is not product strategy. It's the developer-machine and repository-level decisions that make Steps 2–6 of the execution plan run smoothly.
>
> **When to read:** Before Step 2 (Git setup) and Step 4 (Scaffold). Re-read before the first feature chat (Step 6) for the service access pattern.

---

## Part A — Developer Workstation Setup

### A.1 The decision

**Use Unity Hub to install everything.** Unity Hub bundles the exact JDK / SDK / NDK versions that match your Unity editor version. Manual installation is technically possible but creates version mismatches that cause cryptic "build failed" errors that take hours to diagnose.

### A.2 Verified versions for Unity 6.3 LTS

Per Unity's official supported dependency matrix (verified for Unity 6.3 LTS):

| Component | Required version | Notes |
|---|---|---|
| **JDK** | OpenJDK 17 | Same as Unity 6.0+. Older JDK 11 will cause Gradle errors. |
| **Android NDK** | r27c (27.2.12479018) | Pinned exact version. Mismatched NDK = silent IL2CPP failures. |
| **Android SDK Build Tools** | 36.0.0 | Auto-managed if installed via Hub. |
| **Android SDK Command-line Tools** | 16 | Auto-managed if installed via Hub. |
| **Android SDK Platform Tools** | 36.0.0 | Auto-managed if installed via Hub. |
| **CMake** | 3.22.1 | Required for Unity 6+. Auto-installed with NDK. |
| **Min Android API** | 29 (Android 10) | Per Project Instructions. |
| **Target Android API** | 36 (Android 15) recommended | Verify current Google Play target requirement at build time. |

Verify current state at scaffold time at:
`https://docs.unity3d.com/6000.3/Documentation/Manual/android-supported-dependency-versions.html`

### A.3 Install procedure

1. Install **Unity Hub** from unity.com
2. In Unity Hub → Installs → Install Editor → select Unity 6.3 LTS
3. **Critical:** in the modules dialog, check:
   - Android Build Support
   - Android SDK & NDK Tools (this auto-installs the matching JDK 17, NDK r27c, and SDK Build Tools)
   - OpenJDK
4. Wait for the install to finish (15–30 minutes; the Android module is multi-GB)
5. Open the new editor once to verify it loads
6. In Edit → Preferences → External Tools → Android, verify all three paths are auto-populated and pointing inside the Unity Hub install directory

If you already have JDK / Android Studio installed from other work, leave them alone. Don't try to point Unity at them — version mismatches are the #1 cause of Android build failures in Unity. The Hub-bundled tools are isolated and predictable.

### A.4 IDE choice

**Recommendation: JetBrains Rider.** Three reasons:

1. Best-in-class Unity integration (event method recognition, ScriptableObject CreateAssetMenu navigation, asset reference checking)
2. Cross-platform consistent (matters if you switch between machines)
3. Free for non-commercial use as of 2024 — covers academic projects

**Alternatives if Rider isn't an option:**
- **Visual Studio (Windows)** — works well, slightly heavier than Rider, free Community edition
- **VS Code with C# Dev Kit + Unity extension** — workable but loses some Unity-specific tooling. Use only if Rider/VS aren't available

**Don't use:**
- Vim/Emacs without LSP — you'll fight the toolchain
- Visual Studio for Mac — discontinued, no future support

### A.5 Where Android Studio fits

**Don't install Android Studio for AnatomiQ.** Unity Hub bundles everything you need. Android Studio is only useful if:
- You're writing Android-native plugin code (not in scope for AnatomiQ)
- You need standalone APK signing tools (Unity handles this)
- You're debugging via Android Studio's profiler (Unity has its own)

If installed for other projects, that's fine — just don't point Unity at it.

### A.6 Required external tools

- **Git** (version 2.40+, with bundled Git LFS extension — see Part B)
- **Blender** 4.0+ (for the 3D model preparation pipeline)
- **A Unity-compatible USB-C cable** — yes, this matters. Cheap charge-only cables won't expose ADB. Use the cable that came with the phone or a known-good data cable.

### A.7 Device setup (Poco X5 Pro)

1. Settings → About phone → tap "MIUI version" 7 times to enable Developer Options
2. Settings → Additional settings → Developer options:
   - Enable USB debugging
   - Enable "Install via USB"
   - Disable "MIUI optimization" (allows ADB to install non-Play-Store apps)
3. Connect to dev machine via USB cable, accept the RSA fingerprint prompt on the phone
4. Verify with `adb devices` — should show the device serial. If unauthorized, re-accept the prompt.

The "MIUI optimization" toggle resets to enabled after some MIUI updates. If ADB installs start failing weeks into the project, check this first.

---

## Part B — Git LFS Strategy

### B.1 The rule

**Git LFS is configured before the first commit that contains any binary file.** Not after, not "when we get to the model assets." Once a binary file is committed without LFS, it stays in the repo's history forever — the only fixes are `git filter-repo` or BFG, both of which break clones for any collaborator and rewrite commit hashes.

### B.2 Why LFS is non-negotiable for AnatomiQ

| File type | Estimated size | Why LFS |
|---|---|---|
| Z-Anatomy `.blend` source | 200–500 MB per file | Way past GitHub's 100 MB hard limit |
| Per-layer `.glb` exports | 30–80 MB each, 6 files | Cumulative 200–500 MB |
| ONNX inference models | 7–30 MB each | Several models loaded over time |
| Texture atlases (`.png`, `.exr`) | 5–20 MB each | Re-exported frequently |
| `.psd` design source files | 50–500 MB | If any are added later |
| LightingData / OcclusionData | Often 100+ MB | Generated by Unity, must be tracked |

A single un-LFS'd `.blend` commit pushes the repo over GitHub's free-tier limits. Multiple revisions makes clone times unworkable within weeks.

### B.3 Setup sequence

This replaces the `.gitignore`-only opener in Step 2 of the execution plan:

```bash
# 1. Install Git LFS extension (one time per machine)
git lfs install

# 2. Clone empty repo or initialize
git init anatomiq
cd anatomiq

# 3. Add .gitignore (next section)
# 4. Add .gitattributes WITH LFS RULES (Part B.5)

# 5. Verify LFS will catch the right files BEFORE committing
git lfs track  # lists current rules
git check-attr filter -- some_test.blend  # should report "filter: lfs"

# 6. First commit
git add .gitignore .gitattributes
git commit -m "Initial repo with .gitignore and Git LFS rules"

# 7. Remote setup
git remote add origin <github-url>
git push -u origin main
```

**Verification step (B.5 paragraph) is critical** — it's the only way to catch a missing LFS rule before it pollutes history.

### B.4 `.gitignore` (Unity 6.3 + AnatomiQ-specific)

```gitignore
# ========== Unity ==========
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Mm]emoryCaptures/
[Uu]ser[Ss]ettings/

# Asset metadata that should not be committed
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Unity3D generated meta files
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated file on crash reports
sysinfo.txt

# Builds
*.apk
*.aab
*.unitypackage
*.app

# Crashlytics
crashlytics-build.properties

# ========== IDEs ==========
.vs/
.vscode/
.idea/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs

# ========== OS ==========
.DS_Store
Thumbs.db
desktop.ini

# ========== AnatomiQ-specific ==========
# API keys must NEVER be committed
secrets.json
api_keys.json
.env
.env.local
*.apikey

# Local-only profiling data
ProfilerCaptures/
PerformanceLogs/

# Blender autosave files
*.blend1
*.blend2
*.blend@

# Inference Engine model staging
InferenceModels/_staging/
```

The AnatomiQ-specific section is critical: **any file matching `*.apikey`, `secrets.json`, `.env*` is excluded.** Combined with the `IAIProvider` config pattern, this prevents accidental key commits.

### B.5 `.gitattributes` (Unity 6.3 + LFS)

```gitattributes
# ========== Default ==========
* text=auto eol=lf

# C# source — text with diff hint
*.cs diff=csharp text eol=lf
*.cginc text
*.shader text
*.compute text

# ========== Unity YAML smart-merge ==========
# Apply UnityYAMLMerge to scenes and prefabs only
*.unity merge=unityyamlmerge eol=lf
*.prefab merge=unityyamlmerge eol=lf

# Other Unity assets — text but no smart-merge
*.mat eol=lf
*.anim eol=lf
*.controller eol=lf
*.physicsMaterial2D eol=lf
*.physicMaterial eol=lf
*.meta eol=lf

# ========== LFS — 3D source files ==========
*.blend filter=lfs diff=lfs merge=lfs -text
*.blend1 filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.obj filter=lfs diff=lfs merge=lfs -text
*.dae filter=lfs diff=lfs merge=lfs -text
*.3ds filter=lfs diff=lfs merge=lfs -text
*.max filter=lfs diff=lfs merge=lfs -text
*.ma filter=lfs diff=lfs merge=lfs -text
*.mb filter=lfs diff=lfs merge=lfs -text

# ========== LFS — exported 3D ==========
*.glb filter=lfs diff=lfs merge=lfs -text
*.gltf filter=lfs diff=lfs merge=lfs -text

# ========== LFS — ML / Inference models ==========
*.onnx filter=lfs diff=lfs merge=lfs -text
*.tflite filter=lfs diff=lfs merge=lfs -text
*.sentis filter=lfs diff=lfs merge=lfs -text

# ========== LFS — raster images ==========
*.psd filter=lfs diff=lfs merge=lfs -text
*.tif filter=lfs diff=lfs merge=lfs -text
*.tiff filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.exr filter=lfs diff=lfs merge=lfs -text
*.hdr filter=lfs diff=lfs merge=lfs -text
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.gif filter=lfs diff=lfs merge=lfs -text
*.ai filter=lfs diff=lfs merge=lfs -text

# ========== LFS — audio ==========
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.ogg filter=lfs diff=lfs merge=lfs -text
*.aif filter=lfs diff=lfs merge=lfs -text

# ========== LFS — video ==========
*.mp4 filter=lfs diff=lfs merge=lfs -text
*.mov filter=lfs diff=lfs merge=lfs -text
*.webm filter=lfs diff=lfs merge=lfs -text

# ========== LFS — fonts ==========
*.ttf filter=lfs diff=lfs merge=lfs -text
*.otf filter=lfs diff=lfs merge=lfs -text

# ========== LFS — Unity-specific large generated files ==========
*LightingData.asset filter=lfs diff=lfs merge=lfs -text
*OcclusionCullingData.asset filter=lfs diff=lfs merge=lfs -text
*NavMesh*.asset filter=lfs diff=lfs merge=lfs -text

# ========== LFS — archives ==========
*.zip filter=lfs diff=lfs merge=lfs -text
*.7z filter=lfs diff=lfs merge=lfs -text
*.rar filter=lfs diff=lfs merge=lfs -text
*.tar.gz filter=lfs diff=lfs merge=lfs -text
```

### B.6 Important nuance on `.asset` files

Do **not** apply `merge=unityyamlmerge` blanket to `*.asset`. Some asset types (TerrainData, LightingData, OcclusionCullingData) are binary YAML and the YAML merge tool will corrupt them. The pattern above scopes the smart-merge to just `*.unity` and `*.prefab` (which always benefit from it), and uses LFS specifically for the binary `.asset` files known to be large.

The default behavior for other `*.asset` files (organ definitions, disease ScriptableObjects, etc.) is plain text — which is exactly what AnatomiQ wants for the JSON-as-ScriptableObject pattern.

### B.7 LFS quota considerations

GitHub's free LFS quota is 1 GB storage and 1 GB/month bandwidth. The free quota is enough for the academic build *if* you don't push every Blender autosave. If hit, options are:

- **Upgrade GitHub LFS data pack** ($5/month for 50 GB)
- **Self-hosted LFS server** (Gitea, custom S3) — overkill for this project
- **Use GitLab instead** (10 GB free LFS) — only if you haven't started the GitHub repo yet

Verify free quota state at start of project. If on a paid GitHub plan via institution or otherwise, this likely isn't a constraint.

### B.8 The verify-before-first-binary-commit rule

Always run before committing any new binary file type for the first time:

```bash
git check-attr filter -- path/to/file.blend
# Expected output: path/to/file.blend: filter: lfs
```

If it reports `filter: unspecified`, the LFS rule is missing. **Stop. Add the rule. Verify again.** Don't commit until LFS reports correctly.

### B.9 Recovery if LFS is missed

If a binary file *was* committed without LFS, recovery options in order of preference:

1. If it's only in your local commits (not pushed): `git reset --soft HEAD~N`, fix LFS rules, re-commit
2. If pushed but no collaborators: `git filter-repo --strip-blobs-bigger-than 50M`, force-push
3. If shared: live with it. Don't try to rewrite history that others have pulled.

The cost of recovery scales steeply, which is why prevention via the verify rule matters.

---

## Part C — Localization Approach

### C.1 The decision

**Use Unity Localization package (`com.unity.localization`)** from day one. Even though only English ships in the academic build, every UI string is created as a localized key from the first feature chat onward.

### C.2 Why this package over alternatives

| Approach | Pros | Cons | Verdict |
|---|---|---|---|
| **Hardcoded strings** | Zero setup | Refactor cost when localization is added; no "single source of truth" | ❌ |
| **`LocalizedStrings.cs` lookup** | Simple custom solution | Reinvents what Unity already provides; no editor tooling | ❌ |
| **CSV-driven custom system** | Author-friendly | Must build importer, fallback logic, runtime swap | ❌ |
| **Unity Localization package** | Official; integrates with ScriptableObjects; async-aware; supports CSV import for translators; runtime locale switching | One-time learning curve, ~30 min setup | ✅ |

The Localization package is mature in Unity 6 and integrates naturally with AnatomiQ's existing ScriptableObject-heavy architecture (organ display names, disease descriptions, narration fallbacks all become localizable assets).

### C.3 Setup

In the scaffold chat, after package installation:

1. Install via Window → Package Manager → Unity Registry → "Localization"
2. Window → Asset Management → Localization Tables → create `LocaleEnglish` and add Locale: English (en)
3. Create three String Tables matching feature areas:
   - `UIStrings` — buttons, labels, navigation
   - `OrganNames` — display names from organ data (per Phase 1 Medical Content)
   - `DiseaseContent` — disease names, descriptions, stage labels, narration fallbacks
4. Set the locale selector for runtime: `LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en")`

### C.4 Key naming convention

```
ui.<screen>.<element>.<state>     — for UI strings
  ui.atlas_home.button.layer_toggle
  ui.prism.symptom.placeholder
  ui.cadence.scenario.difficulty_easy

organ.<organId>.display_name      — for organ names
organ.<organId>.description       — for organ descriptions
  organ.pancreas_beta_cells.display_name = "Pancreatic Beta Cells"

disease.<diseaseId>.display_name  — for disease metadata
disease.<diseaseId>.description
disease.<diseaseId>.stage.<stageId>.label

cascade.<diseaseId>.<stepIndex>.fallback_narration
  cascade.t2_diabetes.0.fallback_narration = "Pancreatic beta cells become..."
```

This convention keeps the namespace flat and predictable. Translators get keys grouped by feature area; developers find strings via predictable paths.

### C.5 The cascade narration rule

`narrationFallback` in cascade JSON files **does not** go through Unity Localization initially — keep them in the JSON for v1. Reason: the medical reviewer edits them in JSON; round-tripping through Unity Localization tables adds friction and re-review risk for medically-validated content.

If localization is needed later, build a one-time importer that reads JSON narrations and creates Localization Table entries with `cascade.{diseaseId}.{stepIndex}.fallback_narration` keys. Until then, narration is English-only and lives in JSON. Document this explicitly in CORE-008.

### C.6 String externalization rule from day 1

In every feature chat, the rule is:

```csharp
// ❌ Wrong
button.text = "Show Layer";

// ❌ Wrong  
button.text = LocalizedStrings.Get("ShowLayer");

// ✅ Right (using Unity Localization)
button.text = LocalizationSettings.StringDatabase.GetLocalizedString(
    "UIStrings", "ui.atlas_home.button.show_layer");
```

For TextMeshPro components, use the `LocalizeStringEvent` component in the inspector — no code, just a key reference.

---

## Part D — Cross-Cutting Service Access Pattern

### D.1 The decision

**ScriptableObject-as-service-locator.** A single `ServiceRegistry` ScriptableObject holds references to all cross-cutting services (Fallback Manager, AI Orchestrator, Data Layer, Body Renderer). Every feature gets a serialized reference to the registry; services are accessed via `_registry.FallbackManager`.

### D.2 Why this and not the alternatives

| Pattern | Pros | Cons | Verdict |
|---|---|---|---|
| **Static singleton MonoBehaviour** (`FallbackManager.Instance`) | Familiar; easy access | Fights Unity's serialization; hard to unit test; lifecycle bugs in Edit mode | ❌ |
| **`FindObjectOfType<>()`** | Trivial | Slow; brittle when scenes change; explicitly forbidden in Project Instructions | ❌ |
| **Dependency injection (Zenject, VContainer)** | Industry-standard | Heavy framework; learning curve; overkill for solo project | ❌ |
| **Scene-injected references in inspector** | Unity-native | Tedious to wire up; breaks when prefabs span scenes | ⚠️ |
| **ScriptableObject service registry** | Unity-native; testable; works in Edit mode; no boilerplate per feature; aligns with existing ScriptableObject-everywhere decision | One-time setup; registry must be assigned in scenes | ✅ |

The ScriptableObject pattern aligns with every other architectural decision in AnatomiQ (data is ScriptableObjects, content is ScriptableObjects, prompts will be ScriptableObjects in CORE-006). One pattern, applied consistently.

### D.3 Implementation sketch

```csharp
namespace AnatomiQ.Core
{
    /// <summary>
    /// Central service registry. One asset per project, referenced by
    /// every feature that needs cross-cutting services. Eliminates
    /// FindObjectOfType, singletons, and direct scene references.
    /// </summary>
    [CreateAssetMenu(fileName = "ServiceRegistry", menuName = "AnatomiQ/Core/Service Registry")]
    public class ServiceRegistry : ScriptableObject
    {
        [SerializeField] private FallbackManager _fallbackManager;
        [SerializeField] private AIOrchestrator _aiOrchestrator;
        [SerializeField] private DataLayer _dataLayer;
        [SerializeField] private BodyModelRenderer _bodyRenderer;

        public IFallbackManager FallbackManager => _fallbackManager;
        public IAIOrchestrator AIOrchestrator => _aiOrchestrator;
        public IDataLayer DataLayer => _dataLayer;
        public IBodyModelRenderer BodyRenderer => _bodyRenderer;

        // Validation in editor — fires if a service is unassigned at runtime
        private void OnValidate()
        {
            if (_fallbackManager == null) Debug.LogWarning("[ServiceRegistry] FallbackManager unassigned");
            // ... etc for each service
        }
    }
}
```

Feature usage:

```csharp
namespace AnatomiQ.Anatomy
{
    public class CascadePlayer : MonoBehaviour
    {
        [SerializeField] private ServiceRegistry _services;

        private async Task PlayCascadeAsync(DiseaseAsset disease, CancellationToken ct)
        {
            // Read AppState
            if (_services.FallbackManager.AppState == AppState.OFFLINE_MODE)
            {
                // use cached narration
            }

            // Generate narration via the orchestrator
            var narration = await _services.AIOrchestrator.GenerateAsync(...);
        }
    }
}
```

### D.4 Why interfaces matter here

Each service is exposed as an interface (`IFallbackManager`, `IAIOrchestrator`, etc.), not the concrete class. This enables:

- Unit tests use mock implementations (`MockFallbackManager`)
- Swappable AI providers per the `IAIProvider` pattern
- Test-only service registries with substitute implementations
- No accidental coupling to MonoBehaviour-specific APIs

### D.5 Pattern enforcement

In code review (or for self-review):

- ❌ `FindObjectOfType<FallbackManager>()` → use registry
- ❌ `static FallbackManager Instance` → use registry
- ❌ `[SerializeField] FallbackManager _fb;` direct reference → use registry interface
- ✅ `[SerializeField] ServiceRegistry _services; _services.FallbackManager.X` → correct

This is the *only* approved cross-cutting access pattern in AnatomiQ. Add to bugs_and_decisions.md and the Project Instructions.

---

## Part E — Smaller Operational Items

### E.1 PRISM data handling statement

For the Settings → Privacy / data handling screen and for Q&A on demo day:

> **AnatomiQ — Privacy and Data Handling**
>
> AnatomiQ is an educational tool. We do not collect, store, or transmit personal information.
>
> **What stays on your device:** All anatomical content, organ data, disease cascades, and your selected audience level live entirely on your device. No account, sign-up, or cloud sync.
>
> **What is sent to AI providers:** When you use AI features (Q&A, symptom exploration, scan analysis, narration), the *content of your message* is sent to the AI provider for processing. By default, this is Anthropic's Claude API. The provider's data retention policy applies — Anthropic does not train its models on inputs from API users (see anthropic.com/legal/privacy).
>
> **What is NOT sent:** Your name, email, location, device identifiers, or any other identifying information. We don't have these to send.
>
> **Symptom exploration specifically:** PRISM's symptom checker is an educational dialogue. It is not a diagnostic tool. Your symptom descriptions are sent to the AI provider only for the duration of the dialogue. They are not stored after the conversation ends. If you generate a patient report, that report stays on your device until you choose to share it.
>
> **Scan analysis specifically:** Scan images uploaded to PRISM are sent to the AI provider for analysis and are not retained beyond that request. Do not upload scans containing visible patient identifiers (name, ID number) — crop these out first.
>
> **You're in control:** Use the cache reset option in Settings to clear all locally cached AI responses at any time. AR features and core anatomy exploration work fully offline.

This text goes in the Settings → Privacy / data handling screen and in the README. Update if the AI provider or retention policy changes.

### E.2 Schema v2 migration policy

Add to bugs_and_decisions.md as a settled decision:

> **Schema versioning policy:** Every ScriptableObject and JSON content file has a `schemaVersion: int` field, currently `1`. When breaking schema changes are needed (planned: branching cascades, recovery animations):
>
> 1. Increment to `schemaVersion: 2` only after a migration script exists
> 2. Migration scripts live in `Assets/_AnatomiQ/Scripts/Editor/Migrations/` and are run from a Tools menu in the Editor (`AnatomiQ → Migrations → Migrate v1 to v2`)
> 3. Migrations are version-controlled — never delete an older migration; future devs may need to migrate from v1 to v3
> 4. Migrations preserve original data: write to a `_v1_backup/` folder before transforming
> 5. Never auto-run migrations on app startup — Editor-only, deliberate

### E.3 ATLAS-006 model sourcing — flag, don't research

Add to bugs_and_decisions.md as an open task tagged for ATLAS-006 chat:

> **ATLAS-006 model research deferred:** MoveNet Thunder and BlazePose Full are recommended in the Performance & Models doc, but their ONNX file location, license, file size, and Inference Engine 2.4.x compatibility have not been verified. Do not skip this verification at the start of the ATLAS-006 chat. The state of mobile pose models changes — what's recommended in 2026 may not be optimal at build time. Plan the first hour of ATLAS-006 chat as research and download. Acceptable substitute models exist (Google's MoveNet variants, MediaPipe-derived ONNX exports); flexibility is fine.

This deliberate flag (rather than pre-researched content) protects against stale information at the time it actually matters.

---

## Part F — Updates to Existing Documents

After this document, three small updates needed:

### F.1 Project Index — Step 2 opener

Current Step 2 chat opener mentions `.gitignore`, README, LICENSE. Update to add Git LFS:

> Add to the deliverables list:
> 4. `.gitattributes` with full Git LFS rules per AnatomiQ_Build_Environment.md Part B.5
> 5. Run `git lfs install` (one-time per machine)
> 6. Verify LFS catches the right extensions with `git check-attr filter -- test.blend` before any binary commit

### F.2 Project Instructions — add Service Access Rule

Add to the Architecture rules section:

> **Cross-cutting service access pattern:** Use ServiceRegistry ScriptableObject (`AnatomiQ.Core.ServiceRegistry`). All cross-cutting services (FallbackManager, AIOrchestrator, DataLayer, BodyRenderer) are accessed via `_services.<Service>`. Never use `FindObjectOfType`, `static Instance`, or direct scene references. See AnatomiQ_Build_Environment.md Part D for rationale and pattern.

### F.3 bugs_and_decisions.md — log the four explicit decisions

```
[PLANNING PHASE — build-environment round] — Workstation setup via Unity Hub
Decision: Use Unity Hub to install JDK 17 + NDK r27c + SDK Build Tools 36.0.0 (the bundled versions for Unity 6.3 LTS).
Reason: Manual installation creates version mismatches that produce cryptic build errors. Hub-bundled tools are the only officially supported configuration.
IDE: JetBrains Rider primary; VS or VS Code + C# Dev Kit acceptable. Don't install Android Studio for AnatomiQ.

[PLANNING PHASE — build-environment round] — Git LFS configured before first binary commit
Decision: Git LFS rules in .gitattributes are committed before any .blend, .glb, .onnx, .png, etc.
Reason: Once a binary file is committed without LFS, recovery requires history rewriting that breaks collaborator clones.
Verification: `git check-attr filter -- file.ext` must report "filter: lfs" before committing any new binary type.
Spec: AnatomiQ_Build_Environment.md Part B.

[PLANNING PHASE — build-environment round] — Localization via Unity Localization package
Decision: Use com.unity.localization from day one. English only ships in academic build but every string is keyed.
Reason: Externalizing strings later is a refactor pass. Doing it from day one is one-time setup. Unity Localization integrates with the existing ScriptableObject-everywhere architecture.
Exception: Cascade narration fallbacks stay in JSON for medical-review-friendliness.
Spec: AnatomiQ_Build_Environment.md Part C.

[PLANNING PHASE — build-environment round] — Service access via ScriptableObject registry
Decision: Cross-cutting services accessed via ServiceRegistry ScriptableObject. No singletons, no FindObjectOfType.
Reason: Aligns with existing ScriptableObject-everywhere decision. Testable. Works in Edit mode. No boilerplate per feature.
Pattern: Each feature has [SerializeField] ServiceRegistry _services. Services exposed as interfaces (IFallbackManager etc.) for testability.
Spec: AnatomiQ_Build_Environment.md Part D.
```

---

## Part G — What's Now Genuinely Done

After this document, every decision needed for the first six execution-plan steps is settled:

- ✅ Workstation: Unity Hub install with bundled tooling, Rider IDE
- ✅ Git: LFS configured before any binary commit, full .gitattributes ready
- ✅ Project structure: Phase 1 organ list + cascades ready for CORE-008
- ✅ Localization: Unity Localization package, key naming convention
- ✅ Service access: ServiceRegistry ScriptableObject pattern
- ✅ Performance: budget defined per Performance & Models doc
- ✅ Tech stack: versions verified
- ✅ AI provider: Claude primary, OpenAI fallback, IAIProvider interface
- ✅ UX architecture: navigation, screens, interactions
- ✅ Data schemas: organs, diseases, with worked examples and test fixtures
- ✅ Demo strategy: 4:30 script, fallback playbook
- ✅ Privacy story: PRISM data handling statement ready for Settings + demo Q&A

---

*Build environment v1 · 2026 · the last planning artifact before execution begins*
