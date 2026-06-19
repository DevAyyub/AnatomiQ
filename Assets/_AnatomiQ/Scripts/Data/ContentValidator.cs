using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnatomiQ.Data
{
    /// <summary>
    /// Outcome of validating one content asset. <see cref="Errors"/> are hard failures — the caller
    /// (the Data Layer / Editor importer) skips the asset. <see cref="Warnings"/> are soft issues
    /// that degrade gracefully and leave the asset usable (e.g. a single unresolved connection edge,
    /// per the CORE-008 drop-the-edge decision).
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>Hard failures. Non-empty means the asset must be skipped.</summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>Soft issues. The asset is kept; the caller may act on these (e.g. drop an edge).</summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>True when there are no hard failures.</summary>
        public bool IsValid => Errors.Count == 0;

        internal void Error(string message) => Errors.Add(message);
        internal void Warn(string message) => Warnings.Add(message);
    }

    /// <summary>
    /// CORE-008 validation rules (Data Schemas §9, plus the organ rules implied by §2.3 and the
    /// Phase 1 physiological-state relaxations). Operates on already-parsed ScriptableObjects and is
    /// pure: it never throws, never mutates, never logs — it returns a <see cref="ValidationResult"/>.
    /// The caller decides what to do (skip on errors, act on warnings, log specifics). This keeps the
    /// rule engine free of Unity and trivially unit-testable.
    ///
    /// Set-level uniqueness: <see cref="ValidateDisease"/> enforces unique diseaseId via the
    /// <c>existingDiseaseIds</c> argument (a §9 rule). Organ-id uniqueness is enforced where the
    /// dictionary is built (the Data Layer), not here.
    /// </summary>
    public static class ContentValidator
    {
        /// <summary>The schema version this build understands. Assets at other versions are rejected.</summary>
        public const int CurrentSchemaVersion = 1;

        private static readonly Regex _organIdPattern = new Regex("^[a-z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex _hexColorPattern =
            new Regex("^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

        /// <summary>
        /// Validates one organ. Intra-asset rules always run; cross-reference rules (parent and
        /// connection targets resolve) run only when <paramref name="knownOrganIds"/> is supplied,
        /// and surface as WARNINGS (the organ stays usable; an unresolved edge is dropped by the caller).
        /// </summary>
        public static ValidationResult ValidateOrgan(
            OrganAsset organ,
            IReadOnlyCollection<string> knownOrganIds = null)
        {
            var result = new ValidationResult();
            if (organ == null)
            {
                result.Error("organ is null");
                return result;
            }

            var label = string.IsNullOrWhiteSpace(organ.OrganId) ? "<no organId>" : organ.OrganId;

            if (organ.SchemaVersion != CurrentSchemaVersion)
            {
                result.Error($"organ '{label}': schemaVersion {organ.SchemaVersion} != {CurrentSchemaVersion}");
            }

            if (string.IsNullOrWhiteSpace(organ.OrganId))
            {
                result.Error("organ: organId is empty");
            }
            else if (!_organIdPattern.IsMatch(organ.OrganId))
            {
                result.Error($"organ '{label}': organId must be lowercase snake_case (^[a-z0-9_]+$)");
            }

            if (string.IsNullOrWhiteSpace(organ.DisplayName))
            {
                result.Error($"organ '{label}': displayName is empty");
            }

            if (string.IsNullOrWhiteSpace(organ.Description))
            {
                result.Error($"organ '{label}': description is empty");
            }

            // meshId: required for anatomical nodes; optional for physiological states (Phase 1 Part A.1).
            if (organ.NodeType == NodeType.Anatomical && string.IsNullOrWhiteSpace(organ.MeshId))
            {
                result.Error($"organ '{label}': anatomical node requires a meshId");
            }

            if (knownOrganIds != null)
            {
                var ids = AsSet(knownOrganIds);

                if (!string.IsNullOrWhiteSpace(organ.ParentOrganId) && !ids.Contains(organ.ParentOrganId))
                {
                    result.Warn(
                        $"organ '{label}': parentOrganId '{organ.ParentOrganId}' not in organ data " +
                        "(node kept; hierarchy link dropped)");
                }

                if (organ.Connections != null)
                {
                    foreach (var connection in organ.Connections)
                    {
                        if (string.IsNullOrWhiteSpace(connection?.ToOrganId) || !ids.Contains(connection.ToOrganId))
                        {
                            result.Warn(
                                $"organ '{label}': connection toOrganId '{connection?.ToOrganId}' not in " +
                                "organ data (edge dropped)");
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Validates one disease against the §9 rules. <paramref name="knownOrganIds"/> is the set of
        /// valid organ IDs (for entry/target/connection-line existence). <paramref name="existingDiseaseIds"/>
        /// is the set of disease IDs already accepted, used for the uniqueness rule.
        /// </summary>
        public static ValidationResult ValidateDisease(
            DiseaseAsset disease,
            IReadOnlyCollection<string> knownOrganIds,
            IReadOnlyCollection<string> existingDiseaseIds = null)
        {
            var result = new ValidationResult();
            if (disease == null)
            {
                result.Error("disease is null");
                return result;
            }

            var organIds = AsSet(knownOrganIds);
            var diseaseIds = AsSet(existingDiseaseIds);
            var label = string.IsNullOrWhiteSpace(disease.DiseaseId) ? "<no diseaseId>" : disease.DiseaseId;

            if (disease.SchemaVersion != CurrentSchemaVersion)
            {
                result.Error($"disease '{label}': schemaVersion {disease.SchemaVersion} != {CurrentSchemaVersion}");
            }

            if (string.IsNullOrWhiteSpace(disease.DiseaseId))
            {
                result.Error("disease: diseaseId is empty");
            }
            else if (diseaseIds.Contains(disease.DiseaseId))
            {
                result.Error($"disease '{label}': diseaseId is not unique");
            }

            if (string.IsNullOrWhiteSpace(disease.EntryOrganId))
            {
                result.Error($"disease '{label}': entryOrganId is empty");
            }
            else if (!organIds.Contains(disease.EntryOrganId))
            {
                result.Error($"disease '{label}': entryOrganId '{disease.EntryOrganId}' not in organ data");
            }

            var stages = disease.Stages ?? new List<DiseaseStage>();
            var cascade = disease.Cascade ?? new List<CascadeStep>();

            if (stages.Count == 0)
            {
                result.Error($"disease '{label}': has no stages");
            }

            if (cascade.Count == 0)
            {
                result.Error($"disease '{label}': cascade has no steps");
            }

            var stageIds = new HashSet<string>(stages.Where(s => s != null).Select(s => s.StageId));

            // How many stages list each step index (must be exactly one per §9).
            var indexToStageCount = new Dictionary<int, int>();
            foreach (var stage in stages.Where(s => s?.StepIndices != null))
            {
                foreach (var idx in stage.StepIndices)
                {
                    indexToStageCount.TryGetValue(idx, out var count);
                    indexToStageCount[idx] = count + 1;
                }
            }

            // Per-step rules.
            foreach (var step in cascade)
            {
                if (step == null)
                {
                    result.Error($"disease '{label}': contains a null cascade step");
                    continue;
                }

                var stepLabel = $"disease '{label}' step {step.StepIndex}";

                if (string.IsNullOrWhiteSpace(step.Stage) || !stageIds.Contains(step.Stage))
                {
                    result.Error($"{stepLabel}: stage '{step.Stage}' is not a defined stageId");
                }

                if (string.IsNullOrWhiteSpace(step.TargetOrganId) || !organIds.Contains(step.TargetOrganId))
                {
                    result.Error($"{stepLabel}: targetOrganId '{step.TargetOrganId}' not in organ data");
                }

                if (step.Severity < 0 || step.Severity > 100)
                {
                    result.Error($"{stepLabel}: severity {step.Severity} out of range 0-100");
                }

                if (step.DelayMs < 0)
                {
                    result.Error($"{stepLabel}: delayMs {step.DelayMs} must be >= 0");
                }

                if (step.DurationMs <= 0)
                {
                    result.Error($"{stepLabel}: durationMs {step.DurationMs} must be > 0");
                }

                if (string.IsNullOrWhiteSpace(step.NarrationFallback))
                {
                    result.Error($"{stepLabel}: narrationFallback is empty (dual-track rule)");
                }

                ValidateEffect(step.Effect, stepLabel, organIds, result);

                indexToStageCount.TryGetValue(step.StepIndex, out var stageCount);
                if (stageCount == 0)
                {
                    result.Error($"{stepLabel}: stepIndex not present in any stage's stepIndices");
                }
                else if (stageCount > 1)
                {
                    result.Error($"{stepLabel}: stepIndex appears in {stageCount} stages; must be exactly one");
                }
            }

            // Two cascade steps sharing a stepIndex.
            foreach (var group in cascade.Where(s => s != null).GroupBy(s => s.StepIndex).Where(g => g.Count() > 1))
            {
                result.Error($"disease '{label}': duplicate stepIndex {group.Key} ({group.Count()} steps share it)");
            }

            return result;
        }

        private static void ValidateEffect(
            VisualEffect effect,
            string stepLabel,
            ISet<string> organIds,
            ValidationResult result)
        {
            if (effect == null)
            {
                // A cascade step with no visual effect plays nothing but is not structurally invalid.
                result.Warn($"{stepLabel}: no visualEffect (step will animate nothing)");
                return;
            }

            switch (effect.Type)
            {
                case VisualEffectType.ConnectionLine:
                    if (string.IsNullOrWhiteSpace(effect.FromOrganId) || !organIds.Contains(effect.FromOrganId))
                    {
                        result.Error($"{stepLabel}: connection_line fromOrganId '{effect.FromOrganId}' not in organ data");
                    }

                    if (string.IsNullOrWhiteSpace(effect.ToOrganId) || !organIds.Contains(effect.ToOrganId))
                    {
                        result.Error($"{stepLabel}: connection_line toOrganId '{effect.ToOrganId}' not in organ data");
                    }

                    break;

                case VisualEffectType.ColorChange:
                    if (!IsValidHex(effect.FromColor))
                    {
                        result.Error($"{stepLabel}: color_change fromColor '{effect.FromColor}' is not a valid hex color");
                    }

                    if (!IsValidHex(effect.ToColor))
                    {
                        result.Error($"{stepLabel}: color_change toColor '{effect.ToColor}' is not a valid hex color");
                    }

                    break;
            }
        }

        private static bool IsValidHex(string value)
            => !string.IsNullOrWhiteSpace(value) && _hexColorPattern.IsMatch(value);

        private static ISet<string> AsSet(IReadOnlyCollection<string> ids)
            => ids as ISet<string> ?? new HashSet<string>(ids ?? System.Array.Empty<string>());
    }
}
