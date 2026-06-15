# AnatomiQ — Data Schemas & Cascade Authoring Guide

> **Purpose:** This document defines the concrete data shapes for all content in AnatomiQ — organs, diseases, cascade sequences, and procedure steps. It is the technical specification that CORE-008 (Data Layer) and CORE-005 (Interconnectivity Engine) both implement against. Every disease, organ, or procedure added to the app must conform to these schemas.
>
> **Authoring philosophy:** Content lives in JSON files. JSON is the source of truth because medical reviewers and content authors can edit it directly without Unity. At build time, JSON is imported into ScriptableObjects for runtime use. This means **adding a new disease never requires writing code**.

---

## 1. Overview of Data Layers

```
┌─────────────────────────────────────────────────────────────┐
│  Authoring Layer (JSON files in Assets/_AnatomiQ/Content/)  │
│                                                             │
│  organs/*.json        ← anatomical nodes + connections      │
│  diseases/*.json      ← disease definitions + cascades      │
│  procedures/*.json    ← CADENCE procedure step data         │
│  symptoms/*.json      ← PRISM symptom pattern definitions   │
└────────────────────────┬────────────────────────────────────┘
                         │ imported at build time
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Runtime Layer (Unity ScriptableObjects)                    │
│                                                             │
│  OrganAsset           ← runtime organ node                  │
│  DiseaseAsset         ← runtime disease + cascade           │
│  ProcedureAsset       ← runtime procedure                   │
│  SymptomPatternAsset  ← runtime symptom pattern             │
└────────────────────────┬────────────────────────────────────┘
                         │ consumed by
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Engine Layer                                               │
│                                                             │
│  CORE-005 Interconnectivity Engine                          │
│  CORE-008 Data Layer                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Organ Schema

An *organ* in AnatomiQ is any anatomically meaningful node — a whole organ (pancreas), a sub-structure (pancreatic beta cells), or a physiological compartment (systemic blood vessels). The graph of organs and their connections is the substrate the Interconnectivity Engine traverses.

### 2.1 Naming convention for organ IDs

```
lowercase_snake_case, hierarchical when relevant
✓ pancreas
✓ pancreas_beta_cells
✓ kidney_glomerulus
✓ blood_vessels_systemic
✓ nerves_peripheral_lower
✗ Pancreas (no caps)
✗ pancreas-beta-cells (no hyphens — reserved for future namespacing)
```

### 2.2 Organ JSON shape

```json
{
  "schemaVersion": 1,
  "organId": "pancreas_beta_cells",
  "displayName": "Pancreatic Beta Cells",
  "parentOrganId": "pancreas",
  "system": "endocrine",
  "meshId": "Body_Pancreas_BetaCells_LOD0",
  "description": "Insulin-producing cells located in the islets of Langerhans within the pancreas. Central to glucose homeostasis.",
  "layer": "organs",
  "anatomicalRegion": "abdomen_upper",
  "connections": [
    {
      "toOrganId": "blood_glucose_serum",
      "type": "regulates",
      "mechanism": "insulin_secretion",
      "description": "Releases insulin in response to elevated blood glucose"
    },
    {
      "toOrganId": "liver",
      "type": "signals",
      "mechanism": "insulin_signaling",
      "description": "Insulin signals liver to convert glucose to glycogen"
    }
  ],
  "metadata": {
    "sources": ["Gray's Anatomy 41st ed."],
    "lastReviewed": null
  }
}
```

### 2.3 Field definitions

| Field | Type | Required | Notes |
|---|---|---|---|
| `schemaVersion` | int | yes | Always `1` for current schema. Increment on breaking changes. |
| `organId` | string | yes | Unique. Lowercase snake_case. |
| `displayName` | string | yes | Human-readable name shown in UI. |
| `parentOrganId` | string \| null | yes | If this is a sub-structure, the ID of its parent. `null` if top-level. |
| `system` | enum | yes | One of: `skeletal`, `muscular`, `vascular`, `nervous`, `lymphatic`, `endocrine`, `digestive`, `respiratory`, `urinary`, `reproductive`, `integumentary`, `sensory`. |
| `meshId` | string | yes | Maps to a mesh component in the 3D body model. CORE-002 uses this to find the mesh. |
| `description` | string | yes | 1-3 sentence anatomical description. |
| `layer` | enum | yes | Which anatomical layer this belongs to: `skeletal`, `muscular`, `vascular`, `nervous`, `lymphatic`, `organs`. Drives layer toggle (CORE-003). |
| `anatomicalRegion` | enum | yes | Coarse body region for spatial queries. Values listed below. |
| `connections` | array | yes | List of outgoing edges to other organs (the graph structure). |
| `metadata` | object | yes | Provenance and review tracking. |

### 2.4 Connection types

| Type | Meaning | Example |
|---|---|---|
| `regulates` | Controls or modulates | Pancreas → blood glucose |
| `signals` | Sends biochemical signal | Hypothalamus → pituitary |
| `supplies_blood` | Blood flow source | Aorta → kidneys |
| `drains_blood` | Blood flow destination | Kidneys → renal vein |
| `innervates` | Nerve supply | Vagus nerve → heart |
| `mechanically_supports` | Physical support | Spine → spinal cord |
| `produces` | Generates substance | Liver → bile |
| `metabolizes` | Processes substance | Liver → drugs |
| `filters` | Filters substance | Kidneys → blood |
| `contains` | Spatial containment | Skull → brain |

### 2.5 Anatomical regions

`head`, `neck`, `chest_anterior`, `chest_posterior`, `abdomen_upper`, `abdomen_lower`, `pelvis`, `arm_left`, `arm_right`, `leg_left`, `leg_right`, `systemic` (everywhere — use sparingly).

### 2.6 C# runtime equivalent

```csharp
namespace AnatomiQ.Data
{
    [CreateAssetMenu(fileName = "Organ", menuName = "AnatomiQ/Organ")]
    public class OrganAsset : ScriptableObject
    {
        public int SchemaVersion = 1;
        public string OrganId;
        public string DisplayName;
        public string ParentOrganId;
        public BodySystem System;
        public string MeshId;
        [TextArea(2, 4)] public string Description;
        public AnatomyLayer Layer;
        public AnatomicalRegion Region;
        public List<OrganConnection> Connections;
        public OrganMetadata Metadata;
    }

    [Serializable]
    public class OrganConnection
    {
        public string ToOrganId;
        public ConnectionType Type;
        public string Mechanism;
        [TextArea(1, 3)] public string Description;
    }

    public enum ConnectionType
    {
        Regulates, Signals, SuppliesBlood, DrainsBlood,
        Innervates, MechanicallySupports, Produces,
        Metabolizes, Filters, Contains
    }
}
```

---

## 3. Disease Schema

A *disease* is the entry point into a cascade. It defines what condition is being simulated, where the cascade starts, and the ordered sequence of physiological effects across the body.

### 3.1 Disease JSON shape (top level)

```json
{
  "schemaVersion": 1,
  "diseaseId": "t2_diabetes",
  "displayName": "Type 2 Diabetes Mellitus",
  "shortLabel": "Type 2 Diabetes",
  "category": "metabolic",
  "icd10": "E11",
  "description": "A chronic metabolic disorder characterized by insulin resistance and progressive beta cell dysfunction, leading to systemic hyperglycemia and multi-organ complications.",
  "entryOrganId": "pancreas_beta_cells",
  "stages": [...],
  "cascade": [...],
  "metadata": {...}
}
```

### 3.2 Stages

Stages partition the cascade into time-based phases. They drive ATLAS-004 (Time Progression Slider).

```json
"stages": [
  {
    "stageId": "early",
    "label": "Early (0–5 years)",
    "description": "Insulin resistance develops; pancreas compensates with increased insulin output.",
    "stepIndices": [0, 1, 2, 3]
  },
  {
    "stageId": "intermediate",
    "label": "Intermediate (5–15 years)",
    "description": "Beta cell exhaustion begins; sustained hyperglycemia damages microvasculature.",
    "stepIndices": [4, 5, 6, 7]
  },
  {
    "stageId": "advanced",
    "label": "Advanced (15+ years)",
    "description": "Macrovascular complications; established organ damage in kidneys, eyes, nerves.",
    "stepIndices": [8, 9, 10]
  }
]
```

### 3.3 Cascade steps

Each step is one organ activation event in the animated propagation.

```json
{
  "stepIndex": 0,
  "stage": "early",
  "targetOrganId": "pancreas_beta_cells",
  "mechanismKey": "beta_cell_hyperinsulinemia",
  "severity": 30,
  "delayMs": 0,
  "durationMs": 3000,
  "visualEffect": {
    "type": "highlight_pulse",
    "color": "#FFB800",
    "intensity": 0.6
  },
  "narrationFallback": "Pancreas beta cells become hypersensitive to glucose and begin secreting excess insulin in an attempt to lower blood sugar.",
  "aiContext": {
    "physiology": "hyperinsulinemia compensatory phase",
    "clinicalNote": "Often asymptomatic; detectable only via fasting insulin tests.",
    "audience": "general"
  }
}
```

### 3.4 Cascade step field definitions

| Field | Type | Notes |
|---|---|---|
| `stepIndex` | int | Zero-based ordering within the cascade. |
| `stage` | string | Which stage this step belongs to (matches a `stageId`). |
| `targetOrganId` | string | The organ being activated. Must exist in organ data. |
| `mechanismKey` | string | A canonical key naming the physiological mechanism. Used for caching AI narrations and for fallback lookup. |
| `severity` | int (0–100) | Drives visual intensity. 30 = mild, 60 = moderate, 90 = severe. |
| `delayMs` | int | When this step starts, measured from cascade start. Allows simultaneous activations (multiple steps with same `delayMs`). |
| `durationMs` | int | How long the activation animation plays. Typically 2000–4000 ms. |
| `visualEffect` | object | What happens visually on the 3D model. |
| `narrationFallback` | string | Pre-baked, medically-reviewed narration text. Used when AI is offline. **Must be a complete, accurate sentence on its own.** |
| `aiContext` | object | Hints injected into the AI narration prompt when generating live narration. |

### 3.5 Visual effect types

```json
{ "type": "highlight_pulse",   "color": "#FFB800", "intensity": 0.6 }
{ "type": "color_change",      "fromColor": "#E8B5A8", "toColor": "#A85C45", "fadeMs": 2000 }
{ "type": "particle_emit",     "particleType": "inflammation", "rate": 30 }
{ "type": "connection_line",   "fromOrganId": "pancreas", "toOrganId": "kidney_left", "color": "#FF4444" }
{ "type": "shrink",            "scale": 0.85, "durationMs": 2500 }
{ "type": "thicken",           "thicknessFactor": 1.4, "durationMs": 3000 }
```

CORE-002 (3D Body Renderer) implements one handler per type. New visual effect types can be added by extending the renderer; the schema stays flexible.

### 3.6 Severity levels (consistent across diseases)

| Severity | Visual cue | Use for |
|---|---|---|
| 0–29 | Soft glow, subtle color shift | Early/compensatory changes |
| 30–59 | Pulsing glow, moderate color change | Established dysfunction |
| 60–89 | Strong color change, particle effects | Significant damage |
| 90–100 | Dramatic visual, possible shrink/thicken | Organ failure, end-stage |

---

## 4. Worked Example — Type 2 Diabetes Mellitus

This is a complete, realistic disease cascade for the academic demo. Cascade sequence based on established T2DM pathophysiology (StatPearls 2023; Swedish National Diabetes Register cohort studies). **Final medical review required before demo.**

```json
{
  "schemaVersion": 1,
  "diseaseId": "t2_diabetes",
  "displayName": "Type 2 Diabetes Mellitus",
  "shortLabel": "Type 2 Diabetes",
  "category": "metabolic",
  "icd10": "E11",
  "description": "A chronic metabolic disorder characterized by insulin resistance and progressive pancreatic beta cell dysfunction, leading to sustained hyperglycemia and progressive multi-organ complications affecting the cardiovascular, renal, ocular, and nervous systems.",
  "entryOrganId": "pancreas_beta_cells",

  "stages": [
    {
      "stageId": "early",
      "label": "Early (0–5 years)",
      "description": "Insulin resistance develops; pancreas compensates with hyperinsulinemia; blood glucose begins to rise.",
      "stepIndices": [0, 1, 2, 3]
    },
    {
      "stageId": "intermediate",
      "label": "Intermediate (5–15 years)",
      "description": "Beta cell function declines; sustained hyperglycemia damages microvasculature; early end-organ changes.",
      "stepIndices": [4, 5, 6, 7]
    },
    {
      "stageId": "advanced",
      "label": "Advanced (15+ years)",
      "description": "Established microvascular and macrovascular complications across multiple organ systems.",
      "stepIndices": [8, 9, 10]
    }
  ],

  "cascade": [
    {
      "stepIndex": 0,
      "stage": "early",
      "targetOrganId": "pancreas_beta_cells",
      "mechanismKey": "beta_cell_hyperinsulinemia",
      "severity": 25,
      "delayMs": 0,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FFB800",
        "intensity": 0.5
      },
      "narrationFallback": "Pancreatic beta cells become hypersensitive to glucose and begin secreting excess insulin to compensate for rising tissue insulin resistance.",
      "aiContext": {
        "physiology": "compensatory hyperinsulinemia",
        "clinicalNote": "Asymptomatic phase, often missed without targeted screening.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 1,
      "stage": "early",
      "targetOrganId": "tissues_peripheral",
      "mechanismKey": "insulin_resistance",
      "severity": 35,
      "delayMs": 2500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#E8B5A8",
        "toColor": "#D49888",
        "fadeMs": 2500
      },
      "narrationFallback": "Muscle and fat tissues progressively lose sensitivity to insulin, requiring ever-higher insulin levels to maintain normal glucose uptake.",
      "aiContext": {
        "physiology": "peripheral insulin resistance",
        "clinicalNote": "Linked to obesity, sedentary lifestyle, and genetic predisposition.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 2,
      "stage": "early",
      "targetOrganId": "blood_glucose_serum",
      "mechanismKey": "hyperglycemia_onset",
      "severity": 45,
      "delayMs": 5000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#C44545",
        "toColor": "#E8A040",
        "fadeMs": 2500
      },
      "narrationFallback": "Despite elevated insulin, blood glucose rises above the normal range as compensation begins to fail. This marks the clinical onset of diabetes.",
      "aiContext": {
        "physiology": "sustained hyperglycemia",
        "clinicalNote": "HbA1c crosses 6.5% threshold for diagnosis.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 3,
      "stage": "early",
      "targetOrganId": "kidney_glomerulus",
      "mechanismKey": "glomerular_hyperfiltration",
      "severity": 30,
      "delayMs": 8000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FFA040",
        "intensity": 0.5
      },
      "narrationFallback": "The kidneys initially respond to high blood glucose by increasing filtration rate, a phase known as glomerular hyperfiltration.",
      "aiContext": {
        "physiology": "early diabetic nephropathy hyperfiltration phase",
        "clinicalNote": "Reversible if glycemic control achieved.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 4,
      "stage": "intermediate",
      "targetOrganId": "pancreas_beta_cells",
      "mechanismKey": "beta_cell_exhaustion",
      "severity": 65,
      "delayMs": 11500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "shrink",
        "scale": 0.88,
        "durationMs": 2500
      },
      "narrationFallback": "Years of overwork drive beta cell exhaustion. Insulin output progressively declines, accelerating the rise in blood glucose.",
      "aiContext": {
        "physiology": "beta cell apoptosis and dysfunction",
        "clinicalNote": "Many patients now require oral medications or insulin.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 5,
      "stage": "intermediate",
      "targetOrganId": "blood_vessels_systemic",
      "mechanismKey": "microvascular_basement_thickening",
      "severity": 55,
      "delayMs": 14500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.3,
        "durationMs": 3000
      },
      "narrationFallback": "Sustained high glucose causes thickening of small blood vessel walls throughout the body, reducing oxygen delivery to tissues.",
      "aiContext": {
        "physiology": "advanced glycation end products and microvascular damage",
        "clinicalNote": "The mechanism behind the major complications that follow.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 6,
      "stage": "intermediate",
      "targetOrganId": "eye_retina",
      "mechanismKey": "diabetic_retinopathy_early",
      "severity": 50,
      "delayMs": 17500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#D44545",
        "intensity": 0.7
      },
      "narrationFallback": "Damaged retinal capillaries begin to leak and form microaneurysms, the earliest visible sign of diabetic eye disease.",
      "aiContext": {
        "physiology": "non-proliferative diabetic retinopathy",
        "clinicalNote": "Often asymptomatic until significant; annual eye exams critical.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 7,
      "stage": "intermediate",
      "targetOrganId": "nerves_peripheral_lower",
      "mechanismKey": "diabetic_neuropathy_early",
      "severity": 45,
      "delayMs": 20500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#FFE8C8",
        "toColor": "#D49888",
        "fadeMs": 2500
      },
      "narrationFallback": "Peripheral nerves, especially in the feet and legs, lose function due to vascular damage. Patients may notice tingling or reduced sensation.",
      "aiContext": {
        "physiology": "distal symmetric polyneuropathy",
        "clinicalNote": "Increases risk of unnoticed foot injuries and ulcers.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 8,
      "stage": "advanced",
      "targetOrganId": "kidney_left",
      "mechanismKey": "chronic_kidney_disease",
      "severity": 80,
      "delayMs": 23500,
      "durationMs": 3500,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#A85C45",
        "toColor": "#6B3825",
        "fadeMs": 3000
      },
      "narrationFallback": "Years of glomerular damage progress to chronic kidney disease. Kidney function declines, protein leaks into the urine, and waste products accumulate.",
      "aiContext": {
        "physiology": "diabetic nephropathy with declining GFR",
        "clinicalNote": "Diabetes is the leading cause of end-stage renal disease worldwide.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 9,
      "stage": "advanced",
      "targetOrganId": "blood_vessels_coronary",
      "mechanismKey": "accelerated_atherosclerosis",
      "severity": 85,
      "delayMs": 27000,
      "durationMs": 3500,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.6,
        "durationMs": 3500
      },
      "narrationFallback": "Diabetes accelerates atherosclerosis throughout the body. Coronary arteries narrow, dramatically increasing the risk of heart attack.",
      "aiContext": {
        "physiology": "diabetic macrovascular disease",
        "clinicalNote": "Cardiovascular disease causes the majority of deaths in T2DM.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 10,
      "stage": "advanced",
      "targetOrganId": "eye_retina",
      "mechanismKey": "proliferative_retinopathy",
      "severity": 90,
      "delayMs": 30500,
      "durationMs": 3500,
      "visualEffect": {
        "type": "particle_emit",
        "particleType": "inflammation",
        "rate": 40
      },
      "narrationFallback": "Without intervention, fragile new blood vessels grow on the retina and rupture. This is proliferative retinopathy and a leading cause of preventable blindness.",
      "aiContext": {
        "physiology": "proliferative diabetic retinopathy",
        "clinicalNote": "Treatable with laser photocoagulation if caught early.",
        "audience": "general"
      }
    }
  ],

  "metadata": {
    "sources": [
      "StatPearls — Type 2 Diabetes (NCBI Bookshelf, 2023)",
      "Swedish National Diabetes Register cohort study (Lancet Diabetes Endocrinol)",
      "ADA Standards of Care 2024"
    ],
    "medicalReviewedBy": null,
    "medicalReviewedDate": null,
    "version": "0.9-pre-review",
    "totalDurationMs": 34000,
    "stepCount": 11
  }
}
```

**Cascade timing summary:** total runtime ~34 seconds for the full cascade end-to-end. Each step has a 2.5-second delay between starts (so 0.5s of overlap with the previous step's tail), giving the visual a continuous propagation feel rather than discrete jumps.

---

## 5. C# Runtime Definitions

```csharp
namespace AnatomiQ.Data
{
    [CreateAssetMenu(fileName = "Disease", menuName = "AnatomiQ/Disease")]
    public class DiseaseAsset : ScriptableObject
    {
        public int SchemaVersion = 1;
        public string DiseaseId;
        public string DisplayName;
        public string ShortLabel;
        public DiseaseCategory Category;
        public string Icd10Code;
        [TextArea(3, 6)] public string Description;
        public string EntryOrganId;
        public List<DiseaseStage> Stages;
        public List<CascadeStep> Cascade;
        public DiseaseMetadata Metadata;
    }

    [Serializable]
    public class DiseaseStage
    {
        public string StageId;
        public string Label;
        [TextArea(2, 4)] public string Description;
        public List<int> StepIndices;
    }

    [Serializable]
    public class CascadeStep
    {
        public int StepIndex;
        public string Stage;
        public string TargetOrganId;
        public string MechanismKey;
        [Range(0, 100)] public int Severity;
        public int DelayMs;
        public int DurationMs;
        public VisualEffect Effect;
        [TextArea(2, 5)] public string NarrationFallback;
        public AiContext AiContext;
    }

    [Serializable]
    public class VisualEffect
    {
        public VisualEffectType Type;
        public string Color;          // hex, used by highlight_pulse, connection_line
        public string FromColor;      // used by color_change
        public string ToColor;        // used by color_change
        public float Intensity;       // 0–1, used by highlight_pulse
        public float FadeMs;          // used by color_change
        public string ParticleType;   // used by particle_emit
        public int Rate;              // used by particle_emit
        public float Scale;           // used by shrink
        public float ThicknessFactor; // used by thicken
        public string FromOrganId;    // used by connection_line
        public string ToOrganId;      // used by connection_line
        public float DurationMs;      // animation duration (overrides if specified)
    }

    public enum VisualEffectType
    {
        HighlightPulse, ColorChange, ParticleEmit,
        ConnectionLine, Shrink, Thicken
    }

    public enum DiseaseCategory
    {
        Metabolic, Cardiovascular, Respiratory, Neurological,
        Renal, Endocrine, Autoimmune, Infectious, Oncological
    }

    [Serializable]
    public class AiContext
    {
        public string Physiology;
        public string ClinicalNote;
        public AudienceLevel Audience;
    }

    public enum AudienceLevel { General, Patient, Student, Clinician }

    [Serializable]
    public class DiseaseMetadata
    {
        public List<string> Sources;
        public string MedicalReviewedBy;
        public string MedicalReviewedDate;
        public string Version;
        public int TotalDurationMs;
        public int StepCount;
    }
}
```

---

## 6. Cascade Playback Algorithm (CORE-005 + CORE-002)

```csharp
namespace AnatomiQ.Anatomy
{
    public class CascadePlayer : MonoBehaviour
    {
        public async Task PlayCascadeAsync(
            DiseaseAsset disease,
            CancellationToken ct = default)
        {
            var startTime = Time.time;
            var narrationTasks = new List<Task>();

            foreach (var step in disease.Cascade)
            {
                // Wait until this step's start time
                var stepStartTime = startTime + (step.DelayMs / 1000f);
                while (Time.time < stepStartTime)
                {
                    if (ct.IsCancellationRequested) return;
                    await Task.Yield();
                }

                // Trigger visual effect (synchronous, returns immediately)
                _bodyRenderer.PlayEffect(step.TargetOrganId, step.Effect);

                // Fetch and play narration in parallel (does not block next step)
                narrationTasks.Add(NarrateStepAsync(step, ct));
            }

            // Wait for all narrations to finish before considering cascade complete
            await Task.WhenAll(narrationTasks);
        }

        private async Task NarrateStepAsync(CascadeStep step, CancellationToken ct)
        {
            // Try AI narration first; on failure or timeout, use fallback
            var narration = await _aiOrchestrator.GenerateCascadeNarrationAsync(
                step, ct) ?? step.NarrationFallback;

            await _narrationUI.SpeakAsync(narration, ct);
        }
    }
}
```

Key behaviors:
- **Visual effects play on schedule regardless of AI state** — the cascade animation never waits for the network.
- **AI narration is fetched in parallel** — if the API is slow, multiple narrations queue up; if the API fails, the fallback narration plays instead.
- **Cascade is cancellable** — CORE-007 (Fallback Manager) can cancel a running cascade if conditions degrade.

---

## 7. Updated API Timeout Policy (Replaces v1.1 Section)

The v1.1 docs specified an 8-second API timeout. This is too long for mobile — users perceive 3+ seconds as broken. Updated policy:

| Phase | Limit | Behavior on hit |
|---|---|---|
| **Initial response** | 4 seconds | If no response, surface "thinking..." indicator. |
| **Streamed token start** | 5 seconds (from request) | If no first token by then, abort and use cached/fallback response. |
| **Total response** | 12 seconds (hard ceiling) | Always abort. Use cached/fallback. |
| **Cascade narration specifically** | 3 seconds | Cascades feel laggy beyond this. After 3s, use the step's `narrationFallback` instead. |
| **Q&A responses** | 5s soft / 12s hard | Q&A can tolerate slightly longer waits. |
| **Scan analysis (PRISM-004)** | 30s | Vision API is slower; user expects to wait. Show progress indicator. |

**Streaming where supported:** When using Claude API or OpenAI with streaming enabled, the user sees text appearing token-by-token after ~1 second. This dramatically improves perceived performance even if total response takes 5+ seconds. **Always prefer streaming for narration and Q&A; non-streaming only for short structured responses.**

```csharp
// Updated async API call pattern
private async Task<string> CallAIAsync(
    string prompt,
    AICallType type,
    CancellationToken ct = default)
{
    var (softTimeout, hardTimeout) = type switch
    {
        AICallType.CascadeNarration => (TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)),
        AICallType.QandA            => (TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(12)),
        AICallType.ScanAnalysis     => (TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30)),
        _                           => (TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(12))
    };

    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(hardTimeout);

        var response = await _httpClient.PostAsync(API_URL, content, cts.Token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    catch (OperationCanceledException)
    {
        return GetCachedResponse(prompt) ?? GetFallbackForCallType(type);
    }
    catch (Exception ex)
    {
        Debug.LogError($"[AIOrchestrator] {type} call failed: {ex.Message}");
        return GetCachedResponse(prompt) ?? GetFallbackForCallType(type);
    }
}
```

---

## 8. Authoring Workflow

When adding a new disease:

1. **Draft the cascade.** Identify 8–12 cascade steps from established medical sources. Cite them in metadata.
2. **Map each step to organ IDs.** Each `targetOrganId` must already exist in organ data — add new organs first if needed.
3. **Write narration fallbacks.** Each `narrationFallback` must be medically accurate, complete on its own, and audience-appropriate.
4. **Set timing.** Default to 3000ms per step with 500ms overlap (so `delayMs` increments by 2500). Adjust based on visual complexity.
5. **Set severity.** Use the severity scale consistently across diseases.
6. **Save to `Assets/_AnatomiQ/Content/diseases/<diseaseId>.json`.**
7. **Run the importer.** A build-time script converts JSON → ScriptableObject and validates references.
8. **Send for medical review.** Reviewer edits the JSON directly. Once approved, set `medicalReviewedBy` and `medicalReviewedDate`.

---

## 9. Validation Rules (enforced by importer)

The JSON-to-ScriptableObject importer must reject any file that fails these checks:

- `schemaVersion` matches current
- `diseaseId` is unique across all diseases
- `entryOrganId` exists in organ data
- Every `targetOrganId` in cascade steps exists in organ data
- Every `stage` value in steps matches a defined `stageId`
- Every step index appears in exactly one stage's `stepIndices`
- `severity` in 0–100, `delayMs` ≥ 0, `durationMs` > 0
- `narrationFallback` is non-empty (the dual-track rule)
- For `connection_line` effects: both `fromOrganId` and `toOrganId` exist
- For `color_change` effects: both `fromColor` and `toColor` are valid hex

A validation failure logs the specific issue and skips the asset rather than crashing the build.

---

## 10. Open Schema Questions (resolve before Phase 2)

| Question | Why it matters | Options |
|---|---|---|
| Should cascades support branching (different paths based on patient profile)? | Realistic disease trajectories diverge by patient | Add `branches` array per step, or keep linear for v1 |
| Should we support cascade reversal (disease improvement)? | ATLAS-004 slider could go backward to show recovery | Add `reverseNarration` field, or compute reverse playback algorithmically |
| How is procedure data structured? | Needed for CADENCE-001 | Defer until ATLAS pillar is stable; design then |
| Symptom pattern schema? | Needed for PRISM-001 | Defer to PRISM phase |

For the academic build, **linear cascades only** — keep v1 simple. Branching can be added in a future schema version.

---

*Schema version 1 · 2026 · Pair this document with AnatomiQ_Project_Instructions.md and AnatomiQ_Features_Document.docx*
