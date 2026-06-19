using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AnatomiQ.Data
{
    /// <summary>
    /// CORE-008 runtime schema for a disease and its cascade (Data Schemas §3, §5). A disease defines
    /// what condition is simulated, where the cascade starts (<see cref="EntryOrganId"/>), the
    /// time-based <see cref="Stages"/>, and the ordered <see cref="Cascade"/> of organ-activation
    /// steps the Interconnectivity Engine (CORE-005) plays back. Authored as JSON in Content/diseases/
    /// and imported to this ScriptableObject at build time; public fields mirror the JSON 1:1.
    /// </summary>
    [CreateAssetMenu(fileName = "Disease", menuName = "AnatomiQ/Disease")]
    public class DiseaseAsset : ScriptableObject
    {
        /// <summary>Schema version. Always 1 for the current schema.</summary>
        public int SchemaVersion = 1;

        /// <summary>Unique disease identifier (e.g. "t2_diabetes").</summary>
        public string DiseaseId;

        /// <summary>Full display name (e.g. "Type 2 Diabetes Mellitus").</summary>
        public string DisplayName;

        /// <summary>Short label for compact UI (e.g. "Type 2 Diabetes").</summary>
        public string ShortLabel;

        /// <summary>Clinical category of the disease.</summary>
        public DiseaseCategory Category;

        /// <summary>ICD-10 code (e.g. "E11"). JSON key is <c>icd10</c>.</summary>
        [JsonProperty("icd10")] public string Icd10Code;

        /// <summary>Several-sentence clinical description.</summary>
        [TextArea(3, 6)] public string Description;

        /// <summary>The organ where the cascade begins. Must exist in organ data.</summary>
        public string EntryOrganId;

        /// <summary>Time-based phases partitioning the cascade (drives ATLAS-004).</summary>
        public List<DiseaseStage> Stages;

        /// <summary>The ordered sequence of organ-activation steps.</summary>
        public List<CascadeStep> Cascade;

        /// <summary>Provenance, medical review tracking, and computed totals.</summary>
        public DiseaseMetadata Metadata;
    }

    /// <summary>A time-based phase grouping a subset of cascade steps (Data Schemas §3.2).</summary>
    [Serializable]
    public class DiseaseStage
    {
        /// <summary>Unique stage identifier within the disease (e.g. "early").</summary>
        public string StageId;

        /// <summary>Human-readable stage label (e.g. "Early (0-5 years)").</summary>
        public string Label;

        /// <summary>Description of what happens physiologically during this stage.</summary>
        [TextArea(2, 4)] public string Description;

        /// <summary>Indices into <see cref="DiseaseAsset.Cascade"/> belonging to this stage.</summary>
        public List<int> StepIndices;
    }

    /// <summary>One organ-activation event in the animated propagation (Data Schemas §3.3).</summary>
    [Serializable]
    public class CascadeStep
    {
        /// <summary>Zero-based ordering within the cascade.</summary>
        public int StepIndex;

        /// <summary>The stageId this step belongs to.</summary>
        public string Stage;

        /// <summary>The organ being activated. Must exist in organ data.</summary>
        public string TargetOrganId;

        /// <summary>Canonical mechanism key — used for narration caching and fallback lookup.</summary>
        public string MechanismKey;

        /// <summary>0-100 severity, driving visual intensity (30 mild, 60 moderate, 90 severe).</summary>
        [Range(0, 100)] public int Severity;

        /// <summary>Start time of this step measured from cascade start, in milliseconds.</summary>
        public int DelayMs;

        /// <summary>How long the activation animation plays, in milliseconds.</summary>
        public int DurationMs;

        /// <summary>The visual effect to play on the 3D model for this step. JSON key is <c>visualEffect</c>.</summary>
        [JsonProperty("visualEffect")] public VisualEffect Effect;

        /// <summary>Pre-baked, medically-reviewed narration used when AI is offline. Must stand alone.</summary>
        [TextArea(2, 5)] public string NarrationFallback;

        /// <summary>Hints injected into the AI narration prompt for live narration.</summary>
        public AiContext AiContext;
    }

    /// <summary>
    /// A visual effect on the 3D model (Data Schemas §3.5). Different fields apply to different
    /// <see cref="VisualEffectType"/>s; unused fields are left at their defaults.
    /// </summary>
    [Serializable]
    public class VisualEffect
    {
        /// <summary>Which effect to play.</summary>
        public VisualEffectType Type;

        public string Color;          // hex; used by highlight_pulse, connection_line
        public string FromColor;      // used by color_change
        public string ToColor;        // used by color_change
        public float Intensity;       // 0-1; used by highlight_pulse
        public float FadeMs;          // used by color_change
        public string ParticleType;   // used by particle_emit
        public int Rate;              // used by particle_emit
        public float Scale;           // used by shrink
        public float ThicknessFactor; // used by thicken
        public string FromOrganId;    // used by connection_line
        public string ToOrganId;      // used by connection_line
        public float DurationMs;      // optional animation duration override
    }

    /// <summary>Hints for the AI narration prompt (Data Schemas §3.3).</summary>
    [Serializable]
    public class AiContext
    {
        /// <summary>Short physiology summary for the mechanism.</summary>
        public string Physiology;

        /// <summary>Clinical note adding context for the narration.</summary>
        public string ClinicalNote;

        /// <summary>Target audience level for tone/complexity.</summary>
        public AudienceLevel Audience;
    }

    /// <summary>Provenance, review tracking, and computed totals for a disease.</summary>
    [Serializable]
    public class DiseaseMetadata
    {
        /// <summary>Citation strings for the cascade's medical basis.</summary>
        public List<string> Sources;

        /// <summary>Name of the medical reviewer, or null if unreviewed.</summary>
        public string MedicalReviewedBy;

        /// <summary>ISO date of medical review, or null if unreviewed.</summary>
        public string MedicalReviewedDate;

        /// <summary>Content version string (e.g. "0.9-pre-review").</summary>
        public string Version;

        /// <summary>Total cascade runtime in milliseconds (computed/authored).</summary>
        public int TotalDurationMs;

        /// <summary>Number of cascade steps (computed/authored).</summary>
        public int StepCount;
    }

    /// <summary>Clinical categories for diseases (Data Schemas §5).</summary>
    public enum DiseaseCategory
    {
        Metabolic, Cardiovascular, Respiratory, Neurological,
        Renal, Endocrine, Autoimmune, Infectious, Oncological
    }

    /// <summary>Visual effect kinds the 3D renderer (CORE-002) implements (Data Schemas §3.5).</summary>
    public enum VisualEffectType
    {
        HighlightPulse, ColorChange, ParticleEmit, ConnectionLine, Shrink, Thicken
    }

    /// <summary>Audience levels for narration tone/complexity.</summary>
    public enum AudienceLevel
    {
        General, Patient, Student, Clinician
    }
}
