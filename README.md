# AnatomiQ

**See the Body. Understand Everything.**

AnatomiQ is an AI-powered Augmented Reality mobile app that makes the human body fully interactive and explorable. Point your phone at a flat surface, place a life-scale 3D human body, peel through anatomical layers, select any organ, and watch disease effects propagate across the whole body as an animated cascade — narrated and explained by an AI assistant.

This is an academic personal project (with potential future startup direction), built in Unity for Android.

> **Status:** Pre-scaffold. This repository currently contains version-control scaffolding and planning documents only. No Unity project has been created yet.

---

## What makes it different

AnatomiQ is organized around three pillars:

- **ATLAS** — Anatomy & Education Engine: interactive 3D body, layer system, disease cascade simulation, AI Q&A.
- **PRISM** — Clinical AI Assistant: symptom exploration, scan analysis, and a doctor↔patient explanation mode.
- **CADENCE** — Surgical Training: AR-guided procedure walkthroughs with real-time movement evaluation.

The signature feature is the **Body Interconnectivity Engine** — a medical knowledge graph that propagates disease effects across the entire body as an animated cascade, rather than treating organs in isolation.

AR and AI are treated as *enhancements, not requirements*: every failure path degrades to a still-functional 3D viewer rather than a dead end.

---

## Tech stack

| Area | Choice |
|---|---|
| Engine | Unity 6.3 LTS (6000.3.x) |
| Language | C# |
| Render pipeline | Universal Render Pipeline (URP), Forward+ |
| AR framework | AR Foundation 6.3.x + Google ARCore XR Plugin 6.3.x (ARCore *Required*) |
| On-device AI | Unity Inference Engine 2.4.x+ (`Unity.InferenceEngine`, formerly Sentis) |
| Cloud AI | External LLM API, async only (Claude primary, OpenAI fallback via `IAIProvider`) |
| Input | Unity Input System (new) |
| JSON | Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) |
| Localization | Unity Localization (`com.unity.localization`) |
| Target device | Poco X5 Pro 5G — Snapdragon 778G, 8GB RAM, ARCore-supported |

**Android build:** IL2CPP, ARM64 only, min API 29 (Android 10), Vulkan first / OpenGLES3 fallback.

> Package and API versions above reflect the project's early-2026 baseline. The Unity ecosystem moves fast — verify current versions at the start of any scaffold or build session.

---

## Repository layout

```
anatomiq/
├── .gitignore          Unity 6.3 + AnatomiQ-specific ignores (secrets, profiling, autosaves)
├── .gitattributes      Git LFS rules + Unity YAML smart-merge for scenes/prefabs
├── LICENSE             CC BY-SA 4.0 + upstream 3D-data attribution
├── README.md           This file
└── docs/               The 12 planning documents (see docs/README.md)
```

The Unity project (`Assets/_AnatomiQ/...`) is created in a later scaffold step and is **not** part of this initial commit.

---

## Getting started

This repo uses **Git LFS** for all binary assets (`.blend`, `.glb`, `.onnx`, `.png`, fonts, audio, video, and large generated Unity assets). You must have Git LFS installed before cloning, or large files will come down as text pointers.

```bash
# One time per machine
git lfs install

# Clone (LFS files are pulled automatically once LFS is installed)
git clone <github-url> anatomiq
cd anatomiq

# If you cloned before installing LFS, hydrate the binaries now:
git lfs pull
```

**Open in Unity:**

1. Install **Unity Hub**, then install **Unity 6.3 LTS (6000.3.x)** with the **Android Build Support** module (this also installs the matching JDK 17, NDK r27c, and SDK Build Tools — see `docs/AnatomiQ_Build_Environment.md` Part A).
2. In Unity Hub → **Add** → select the cloned `anatomiq` folder.
3. Open with Unity 6.3 LTS. (Note: until the scaffold step runs, there is no Unity project to open — this section describes the workflow once scaffolding exists.)
4. Recommended IDE: **JetBrains Rider** (free for non-commercial use).

**Device setup (Poco X5 Pro):** enable Developer Options + USB debugging, and verify with `adb devices`. Full steps in `docs/AnatomiQ_Build_Environment.md` Part A.7.

---

## Privacy and data handling

AnatomiQ is an educational tool. It does not collect, store, or transmit personal information. All anatomical content lives on-device with no account or cloud sync. When AI features are used, only the *content of the message* (a question, symptom description, or scan image) is sent to the AI provider for processing — never your name, location, or device identifiers. Core anatomy exploration works fully offline. Full statement: `docs/AnatomiQ_Build_Environment.md` Part E.1.

PRISM is a symptom **exploration** tool, not a diagnostic tool. It never makes diagnoses, never recommends treatments, and always directs emergency symptoms to emergency services.

---

## Attribution

The 3D anatomy data in this project is adapted from open, university-backed anatomical models:

```
3D anatomy data based on:
  - BodyParts3D, (c)2008 Life Science Integrated Database Center
  - Z-Anatomy by Lluis Vinent, 2021
  - Open3DModel by Leiden UMC, Utrecht UMC, Maastricht UM, KU Leuven,
    Amsterdam UMC, Radboud UMC, and Ghent University, 2022-

Licensed under Creative Commons Attribution-ShareAlike 2.1 Japan.
```

This attribution must also appear in a visible in-app credits screen. See `LICENSE` for full details.

---

## License

This repository is licensed under **Creative Commons Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)**. The ShareAlike obligation is inherited from the BodyParts3D / Z-Anatomy source data (CC BY-SA 2.1 JP); CC BY-SA 4.0 is a permitted upgrade target for derivatives of that work. See [`LICENSE`](./LICENSE) for the full text and a note on the code-vs-assets licensing nuance.
