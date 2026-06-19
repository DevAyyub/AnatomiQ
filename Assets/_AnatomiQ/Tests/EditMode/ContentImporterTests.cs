using System.IO;
using AnatomiQ.Data;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// CORE-008 chunk 1 gate. Verifies <see cref="ContentImporter"/> round-trips the Phase 1 test
    /// fixtures into ScriptableObjects with correct scalar, list, and (snake_case) enum mapping,
    /// including the three JSON→C# name bridges (anatomicalRegion→Region, icd10→Icd10Code,
    /// visualEffect→Effect) and the BodySystem.Vascular→Cardiovascular rename. Also checks that a
    /// malformed item is skipped and reported rather than throwing.
    ///
    /// This is parsing only; the Section 9 validation rules are exercised by the validator tests in
    /// the next chunk. Pure Edit Mode; no scene or device build.
    /// </summary>
    public sealed class ContentImporterTests
    {
        private const string FixturesDir = "_AnatomiQ/Tests/EditMode/Fixtures";

        private static string ReadFixture(string fileName)
            => File.ReadAllText(Path.Combine(Application.dataPath, FixturesDir, fileName));

        private static void Destroy(Object obj)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        // ---- Disease ----------------------------------------------------------------------------

        [Test]
        public void ParseDisease_MinimalFixture_MapsScalarsAndEnums()
        {
            var disease = ContentImporter.ParseDisease(ReadFixture("minimal_disease.json"), out var error);
            try
            {
                Assert.IsNull(error, "Expected a clean parse.");
                Assert.IsNotNull(disease);
                Assert.AreEqual(1, disease.SchemaVersion);
                Assert.AreEqual("test_disease", disease.DiseaseId);
                Assert.AreEqual("Test Disease", disease.DisplayName);
                Assert.AreEqual(DiseaseCategory.Metabolic, disease.Category);
                Assert.AreEqual("TEST", disease.Icd10Code, "icd10 JSON key must bind to Icd10Code.");
                Assert.AreEqual("test_node_a", disease.EntryOrganId);
                Assert.IsNotNull(disease.Metadata);
                Assert.AreEqual(9000, disease.Metadata.TotalDurationMs);
                Assert.AreEqual(3, disease.Metadata.StepCount);
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ParseDisease_MinimalFixture_MapsStagesAndCascade()
        {
            var disease = ContentImporter.ParseDisease(ReadFixture("minimal_disease.json"), out var error);
            try
            {
                Assert.IsNull(error);
                Assert.IsNotNull(disease);

                Assert.AreEqual(1, disease.Stages.Count);
                CollectionAssert.AreEqual(new[] { 0, 1, 2 }, disease.Stages[0].StepIndices);

                Assert.AreEqual(3, disease.Cascade.Count);

                var first = disease.Cascade[0];
                Assert.AreEqual(0, first.StepIndex);
                Assert.AreEqual("only", first.Stage);
                Assert.AreEqual("test_node_a", first.TargetOrganId);
                Assert.AreEqual(30, first.Severity);
                Assert.AreEqual(0, first.DelayMs);
                Assert.AreEqual(3000, first.DurationMs);

                Assert.IsNotNull(first.Effect, "visualEffect JSON key must bind to Effect.");
                Assert.AreEqual(VisualEffectType.HighlightPulse, first.Effect.Type);
                Assert.AreEqual("#FF0000", first.Effect.Color);
                Assert.AreEqual(0.5f, first.Effect.Intensity, 0.0001f);

                Assert.IsNotNull(first.AiContext);
                Assert.AreEqual(AudienceLevel.General, first.AiContext.Audience);

                Assert.AreEqual(6000, disease.Cascade[2].DelayMs, "Timing should round-trip per step.");
            }
            finally
            {
                Destroy(disease);
            }
        }

        // ---- Organs (collection / envelope) -----------------------------------------------------

        [Test]
        public void ParseOrgans_MinimalGraph_ReturnsThreeNodesInOrder()
        {
            var organs = ContentImporter.ParseOrgans(ReadFixture("minimal_organ_graph.json"), out var errors);
            try
            {
                Assert.IsEmpty(errors, "Expected a clean parse of all three nodes.");
                Assert.AreEqual(3, organs.Count);
                Assert.AreEqual("test_node_a", organs[0].OrganId);
                Assert.AreEqual("test_node_b", organs[1].OrganId);
                Assert.AreEqual("test_node_c", organs[2].OrganId);

                var a = organs[0];
                Assert.AreEqual(NodeType.Anatomical, a.NodeType);
                Assert.AreEqual("FMA_TEST_A", a.FmaId);
                Assert.AreEqual(BodySystem.Endocrine, a.System);
                Assert.AreEqual(AnatomyLayer.Organs, a.Layer);
                Assert.AreEqual(AnatomicalRegion.Systemic, a.Region,
                    "anatomicalRegion JSON key must bind to Region.");
                Assert.AreEqual(1, a.Connections.Count);
                Assert.AreEqual("test_node_b", a.Connections[0].ToOrganId);
                Assert.AreEqual(ConnectionType.Regulates, a.Connections[0].Type);

                Assert.AreEqual(ConnectionType.Signals, organs[1].Connections[0].Type);
                Assert.IsEmpty(organs[2].Connections, "Terminal node has no outgoing edges.");
            }
            finally
            {
                foreach (var organ in organs)
                {
                    Destroy(organ);
                }
            }
        }

        // ---- The headline fix: physiological_state + cardiovascular -----------------------------

        [Test]
        public void ParseOrgan_PhysiologicalStateCardiovascular_MapsRenamedEnumAndAllowsNullMesh()
        {
            // Mirrors blood_pressure_systemic from the Phase 1 organ list (lands as content in a
            // later chunk); proven here so the rename + null-mesh handling are gated at chunk 1.
            const string json = @"{
              ""organId"": ""bp_test"",
              ""displayName"": ""Systemic Blood Pressure"",
              ""nodeType"": ""physiological_state"",
              ""fmaId"": null,
              ""parentOrganId"": null,
              ""system"": ""cardiovascular"",
              ""meshId"": null,
              ""description"": ""Pressure exerted by circulating blood on arterial walls."",
              ""layer"": ""vascular"",
              ""anatomicalRegion"": ""systemic"",
              ""connections"": []
            }";

            var organ = ContentImporter.ParseOrgan(json, out var error);
            try
            {
                Assert.IsNull(error);
                Assert.IsNotNull(organ);
                Assert.AreEqual(NodeType.PhysiologicalState, organ.NodeType);
                Assert.AreEqual(BodySystem.Cardiovascular, organ.System,
                    "system 'cardiovascular' must map to the renamed BodySystem.Cardiovascular.");
                Assert.AreEqual(AnatomyLayer.Vascular, organ.Layer,
                    "the layer axis still has Vascular and is independent of the system axis.");
                Assert.IsNull(organ.MeshId, "physiological_state allows a null meshId.");
                Assert.IsNull(organ.ParentOrganId, "physiological_state allows a null parentOrganId.");
            }
            finally
            {
                Destroy(organ);
            }
        }

        // ---- Skip, don't crash ------------------------------------------------------------------

        [Test]
        public void ParseDisease_MalformedJson_ReturnsNullWithError()
        {
            var disease = ContentImporter.ParseDisease("{ this is not valid json", out var error);
            try
            {
                Assert.IsNull(disease);
                Assert.IsNotNull(error, "A malformed document must report an error rather than throw.");
            }
            finally
            {
                Destroy(disease);
            }
        }

        [Test]
        public void ParseOrgans_MixedValidAndUnknownEnum_SkipsBadKeepsGood()
        {
            const string json = @"{ ""organs"": [
              { ""organId"":""good"", ""displayName"":""Good"", ""system"":""endocrine"",
                ""meshId"":""m"", ""description"":""d"", ""layer"":""organs"",
                ""anatomicalRegion"":""systemic"", ""connections"":[] },
              { ""organId"":""bad"", ""displayName"":""Bad"", ""system"":""not_a_real_system"",
                ""meshId"":""m"", ""description"":""d"", ""layer"":""organs"",
                ""anatomicalRegion"":""systemic"", ""connections"":[] }
            ] }";

            var organs = ContentImporter.ParseOrgans(json, out var errors);
            try
            {
                Assert.AreEqual(1, organs.Count, "The valid organ must still load.");
                Assert.AreEqual("good", organs[0].OrganId);
                Assert.AreEqual(1, errors.Count, "The bad organ must be reported and skipped, not crash.");
            }
            finally
            {
                foreach (var organ in organs)
                {
                    Destroy(organ);
                }
            }
        }
    }
}
