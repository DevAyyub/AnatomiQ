using System.Collections.Generic;
using System.IO;
using AnatomiQ.Data;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// CORE-008 chunk 2 gate. Exercises <see cref="ContentValidator"/> against the Section 9 disease
    /// rules and the organ rules (§2.3 + the physiological-state relaxations). Covers the eight
    /// DataValidationTests from Phase 1 Medical Content Part E.3, plus the remaining §9 rules.
    ///
    /// Per the gap-fill fixtures decision, these run against the minimal fixtures + targeted
    /// mutations rather than the pre-review T2D/Phase 1 content, so validation coverage does not
    /// couple to medical content that may still change. Pure Edit Mode; no scene or device build.
    /// </summary>
    public sealed class ContentValidatorTests
    {
        private const string FixturesDir = "_AnatomiQ/Tests/EditMode/Fixtures";

        // The three organ IDs defined by minimal_organ_graph.json.
        private static readonly string[] GraphIds = { "test_node_a", "test_node_b", "test_node_c" };

        private static string ReadFixture(string fileName)
            => File.ReadAllText(Path.Combine(Application.dataPath, FixturesDir, fileName));

        private static DiseaseAsset NewDisease()
            => ContentImporter.ParseDisease(ReadFixture("minimal_disease.json"), out _);

        private static List<OrganAsset> NewGraphOrgans()
            => ContentImporter.ParseOrgans(ReadFixture("minimal_organ_graph.json"), out _);

        private static void Destroy(Object obj)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        private static void DestroyAll(IEnumerable<OrganAsset> organs)
        {
            foreach (var organ in organs)
            {
                Destroy(organ);
            }
        }

        // ---- Disease: the passing cases ---------------------------------------------------------

        [Test]
        public void ValidateDisease_AllTargetOrgansExist_Passes()
        {
            var disease = NewDisease();
            try
            {
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsTrue(result.IsValid, string.Join("; ", result.Errors));
                Assert.IsEmpty(result.Errors);
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_StepIndicesInExactlyOneStage_Passes()
        {
            var disease = NewDisease();
            try
            {
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsTrue(result.IsValid);
                Assert.IsFalse(
                    result.Errors.Exists(e => e.Contains("stepIndex") || e.Contains("stage")),
                    "The single stage partitions all three steps exactly once.");
            }
            finally
            {
                Destroy(disease);
            }
        }

        // ---- Disease: the failing cases ---------------------------------------------------------

        [Test]
        public void ValidateDisease_MissingTargetOrgan_Fails()
        {
            var disease = NewDisease();
            try
            {
                var result = ContentValidator.ValidateDisease(disease, new[] { "test_node_a", "test_node_b" });
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("test_node_c")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_DuplicateStepIndex_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.Cascade[1].StepIndex = 0; // two steps now share index 0
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("duplicate stepIndex")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_NegativeDelay_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.Cascade[0].DelayMs = -1;
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("delayMs")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_EmptyNarrationFallback_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.Cascade[0].NarrationFallback = "";
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("narrationFallback")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_DuplicateDiseaseId_Fails()
        {
            var disease = NewDisease();
            try
            {
                var result = ContentValidator.ValidateDisease(
                    disease, GraphIds, existingDiseaseIds: new[] { "test_disease" });
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("unique")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_EntryOrganMissing_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.EntryOrganId = "ghost_organ";
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("entryOrganId")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_StageNotDefined_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.Cascade[0].Stage = "ghost_stage";
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("stage")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_DurationZero_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.Cascade[0].DurationMs = 0;
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("durationMs")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_SeverityOutOfRange_Fails()
        {
            var disease = NewDisease();
            try
            {
                disease.Cascade[0].Severity = 150;
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("severity")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_ColorChangeInvalidHex_Fails()
        {
            var disease = NewDisease();
            try
            {
                var effect = disease.Cascade[0].Effect;
                effect.Type = VisualEffectType.ColorChange;
                effect.FromColor = "nothex";
                effect.ToColor = "#FF0000";
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("hex")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ValidateDisease_ConnectionLineMissingEndpoint_Fails()
        {
            var disease = NewDisease();
            try
            {
                var effect = disease.Cascade[0].Effect;
                effect.Type = VisualEffectType.ConnectionLine;
                effect.FromOrganId = "test_node_a";
                effect.ToOrganId = "ghost_organ";
                var result = ContentValidator.ValidateDisease(disease, GraphIds);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("connection_line")));
            }
            finally
            {
                Destroy(disease);
            }
        }

        // ---- Organ rules ------------------------------------------------------------------------

        [Test]
        public void ValidateOrgan_Anatomical_WithMesh_Passes()
        {
            var organs = NewGraphOrgans();
            try
            {
                var result = ContentValidator.ValidateOrgan(organs[0]);
                Assert.IsTrue(result.IsValid, string.Join("; ", result.Errors));
            }
            finally
            {
                DestroyAll(organs);
            }
        }

        [Test]
        public void ValidateOrgan_Anatomical_RequiresMeshId()
        {
            var organs = NewGraphOrgans();
            try
            {
                organs[0].MeshId = null;
                var result = ContentValidator.ValidateOrgan(organs[0]);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("meshId")));
            }
            finally
            {
                DestroyAll(organs);
            }
        }

        [Test]
        public void ValidateOrgan_PhysiologicalState_AllowsNullMeshId()
        {
            const string json = @"{
              ""organId"": ""bp_test"",
              ""displayName"": ""Systemic Blood Pressure"",
              ""nodeType"": ""physiological_state"",
              ""parentOrganId"": null,
              ""system"": ""cardiovascular"",
              ""meshId"": null,
              ""description"": ""Pressure exerted by circulating blood on arterial walls."",
              ""layer"": ""vascular"",
              ""anatomicalRegion"": ""systemic"",
              ""connections"": []
            }";

            var organ = ContentImporter.ParseOrgan(json, out _);
            try
            {
                var result = ContentValidator.ValidateOrgan(organ);
                Assert.IsTrue(result.IsValid, string.Join("; ", result.Errors));
                Assert.IsFalse(result.Errors.Exists(e => e.Contains("meshId")),
                    "physiological_state must not require a meshId.");
            }
            finally
            {
                Destroy(organ);
            }
        }

        [Test]
        public void ValidateOrgan_BadOrganIdFormat_Fails()
        {
            var organs = NewGraphOrgans();
            try
            {
                organs[0].OrganId = "Bad-Id";
                var result = ContentValidator.ValidateOrgan(organs[0]);
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.Errors.Exists(e => e.Contains("snake_case")));
            }
            finally
            {
                DestroyAll(organs);
            }
        }

        [Test]
        public void ValidateOrgan_UnresolvedConnection_DropsEdgeWarnsNotErrors()
        {
            var organs = NewGraphOrgans();
            try
            {
                // node A connects to node B; validate against a set that omits B.
                var result = ContentValidator.ValidateOrgan(organs[0], new[] { "test_node_a" });
                Assert.IsTrue(result.IsValid, "An unresolved edge is a warning, not an error (organ stays usable).");
                Assert.IsNotEmpty(result.Warnings);
                Assert.IsTrue(result.Warnings.Exists(w => w.Contains("edge dropped")));
            }
            finally
            {
                DestroyAll(organs);
            }
        }
    }
}
