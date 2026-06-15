# AnatomiQ — Phase 1 Medical Content

> **Purpose:** All medical content needed for Phase 1 + Phase 2 development, ready to send to a medical reviewer in a single batch. Contains: the canonical Phase 1 organ list, the Hypertension cascade JSON, the Chronic Kidney Disease cascade JSON, and minimal test fixtures for unit tests.
>
> **Companion to:** `AnatomiQ_Data_Schemas.md` (defines schemas) and the existing T2D cascade in that document. This document fills out the academic-build minimum of three diseases.
>
> **Review status:** All content marked `0.9-pre-review`. **Final version requires medical professional sign-off.**
>
> **Authoring sources cited per cascade.** Cascade lengths and timings calibrated to match the T2D cascade for consistency in user experience.

---

## Part A — Schema v1.1 Note

Two small schema additions needed for this content. To be applied in the scaffold chat:

### A.1 New node type: `physiological_state`

Some cascade nodes are not anatomical structures — they're measurable physiological states (blood glucose level, blood pressure, blood oxygen saturation). The data layer needs a way to represent these without forcing them into the organ schema. New optional field on OrganAsset:

```json
{
  "schemaVersion": 1,
  "organId": "blood_glucose_serum",
  "displayName": "Blood Glucose Level",
  "nodeType": "physiological_state",
  ...
}
```

When `nodeType` is `physiological_state`:
- `meshId` becomes optional (no 3D mesh, may be visualized as a label or icon)
- `parentOrganId` becomes optional
- `system` should describe the system the state belongs to (e.g. `metabolic` for glucose, `cardiovascular` for blood pressure)
- `anatomicalRegion` is `systemic`

Default `nodeType` is `anatomical` if omitted (preserves backward compatibility with all existing organ entries).

### A.2 FMA ID field (carried over from Operations doc)

Optional `fmaId` field on every organ node, populated where Z-Anatomy provides one. Anatomical organs have FMA IDs; physiological states do not.

### A.3 Update OrganAsset C# class

```csharp
public class OrganAsset : ScriptableObject
{
    public int SchemaVersion = 1;
    public string OrganId;
    public string DisplayName;
    public NodeType NodeType = NodeType.Anatomical;  // NEW
    public string FmaId;                              // NEW (optional)
    public string ParentOrganId;
    public BodySystem System;
    public string MeshId;
    [TextArea(2, 4)] public string Description;
    public AnatomyLayer Layer;
    public AnatomicalRegion Region;
    public List<OrganConnection> Connections;
    public OrganMetadata Metadata;
}

public enum NodeType { Anatomical, PhysiologicalState }
```

---

## Part B — Phase 1 Canonical Organ List

The following 25 organ nodes cover the three Phase 1 cascades (T2 Diabetes, Hypertension, Chronic Kidney Disease) with no missing references. Connection lists below describe the *static graph* — the disease-specific propagation paths come from the cascade JSON files, but the underlying connectivity must be defined here first.

**Authoring note:** This list is intentionally minimal. It is *not* a complete anatomy ontology. Adding new diseases later will add new nodes; adding new nodes is fine.

### B.1 Endocrine system

#### `pancreas_beta_cells`
```json
{
  "schemaVersion": 1,
  "organId": "pancreas_beta_cells",
  "displayName": "Pancreatic Beta Cells",
  "nodeType": "anatomical",
  "fmaId": "FMA62630",
  "parentOrganId": "pancreas",
  "system": "endocrine",
  "meshId": "Body_Pancreas_BetaCells_LOD0",
  "description": "Insulin-producing cells located in the islets of Langerhans within the pancreas. Central to glucose homeostasis.",
  "layer": "organs",
  "anatomicalRegion": "abdomen_upper",
  "connections": [
    { "toOrganId": "blood_glucose_serum", "type": "regulates", "mechanism": "insulin_secretion",
      "description": "Releases insulin in response to elevated blood glucose" },
    { "toOrganId": "liver", "type": "signals", "mechanism": "insulin_signaling",
      "description": "Insulin signals liver to convert glucose to glycogen" },
    { "toOrganId": "tissues_peripheral", "type": "signals", "mechanism": "insulin_signaling",
      "description": "Insulin enables glucose uptake by muscle and fat tissue" }
  ],
  "metadata": { "sources": ["Gray's Anatomy 41st ed.", "FMA Ontology"], "lastReviewed": null }
}
```

#### `pancreas`
```json
{
  "organId": "pancreas",
  "displayName": "Pancreas",
  "nodeType": "anatomical",
  "fmaId": "FMA7198",
  "parentOrganId": null,
  "system": "endocrine",
  "meshId": "Body_Pancreas_LOD0",
  "description": "Mixed exocrine-endocrine organ in the upper abdomen. Endocrine portion produces insulin and glucagon for glucose regulation.",
  "layer": "organs",
  "anatomicalRegion": "abdomen_upper",
  "connections": [
    { "toOrganId": "liver", "type": "produces", "mechanism": "digestive_enzymes",
      "description": "Produces digestive enzymes secreted into the duodenum" }
  ]
}
```

#### `parathyroid_glands`
```json
{
  "organId": "parathyroid_glands",
  "displayName": "Parathyroid Glands",
  "nodeType": "anatomical",
  "fmaId": "FMA13885",
  "parentOrganId": null,
  "system": "endocrine",
  "meshId": "Body_Parathyroid_LOD0",
  "description": "Four small glands behind the thyroid that secrete parathyroid hormone (PTH), regulating calcium and phosphate homeostasis.",
  "layer": "organs",
  "anatomicalRegion": "neck",
  "connections": [
    { "toOrganId": "bone_skeletal_general", "type": "regulates", "mechanism": "pth_calcium_mobilization",
      "description": "PTH mobilizes calcium from bone" },
    { "toOrganId": "kidney_left", "type": "regulates", "mechanism": "pth_renal_action",
      "description": "PTH increases renal calcium reabsorption and phosphate excretion" }
  ]
}
```

### B.2 Cardiovascular system

#### `heart_left_ventricle`
```json
{
  "organId": "heart_left_ventricle",
  "displayName": "Left Ventricle of Heart",
  "nodeType": "anatomical",
  "fmaId": "FMA7101",
  "parentOrganId": "heart",
  "system": "cardiovascular",
  "meshId": "Body_Heart_LeftVentricle_LOD0",
  "description": "The thickest-walled chamber of the heart, responsible for pumping oxygenated blood into systemic circulation against arterial pressure.",
  "layer": "organs",
  "anatomicalRegion": "chest_anterior",
  "connections": [
    { "toOrganId": "arteries_large", "type": "supplies_blood", "mechanism": "systolic_ejection",
      "description": "Pumps blood into the aorta during systole" },
    { "toOrganId": "blood_vessels_coronary", "type": "supplies_blood", "mechanism": "coronary_perfusion",
      "description": "Receives its own blood supply via the coronary arteries" }
  ]
}
```

#### `heart`
```json
{
  "organId": "heart",
  "displayName": "Heart",
  "nodeType": "anatomical",
  "fmaId": "FMA7088",
  "parentOrganId": null,
  "system": "cardiovascular",
  "meshId": "Body_Heart_LOD0",
  "description": "Four-chambered muscular organ that pumps blood through the pulmonary and systemic circulations.",
  "layer": "organs",
  "anatomicalRegion": "chest_anterior",
  "connections": []
}
```

#### `arteries_large`
```json
{
  "organId": "arteries_large",
  "displayName": "Large Arteries",
  "nodeType": "anatomical",
  "fmaId": "FMA50723",
  "parentOrganId": null,
  "system": "cardiovascular",
  "meshId": "Body_Arteries_Large_LOD0",
  "description": "Aorta and major branches; elastic conduits that distribute oxygenated blood from the heart to all body regions.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "blood_vessels_systemic", "type": "supplies_blood", "mechanism": "arterial_pressure",
      "description": "Distributes blood under pressure to smaller vessels throughout the body" }
  ]
}
```

#### `blood_vessels_systemic`
```json
{
  "organId": "blood_vessels_systemic",
  "displayName": "Systemic Microvasculature",
  "nodeType": "anatomical",
  "fmaId": "FMA50721",
  "parentOrganId": null,
  "system": "cardiovascular",
  "meshId": "Body_Vessels_Systemic_LOD0",
  "description": "Network of arterioles, capillaries, and venules throughout the body. Site of oxygen and nutrient exchange with tissues.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "tissues_peripheral", "type": "supplies_blood", "mechanism": "capillary_perfusion",
      "description": "Delivers oxygen and nutrients to peripheral tissues" }
  ]
}
```

#### `blood_vessels_coronary`
```json
{
  "organId": "blood_vessels_coronary",
  "displayName": "Coronary Arteries",
  "nodeType": "anatomical",
  "fmaId": "FMA50047",
  "parentOrganId": null,
  "system": "cardiovascular",
  "meshId": "Body_Vessels_Coronary_LOD0",
  "description": "Arteries supplying the heart muscle itself. Critical for cardiac function; narrowing causes ischemic heart disease.",
  "layer": "vascular",
  "anatomicalRegion": "chest_anterior",
  "connections": [
    { "toOrganId": "heart_left_ventricle", "type": "supplies_blood", "mechanism": "myocardial_perfusion",
      "description": "Supplies oxygenated blood to ventricular myocardium" }
  ]
}
```

#### `blood_red_cells`
```json
{
  "organId": "blood_red_cells",
  "displayName": "Red Blood Cells",
  "nodeType": "anatomical",
  "fmaId": "FMA62845",
  "parentOrganId": null,
  "system": "cardiovascular",
  "meshId": "Body_Blood_RedCells_LOD0",
  "description": "Oxygen-carrying cells produced in bone marrow under stimulation by erythropoietin. Reduced production causes anemia.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "tissues_peripheral", "type": "supplies_blood", "mechanism": "oxygen_delivery",
      "description": "Carry oxygen from lungs to tissues throughout the body" }
  ]
}
```

### B.3 Renal system

#### `kidney_left`
```json
{
  "organId": "kidney_left",
  "displayName": "Left Kidney",
  "nodeType": "anatomical",
  "fmaId": "FMA7204",
  "parentOrganId": null,
  "system": "urinary",
  "meshId": "Body_Kidney_Left_LOD0",
  "description": "Bean-shaped retroperitoneal organ filtering blood and producing urine. Also produces erythropoietin and activates vitamin D.",
  "layer": "organs",
  "anatomicalRegion": "abdomen_upper",
  "connections": [
    { "toOrganId": "kidney_glomerulus", "type": "contains", "mechanism": "structural",
      "description": "Glomeruli are the filtration units within the kidney" },
    { "toOrganId": "blood_red_cells", "type": "regulates", "mechanism": "erythropoietin_secretion",
      "description": "Secretes erythropoietin to stimulate red blood cell production" },
    { "toOrganId": "bone_skeletal_general", "type": "regulates", "mechanism": "vitamin_d_activation",
      "description": "Converts vitamin D to its active form for calcium absorption" }
  ]
}
```

#### `kidney_glomerulus`
```json
{
  "organId": "kidney_glomerulus",
  "displayName": "Renal Glomerulus",
  "nodeType": "anatomical",
  "fmaId": "FMA15624",
  "parentOrganId": "kidney_left",
  "system": "urinary",
  "meshId": "Body_Kidney_Glomerulus_LOD0",
  "description": "Tuft of capillaries within the kidney where blood filtration occurs. Damage causes proteinuria and reduced filtration.",
  "layer": "organs",
  "anatomicalRegion": "abdomen_upper",
  "connections": [
    { "toOrganId": "blood_vessels_systemic", "type": "filters", "mechanism": "glomerular_filtration",
      "description": "Filters plasma to form primary urine" }
  ]
}
```

### B.4 Nervous system

#### `brain_cerebrum`
```json
{
  "organId": "brain_cerebrum",
  "displayName": "Cerebrum",
  "nodeType": "anatomical",
  "fmaId": "FMA62000",
  "parentOrganId": "brain",
  "system": "nervous",
  "meshId": "Body_Brain_Cerebrum_LOD0",
  "description": "Largest part of the brain, controlling higher functions including movement, sensation, language, and cognition. Highly dependent on cerebral perfusion.",
  "layer": "nervous",
  "anatomicalRegion": "head",
  "connections": [
    { "toOrganId": "blood_vessels_systemic", "type": "drains_blood", "mechanism": "cerebral_circulation",
      "description": "Receives blood through cerebral arteries; vulnerable to hypertensive damage" }
  ]
}
```

#### `brain`
```json
{
  "organId": "brain",
  "displayName": "Brain",
  "nodeType": "anatomical",
  "fmaId": "FMA50801",
  "parentOrganId": null,
  "system": "nervous",
  "meshId": "Body_Brain_LOD0",
  "description": "Central organ of the nervous system, occupying the cranial cavity.",
  "layer": "nervous",
  "anatomicalRegion": "head",
  "connections": []
}
```

#### `nerves_peripheral_lower`
```json
{
  "organId": "nerves_peripheral_lower",
  "displayName": "Peripheral Nerves (Lower Extremity)",
  "nodeType": "anatomical",
  "fmaId": "FMA65229",
  "parentOrganId": null,
  "system": "nervous",
  "meshId": "Body_Nerves_PeripheralLower_LOD0",
  "description": "Nerve fibers innervating the legs and feet. Vulnerable to vascular and metabolic damage; symptoms appear distally first.",
  "layer": "nervous",
  "anatomicalRegion": "leg_left",
  "connections": [
    { "toOrganId": "blood_vessels_systemic", "type": "drains_blood", "mechanism": "vasa_nervorum",
      "description": "Receives oxygen via small vessels supplying the nerves themselves" }
  ]
}
```

### B.5 Sensory system

#### `eye_retina`
```json
{
  "organId": "eye_retina",
  "displayName": "Retina",
  "nodeType": "anatomical",
  "fmaId": "FMA58301",
  "parentOrganId": "eye",
  "system": "sensory",
  "meshId": "Body_Eye_Retina_LOD0",
  "description": "Light-sensitive layer at the back of the eye. Densely vascularized; damage from systemic disease produces visible retinopathy.",
  "layer": "nervous",
  "anatomicalRegion": "head",
  "connections": [
    { "toOrganId": "blood_vessels_systemic", "type": "drains_blood", "mechanism": "retinal_circulation",
      "description": "Supplied by retinal capillaries that mirror systemic vascular health" }
  ]
}
```

#### `eye`
```json
{
  "organId": "eye",
  "displayName": "Eye",
  "nodeType": "anatomical",
  "fmaId": "FMA54448",
  "parentOrganId": null,
  "system": "sensory",
  "meshId": "Body_Eye_LOD0",
  "description": "Organ of vision.",
  "layer": "organs",
  "anatomicalRegion": "head",
  "connections": []
}
```

### B.6 Hepatic system

#### `liver`
```json
{
  "organId": "liver",
  "displayName": "Liver",
  "nodeType": "anatomical",
  "fmaId": "FMA7197",
  "parentOrganId": null,
  "system": "digestive",
  "meshId": "Body_Liver_LOD0",
  "description": "Largest internal organ, central to metabolism, glycogen storage, and detoxification.",
  "layer": "organs",
  "anatomicalRegion": "abdomen_upper",
  "connections": [
    { "toOrganId": "blood_glucose_serum", "type": "regulates", "mechanism": "glycogen_storage",
      "description": "Stores excess glucose as glycogen and releases it under glucagon stimulation" }
  ]
}
```

### B.7 Skeletal & muscular systems

#### `bone_skeletal_general`
```json
{
  "organId": "bone_skeletal_general",
  "displayName": "Skeletal System",
  "nodeType": "anatomical",
  "fmaId": "FMA23881",
  "parentOrganId": null,
  "system": "skeletal",
  "meshId": "Body_Skeleton_LOD0",
  "description": "Full bone framework. Site of calcium storage and bone marrow. Mineral homeostasis is regulated by parathyroid hormone, calcitonin, and vitamin D.",
  "layer": "skeletal",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "bone_marrow", "type": "contains", "mechanism": "structural",
      "description": "Contains bone marrow within trabecular spaces" }
  ]
}
```

#### `bone_marrow`
```json
{
  "organId": "bone_marrow",
  "displayName": "Bone Marrow",
  "nodeType": "anatomical",
  "fmaId": "FMA9608",
  "parentOrganId": "bone_skeletal_general",
  "system": "lymphatic",
  "meshId": "Body_BoneMarrow_LOD0",
  "description": "Soft tissue inside bones; site of red and white blood cell production. Stimulated by erythropoietin from the kidneys.",
  "layer": "skeletal",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "blood_red_cells", "type": "produces", "mechanism": "erythropoiesis",
      "description": "Produces red blood cells in response to erythropoietin" }
  ]
}
```

#### `tissues_peripheral`
```json
{
  "organId": "tissues_peripheral",
  "displayName": "Peripheral Tissues",
  "nodeType": "anatomical",
  "fmaId": null,
  "parentOrganId": null,
  "system": "muscular",
  "meshId": "Body_Tissues_Peripheral_LOD0",
  "description": "Aggregate term for muscle and fat tissue throughout the body. Primary site of insulin-mediated glucose uptake.",
  "layer": "muscular",
  "anatomicalRegion": "systemic",
  "connections": []
}
```

### B.8 Physiological states (`nodeType: physiological_state`)

#### `blood_glucose_serum`
```json
{
  "organId": "blood_glucose_serum",
  "displayName": "Blood Glucose Level",
  "nodeType": "physiological_state",
  "fmaId": null,
  "parentOrganId": null,
  "system": "endocrine",
  "meshId": null,
  "description": "Circulating glucose concentration in the bloodstream. Sustained elevation defines diabetes and drives microvascular complications.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "kidney_glomerulus", "type": "filters", "mechanism": "renal_filtration",
      "description": "Filtered by the kidneys; high levels exceed reabsorptive capacity" }
  ]
}
```

#### `blood_pressure_systemic`
```json
{
  "organId": "blood_pressure_systemic",
  "displayName": "Systemic Blood Pressure",
  "nodeType": "physiological_state",
  "fmaId": null,
  "parentOrganId": null,
  "system": "cardiovascular",
  "meshId": null,
  "description": "Pressure exerted by circulating blood on arterial walls. Sustained elevation damages target organs throughout the body.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "blood_vessels_systemic", "type": "regulates", "mechanism": "arterial_pressure",
      "description": "Determines the force on arterial and arteriolar walls" },
    { "toOrganId": "heart_left_ventricle", "type": "regulates", "mechanism": "afterload",
      "description": "Defines the resistance the left ventricle must pump against" }
  ]
}
```

#### `serum_calcium`
```json
{
  "organId": "serum_calcium",
  "displayName": "Serum Calcium Level",
  "nodeType": "physiological_state",
  "fmaId": null,
  "parentOrganId": null,
  "system": "endocrine",
  "meshId": null,
  "description": "Circulating ionized calcium concentration. Tightly regulated by parathyroid hormone, calcitonin, and vitamin D.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "parathyroid_glands", "type": "signals", "mechanism": "calcium_sensing",
      "description": "Low calcium triggers parathyroid hormone release" }
  ]
}
```

#### `serum_phosphate`
```json
{
  "organId": "serum_phosphate",
  "displayName": "Serum Phosphate Level",
  "nodeType": "physiological_state",
  "fmaId": null,
  "parentOrganId": null,
  "system": "urinary",
  "meshId": null,
  "description": "Circulating phosphate concentration. Excreted by the kidneys; rises in CKD and drives mineral bone disease.",
  "layer": "vascular",
  "anatomicalRegion": "systemic",
  "connections": [
    { "toOrganId": "kidney_left", "type": "filters", "mechanism": "renal_excretion",
      "description": "Excreted by the kidneys; retention occurs as renal function declines" }
  ]
}
```

### B.9 Organ list summary

| organId | displayName | nodeType | layer | system | Used in cascades |
|---|---|---|---|---|---|
| pancreas | Pancreas | anatomical | organs | endocrine | T2D |
| pancreas_beta_cells | Pancreatic Beta Cells | anatomical | organs | endocrine | T2D |
| parathyroid_glands | Parathyroid Glands | anatomical | organs | endocrine | CKD |
| heart | Heart | anatomical | organs | cardiovascular | (parent only) |
| heart_left_ventricle | Left Ventricle | anatomical | organs | cardiovascular | HTN, CKD |
| arteries_large | Large Arteries | anatomical | vascular | cardiovascular | HTN |
| blood_vessels_systemic | Systemic Microvasculature | anatomical | vascular | cardiovascular | T2D, HTN |
| blood_vessels_coronary | Coronary Arteries | anatomical | vascular | cardiovascular | T2D |
| blood_red_cells | Red Blood Cells | anatomical | vascular | cardiovascular | CKD |
| kidney_left | Left Kidney | anatomical | organs | urinary | T2D, HTN, CKD |
| kidney_glomerulus | Renal Glomerulus | anatomical | organs | urinary | T2D, CKD |
| brain | Brain | anatomical | nervous | nervous | (parent only) |
| brain_cerebrum | Cerebrum | anatomical | nervous | nervous | HTN |
| nerves_peripheral_lower | Peripheral Nerves (Lower) | anatomical | nervous | nervous | T2D |
| eye | Eye | anatomical | organs | sensory | (parent only) |
| eye_retina | Retina | anatomical | nervous | sensory | T2D, HTN |
| liver | Liver | anatomical | organs | digestive | T2D (referenced in connections) |
| bone_skeletal_general | Skeletal System | anatomical | skeletal | skeletal | CKD |
| bone_marrow | Bone Marrow | anatomical | skeletal | lymphatic | CKD |
| tissues_peripheral | Peripheral Tissues | anatomical | muscular | muscular | T2D |
| blood_glucose_serum | Blood Glucose Level | physiological_state | vascular | endocrine | T2D |
| blood_pressure_systemic | Systemic Blood Pressure | physiological_state | vascular | cardiovascular | HTN |
| serum_calcium | Serum Calcium | physiological_state | vascular | endocrine | CKD |
| serum_phosphate | Serum Phosphate | physiological_state | vascular | urinary | CKD |

**Total: 24 nodes** (20 anatomical + 4 physiological states). All cascade `targetOrganId` references resolve.

---

## Part C — Hypertension Cascade

Sources: Wikipedia "End organ damage" (current); Pathogenesis of TOD (PMC4307277); Hypertension and TOD (PMC4948792); Mechanisms of TOD (Sciencedirect); RAAS in End-Organ Damage (PMC4964362). All findings consistent with standard medical school curricula.

```json
{
  "schemaVersion": 1,
  "diseaseId": "essential_hypertension",
  "displayName": "Essential Hypertension",
  "shortLabel": "Hypertension",
  "category": "cardiovascular",
  "icd10": "I10",
  "description": "Chronically elevated systemic arterial pressure of unknown specific cause. Sustained elevation damages target organs through microvascular dysfunction, endothelial injury, and pressure-induced structural remodeling, primarily affecting heart, kidneys, brain, retina, and large vessels.",
  "entryOrganId": "blood_pressure_systemic",

  "stages": [
    {
      "stageId": "early",
      "label": "Early (0–5 years)",
      "description": "Sustained elevated blood pressure begins inducing endothelial dysfunction and microvascular changes. Often asymptomatic.",
      "stepIndices": [0, 1, 2, 3]
    },
    {
      "stageId": "intermediate",
      "label": "Intermediate (5–15 years)",
      "description": "Cardiac structural changes develop. Microvascular damage spreads to retina, kidneys, and brain. Early end-organ damage detectable.",
      "stepIndices": [4, 5, 6, 7]
    },
    {
      "stageId": "advanced",
      "label": "Advanced (15+ years)",
      "description": "Established target organ damage. Increased risk of stroke, heart failure, chronic kidney disease, and vision loss.",
      "stepIndices": [8, 9, 10]
    }
  ],

  "cascade": [
    {
      "stepIndex": 0,
      "stage": "early",
      "targetOrganId": "blood_pressure_systemic",
      "mechanismKey": "sustained_pressure_elevation",
      "severity": 30,
      "delayMs": 0,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FF4040",
        "intensity": 0.5
      },
      "narrationFallback": "Systemic blood pressure rises and remains elevated, exerting greater force against arterial walls throughout the body.",
      "aiContext": {
        "physiology": "sustained systemic hypertension",
        "clinicalNote": "Often asymptomatic; detected on routine checks. Defined as ≥130/80 mmHg per current guidelines.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 1,
      "stage": "early",
      "targetOrganId": "blood_vessels_systemic",
      "mechanismKey": "endothelial_dysfunction",
      "severity": 35,
      "delayMs": 2500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#C44545",
        "toColor": "#A53333",
        "fadeMs": 2500
      },
      "narrationFallback": "The inner lining of blood vessels — the endothelium — begins to malfunction under sustained pressure, reducing nitric oxide and impairing vessel relaxation.",
      "aiContext": {
        "physiology": "endothelial dysfunction with reduced NO bioavailability",
        "clinicalNote": "The earliest reversible stage; aggressive blood pressure control here can prevent later complications.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 2,
      "stage": "early",
      "targetOrganId": "arteries_large",
      "mechanismKey": "arterial_stiffening",
      "severity": 40,
      "delayMs": 5000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.2,
        "durationMs": 2500
      },
      "narrationFallback": "Large arteries begin to stiffen as elastin fibers are gradually replaced by collagen, reducing their ability to buffer the heart's pulsing flow.",
      "aiContext": {
        "physiology": "arterial stiffening and reduced compliance",
        "clinicalNote": "Detectable as widening pulse pressure on standard blood pressure measurement.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 3,
      "stage": "early",
      "targetOrganId": "heart_left_ventricle",
      "mechanismKey": "compensatory_lvh_early",
      "severity": 35,
      "delayMs": 8000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.15,
        "durationMs": 3000
      },
      "narrationFallback": "The left ventricle begins to thicken as it works harder to pump blood against increased arterial pressure.",
      "aiContext": {
        "physiology": "early left ventricular hypertrophy",
        "clinicalNote": "Visible on echocardiogram before symptoms develop.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 4,
      "stage": "intermediate",
      "targetOrganId": "blood_vessels_systemic",
      "mechanismKey": "microvascular_remodeling",
      "severity": 55,
      "delayMs": 11500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.35,
        "durationMs": 3000
      },
      "narrationFallback": "Small blood vessels throughout the body remodel and stiffen, narrowing their lumens and reducing oxygen delivery to tissues.",
      "aiContext": {
        "physiology": "microvascular rarefaction and inward remodeling",
        "clinicalNote": "Drives the systemic nature of hypertensive end-organ damage.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 5,
      "stage": "intermediate",
      "targetOrganId": "kidney_glomerulus",
      "mechanismKey": "hypertensive_nephrosclerosis_early",
      "severity": 50,
      "delayMs": 14500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#D44545",
        "intensity": 0.6
      },
      "narrationFallback": "The kidney's filtering units sustain damage from sustained pressure, and small amounts of protein begin appearing in the urine.",
      "aiContext": {
        "physiology": "early hypertensive nephrosclerosis with microalbuminuria",
        "clinicalNote": "Microalbuminuria is the earliest detectable sign of hypertensive kidney damage.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 6,
      "stage": "intermediate",
      "targetOrganId": "eye_retina",
      "mechanismKey": "hypertensive_retinopathy",
      "severity": 50,
      "delayMs": 17500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#D44545",
        "intensity": 0.7
      },
      "narrationFallback": "Retinal blood vessels narrow and develop characteristic kinks visible on eye examination, signaling systemic vascular damage.",
      "aiContext": {
        "physiology": "hypertensive retinopathy",
        "clinicalNote": "The retina is the only place where small arteries can be examined directly without imaging.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 7,
      "stage": "intermediate",
      "targetOrganId": "heart_left_ventricle",
      "mechanismKey": "established_lvh",
      "severity": 65,
      "delayMs": 20500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.4,
        "durationMs": 3000
      },
      "narrationFallback": "Years of pumping against high pressure produce significant thickening of the left ventricle, reducing its efficiency and increasing oxygen demand.",
      "aiContext": {
        "physiology": "established concentric left ventricular hypertrophy",
        "clinicalNote": "Major risk factor for arrhythmia, heart failure, and sudden cardiac death.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 8,
      "stage": "advanced",
      "targetOrganId": "brain_cerebrum",
      "mechanismKey": "small_vessel_cerebral_disease",
      "severity": 75,
      "delayMs": 23500,
      "durationMs": 3500,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#E8C8B8",
        "toColor": "#A85C45",
        "fadeMs": 3000
      },
      "narrationFallback": "Damage to small cerebral arteries causes microscopic areas of brain tissue death, contributing to cognitive decline and increasing the risk of stroke.",
      "aiContext": {
        "physiology": "cerebral small vessel disease and lacunar infarcts",
        "clinicalNote": "Hypertension triples the risk of stroke and is a leading cause of vascular dementia.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 9,
      "stage": "advanced",
      "targetOrganId": "kidney_left",
      "mechanismKey": "hypertensive_ckd",
      "severity": 80,
      "delayMs": 27000,
      "durationMs": 3500,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#A85C45",
        "toColor": "#6B3825",
        "fadeMs": 3000
      },
      "narrationFallback": "Progressive kidney damage results in chronic kidney disease, with declining filtration capacity and rising waste products in the blood.",
      "aiContext": {
        "physiology": "hypertensive chronic kidney disease",
        "clinicalNote": "Hypertension is the second leading cause of end-stage renal disease worldwide.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 10,
      "stage": "advanced",
      "targetOrganId": "heart_left_ventricle",
      "mechanismKey": "heart_failure",
      "severity": 90,
      "delayMs": 30500,
      "durationMs": 3500,
      "visualEffect": {
        "type": "particle_emit",
        "particleType": "inflammation",
        "rate": 35
      },
      "narrationFallback": "The thickened, overworked left ventricle eventually loses pumping efficiency, producing heart failure with shortness of breath, fatigue, and fluid retention.",
      "aiContext": {
        "physiology": "hypertensive heart failure",
        "clinicalNote": "Hypertension triples lifetime risk of heart failure.",
        "audience": "general"
      }
    }
  ],

  "metadata": {
    "sources": [
      "Mechanisms of target organ damage caused by hypertension (Sciencedirect, 2005)",
      "Pathogenesis of Target Organ Damage in Hypertension (PMC4307277)",
      "Hypertension and Target Organ Damage (PMC4948792)",
      "Pathophysiology of Hypertension Mosaic Theory (PMC8023760)",
      "Systemic and Cardiac Microvascular Dysfunction in Hypertension (PMC11677602)"
    ],
    "medicalReviewedBy": null,
    "medicalReviewedDate": null,
    "version": "0.9-pre-review",
    "totalDurationMs": 34000,
    "stepCount": 11
  }
}
```

---

## Part D — Chronic Kidney Disease Cascade

Sources: StatPearls "Chronic Kidney Disease" (NIH, 2024); StatPearls "End-Stage Renal Disease" (NIH, 2025); CKD complications (PMC2474786); CKD-MBD bone disease (PMC10941011); AMBOSS Chronic Kidney Disease; Wikipedia CKD (current). All findings consistent with KDIGO guidelines.

```json
{
  "schemaVersion": 1,
  "diseaseId": "chronic_kidney_disease",
  "displayName": "Chronic Kidney Disease",
  "shortLabel": "Chronic Kidney Disease",
  "category": "renal",
  "icd10": "N18",
  "description": "Progressive loss of kidney function over months to years, regardless of cause. As filtration declines, multiple downstream complications develop: anemia from reduced erythropoietin, mineral and bone disease from disrupted calcium-phosphate balance, cardiovascular disease, and ultimately end-stage renal failure requiring dialysis or transplant.",
  "entryOrganId": "kidney_glomerulus",

  "stages": [
    {
      "stageId": "early",
      "label": "Early — Stages 1–2 (eGFR ≥60)",
      "description": "Detectable kidney damage with preserved or mildly reduced filtration. Often asymptomatic. Markers like proteinuria appear.",
      "stepIndices": [0, 1, 2]
    },
    {
      "stageId": "intermediate",
      "label": "Intermediate — Stage 3 (eGFR 30–59)",
      "description": "Moderate reduction in filtration. Erythropoietin and vitamin D metabolism become impaired. Anemia and early bone disease begin.",
      "stepIndices": [3, 4, 5, 6]
    },
    {
      "stageId": "advanced",
      "label": "Advanced — Stages 4–5 (eGFR <30)",
      "description": "Severe loss of kidney function approaching end-stage renal disease. Multiple systemic complications including significant cardiovascular disease.",
      "stepIndices": [7, 8, 9, 10]
    }
  ],

  "cascade": [
    {
      "stepIndex": 0,
      "stage": "early",
      "targetOrganId": "kidney_glomerulus",
      "mechanismKey": "glomerular_damage_initial",
      "severity": 30,
      "delayMs": 0,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FFB800",
        "intensity": 0.5
      },
      "narrationFallback": "Glomerular filtration units begin sustaining damage from underlying causes such as diabetes, hypertension, or autoimmune disease.",
      "aiContext": {
        "physiology": "early glomerular injury",
        "clinicalNote": "Most CKD is caused by diabetes or hypertension, but many other causes exist.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 1,
      "stage": "early",
      "targetOrganId": "kidney_left",
      "mechanismKey": "proteinuria_onset",
      "severity": 35,
      "delayMs": 2500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#A85C45",
        "toColor": "#8B4830",
        "fadeMs": 2500
      },
      "narrationFallback": "Damaged kidney filters allow protein to leak into the urine, an early warning sign detectable on a urine test.",
      "aiContext": {
        "physiology": "albuminuria as early CKD marker",
        "clinicalNote": "Microalbuminuria is the earliest detectable sign and a key predictor of progression.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 2,
      "stage": "early",
      "targetOrganId": "blood_pressure_systemic",
      "mechanismKey": "renal_hypertension",
      "severity": 45,
      "delayMs": 5000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FF4040",
        "intensity": 0.6
      },
      "narrationFallback": "Damaged kidneys disrupt fluid and hormone balance, raising systemic blood pressure and creating a self-reinforcing cycle of further kidney damage.",
      "aiContext": {
        "physiology": "renin-angiotensin-aldosterone activation in CKD",
        "clinicalNote": "CKD and hypertension worsen each other; controlling blood pressure is essential to slow CKD progression.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 3,
      "stage": "intermediate",
      "targetOrganId": "kidney_left",
      "mechanismKey": "epo_synthesis_decline",
      "severity": 55,
      "delayMs": 8000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "shrink",
        "scale": 0.93,
        "durationMs": 2500
      },
      "narrationFallback": "As functional kidney tissue is lost, the kidneys produce less erythropoietin — the hormone that tells bone marrow to make red blood cells.",
      "aiContext": {
        "physiology": "decreased erythropoietin synthesis from tubulointerstitial fibrosis",
        "clinicalNote": "Erythropoietin deficiency is the primary mechanism of CKD-associated anemia.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 4,
      "stage": "intermediate",
      "targetOrganId": "bone_marrow",
      "mechanismKey": "reduced_erythropoiesis",
      "severity": 55,
      "delayMs": 11500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#D44545",
        "toColor": "#9B3535",
        "fadeMs": 3000
      },
      "narrationFallback": "With less erythropoietin reaching it, bone marrow produces fewer red blood cells, leading to anemia and chronic fatigue.",
      "aiContext": {
        "physiology": "anemia of chronic kidney disease",
        "clinicalNote": "Affects 8% at stage 1 and over 50% at stage 5. Treatable with EPO-stimulating agents and iron.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 5,
      "stage": "intermediate",
      "targetOrganId": "serum_phosphate",
      "mechanismKey": "phosphate_retention",
      "severity": 50,
      "delayMs": 14500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FF8040",
        "intensity": 0.6
      },
      "narrationFallback": "Failing kidneys can no longer fully excrete phosphate, allowing levels to rise in the blood and disrupting calcium balance.",
      "aiContext": {
        "physiology": "hyperphosphatemia from reduced renal excretion",
        "clinicalNote": "Phosphate retention drives much of CKD bone disease and cardiovascular calcification.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 6,
      "stage": "intermediate",
      "targetOrganId": "parathyroid_glands",
      "mechanismKey": "secondary_hyperparathyroidism",
      "severity": 60,
      "delayMs": 17500,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FFB800",
        "intensity": 0.7
      },
      "narrationFallback": "In response to high phosphate and low active vitamin D, the parathyroid glands grow and secrete excess parathyroid hormone, attempting to correct the imbalance.",
      "aiContext": {
        "physiology": "secondary hyperparathyroidism in CKD-MBD",
        "clinicalNote": "Initially adaptive, but chronic overactivation drives bone disease and vascular calcification.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 7,
      "stage": "advanced",
      "targetOrganId": "bone_skeletal_general",
      "mechanismKey": "renal_osteodystrophy",
      "severity": 75,
      "delayMs": 20500,
      "durationMs": 3500,
      "visualEffect": {
        "type": "color_change",
        "fromColor": "#E8DCC0",
        "toColor": "#B8A080",
        "fadeMs": 3000
      },
      "narrationFallback": "Sustained parathyroid hormone elevation pulls calcium from the bones, weakening the skeleton and causing bone pain, deformity, and fracture risk.",
      "aiContext": {
        "physiology": "chronic kidney disease mineral and bone disorder (CKD-MBD)",
        "clinicalNote": "Spectrum includes high-turnover, low-turnover, and mixed bone disease.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 8,
      "stage": "advanced",
      "targetOrganId": "blood_vessels_systemic",
      "mechanismKey": "vascular_calcification",
      "severity": 75,
      "delayMs": 24000,
      "durationMs": 3500,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.45,
        "durationMs": 3500
      },
      "narrationFallback": "Calcium displaced from bone, combined with high phosphate, deposits in arterial walls, accelerating vascular stiffening and cardiovascular risk.",
      "aiContext": {
        "physiology": "vascular calcification in CKD-MBD",
        "clinicalNote": "A key reason cardiovascular disease causes most deaths in CKD patients.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 9,
      "stage": "advanced",
      "targetOrganId": "heart_left_ventricle",
      "mechanismKey": "cardiorenal_syndrome",
      "severity": 85,
      "delayMs": 27500,
      "durationMs": 3500,
      "visualEffect": {
        "type": "thicken",
        "thicknessFactor": 1.5,
        "durationMs": 3500
      },
      "narrationFallback": "The combination of anemia, fluid overload, hypertension, and vascular calcification overburdens the heart, producing left ventricular hypertrophy and progressive heart failure.",
      "aiContext": {
        "physiology": "cardiorenal anemia syndrome",
        "clinicalNote": "Cardiovascular disease accounts for the majority of mortality in CKD.",
        "audience": "general"
      }
    },
    {
      "stepIndex": 10,
      "stage": "advanced",
      "targetOrganId": "kidney_left",
      "mechanismKey": "end_stage_renal_disease",
      "severity": 95,
      "delayMs": 31000,
      "durationMs": 4000,
      "visualEffect": {
        "type": "shrink",
        "scale": 0.7,
        "durationMs": 3500
      },
      "narrationFallback": "Kidney function falls below the threshold required to sustain life. Dialysis or kidney transplantation becomes necessary.",
      "aiContext": {
        "physiology": "end-stage renal disease, eGFR <15",
        "clinicalNote": "Without renal replacement therapy, ESRD is fatal within weeks to months.",
        "audience": "general"
      }
    }
  ],

  "metadata": {
    "sources": [
      "StatPearls — Chronic Kidney Disease (NCBI, 2024)",
      "StatPearls — End-Stage Renal Disease (NCBI, 2025)",
      "Chronic Kidney Disease and Its Complications (PMC2474786)",
      "Bone and bone derived factors in kidney disease (PMC10941011)",
      "AMBOSS Chronic Kidney Disease",
      "KDIGO Clinical Practice Guidelines for CKD (2024)"
    ],
    "medicalReviewedBy": null,
    "medicalReviewedDate": null,
    "version": "0.9-pre-review",
    "totalDurationMs": 35000,
    "stepCount": 11
  }
}
```

---

## Part E — Test Fixtures

Minimal fixtures for unit testing CORE-005 (Interconnectivity Engine), CORE-008 (Data Layer), and the JSON validator.

### E.1 `minimal_organ_graph.json`

Three connected nodes with predictable structure. Used to verify graph traversal logic without depending on the full Phase 1 organ list.

```json
{
  "organs": [
    {
      "schemaVersion": 1,
      "organId": "test_node_a",
      "displayName": "Test Node A",
      "nodeType": "anatomical",
      "fmaId": "FMA_TEST_A",
      "parentOrganId": null,
      "system": "endocrine",
      "meshId": "Test_NodeA_LOD0",
      "description": "Test fixture node A. Connects to B with severity 50.",
      "layer": "organs",
      "anatomicalRegion": "systemic",
      "connections": [
        { "toOrganId": "test_node_b", "type": "regulates", "mechanism": "test_mechanism_ab",
          "description": "Test connection A to B" }
      ],
      "metadata": { "sources": ["test fixture"], "lastReviewed": null }
    },
    {
      "schemaVersion": 1,
      "organId": "test_node_b",
      "displayName": "Test Node B",
      "nodeType": "anatomical",
      "fmaId": "FMA_TEST_B",
      "parentOrganId": null,
      "system": "endocrine",
      "meshId": "Test_NodeB_LOD0",
      "description": "Test fixture node B. Connects to C.",
      "layer": "organs",
      "anatomicalRegion": "systemic",
      "connections": [
        { "toOrganId": "test_node_c", "type": "signals", "mechanism": "test_mechanism_bc",
          "description": "Test connection B to C" }
      ],
      "metadata": { "sources": ["test fixture"], "lastReviewed": null }
    },
    {
      "schemaVersion": 1,
      "organId": "test_node_c",
      "displayName": "Test Node C",
      "nodeType": "anatomical",
      "fmaId": "FMA_TEST_C",
      "parentOrganId": null,
      "system": "endocrine",
      "meshId": "Test_NodeC_LOD0",
      "description": "Test fixture node C. Terminal node.",
      "layer": "organs",
      "anatomicalRegion": "systemic",
      "connections": [],
      "metadata": { "sources": ["test fixture"], "lastReviewed": null }
    }
  ]
}
```

### E.2 `minimal_disease.json`

A simple 3-step linear cascade for testing cascade playback, narration scheduling, and timing. Designed to be fast (9 seconds total) so tests run quickly.

```json
{
  "schemaVersion": 1,
  "diseaseId": "test_disease",
  "displayName": "Test Disease",
  "shortLabel": "Test",
  "category": "metabolic",
  "icd10": "TEST",
  "description": "Test fixture disease. Three-step linear cascade across test nodes A, B, C.",
  "entryOrganId": "test_node_a",
  "stages": [
    {
      "stageId": "only",
      "label": "Only Stage",
      "description": "Single test stage.",
      "stepIndices": [0, 1, 2]
    }
  ],
  "cascade": [
    {
      "stepIndex": 0,
      "stage": "only",
      "targetOrganId": "test_node_a",
      "mechanismKey": "test_mechanism_a",
      "severity": 30,
      "delayMs": 0,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FF0000",
        "intensity": 0.5
      },
      "narrationFallback": "Test step zero. Node A activated.",
      "aiContext": {
        "physiology": "test physiology zero",
        "clinicalNote": "test clinical note zero",
        "audience": "general"
      }
    },
    {
      "stepIndex": 1,
      "stage": "only",
      "targetOrganId": "test_node_b",
      "mechanismKey": "test_mechanism_b",
      "severity": 50,
      "delayMs": 3000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#FFFF00",
        "intensity": 0.5
      },
      "narrationFallback": "Test step one. Node B activated.",
      "aiContext": {
        "physiology": "test physiology one",
        "clinicalNote": "test clinical note one",
        "audience": "general"
      }
    },
    {
      "stepIndex": 2,
      "stage": "only",
      "targetOrganId": "test_node_c",
      "mechanismKey": "test_mechanism_c",
      "severity": 80,
      "delayMs": 6000,
      "durationMs": 3000,
      "visualEffect": {
        "type": "highlight_pulse",
        "color": "#00FF00",
        "intensity": 0.5
      },
      "narrationFallback": "Test step two. Node C activated.",
      "aiContext": {
        "physiology": "test physiology two",
        "clinicalNote": "test clinical note two",
        "audience": "general"
      }
    }
  ],
  "metadata": {
    "sources": ["test fixture"],
    "medicalReviewedBy": "test fixture",
    "medicalReviewedDate": "2026-01-01",
    "version": "1.0-test",
    "totalDurationMs": 9000,
    "stepCount": 3
  }
}
```

### E.3 Required test cases for CORE-005 / CORE-008

The fixtures above support these tests:

```csharp
namespace AnatomiQ.Tests.EditMode
{
    public class InterconnectivityEngineTests
    {
        [Test] public void GetCascadeSequence_ValidDisease_ReturnsOrderedSteps() { /* uses minimal_disease */ }
        [Test] public void GetCascadeSequence_StepsInExpectedOrder() { /* checks stepIndex ordering */ }
        [Test] public void GetCascadeSequence_TimingMonotonic() { /* delayMs increases */ }
        [Test] public void GetConnectedOrgans_NodeA_ReturnsB() { /* uses minimal_organ_graph */ }
        [Test] public void GetConnectedOrgans_NodeC_ReturnsEmpty() { /* terminal node */ }
        [Test] public void GetReachableOrgans_FromA_ReturnsBAndC() { /* graph traversal */ }
    }

    public class DataValidationTests
    {
        [Test] public void ValidateCascade_AllTargetOrgansExist_Passes() { /* T2D + Phase 1 organs */ }
        [Test] public void ValidateCascade_MissingTargetOrgan_Fails() { /* T2D - 1 organ */ }
        [Test] public void ValidateCascade_StepIndicesInExactlyOneStage_Passes() { /* T2D */ }
        [Test] public void ValidateCascade_DuplicateStepIndex_Fails() { /* malformed */ }
        [Test] public void ValidateCascade_NegativeDelay_Fails() { /* malformed */ }
        [Test] public void ValidateCascade_EmptyNarrationFallback_Fails() { /* malformed */ }
        [Test] public void ValidateOrgan_PhysiologicalState_AllowsNullMeshId() { /* blood_glucose */ }
        [Test] public void ValidateOrgan_Anatomical_RequiresMeshId() { /* schema rule */ }
    }
}
```

### E.4 Where fixtures live

```
Assets/_AnatomiQ/Tests/EditMode/Fixtures/
    minimal_organ_graph.json
    minimal_disease.json
```

Loaded in tests via `Resources.Load` after copying to a `Resources` folder, or via `AssetDatabase.LoadAssetAtPath` in editor-only tests. **Do not** include test fixtures in the build — they're test-only assets.

---

## Part F — Medical Review Submission Checklist

When the medical reviewer is identified (Risk R1), send the following in one batch:

- [ ] One-page project overview (extracted from Project Overview docx)
- [ ] This document
- [ ] T2 Diabetes cascade (already in `AnatomiQ_Data_Schemas.md` Section 4)
- [ ] Plain-language summary of the cascade format and what the reviewer is checking
- [ ] Specific request: medical accuracy of cascade sequences, narration text, mechanism keys, and severity levels
- [ ] Estimated time required: 30-60 min per disease, ~3 hours total
- [ ] Offer of acknowledgement on project documentation and any resulting paper

The Medical Review Request template in `AnatomiQ_Operations_And_Planning.md` Part C.7 has the email text.

---

## Part G — Open Authoring Notes

Decisions worth flagging for the medical reviewer's attention:

**1. Generic timelines.** Cascades show "early / intermediate / advanced" rather than specific years. Real disease progression varies by patient. Timeline labels include rough year ranges as guidance ("0–5 years") but reviewer should confirm these are reasonable for an educational tool.

**2. Severity scale subjectivity.** Severity 0–100 is calibrated by feel rather than clinical metric. Reviewer should confirm the *relative* severities make sense (e.g. early proteinuria less severe than ESRD).

**3. Mechanism key naming.** Mechanism keys (e.g. `secondary_hyperparathyroidism`) are used internally for caching AI narrations and grouping similar mechanisms across diseases. Reviewer doesn't need to validate these as long as the narration text and clinical notes are accurate.

**4. Audience level.** All cascades currently use `audience: general`. Reviewer should confirm narrations are appropriate for a general audience (e.g. patient or curious learner). Student/clinician variants can be added later via the same data structure.

**5. Cascade endpoints.** Each cascade ends with a serious complication rather than a neutral state. This is medically accurate but may feel abrupt. Reviewer can suggest if a "with treatment, this can be slowed" closing step would improve the educational framing. Schema supports adding such steps.

---

*Phase 1 medical content v1 · 2026 · status: 0.9-pre-review · authored from peer-reviewed sources, awaiting medical professional sign-off*
