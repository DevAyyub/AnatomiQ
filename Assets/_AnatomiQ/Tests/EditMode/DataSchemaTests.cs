using AnatomiQ.Data;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// Section 7 gate. Verifies the data schemas instantiate with their documented defaults —
    /// in particular the schema version and the v1.1 OrganAsset additions (NodeType, FmaId).
    /// Pure Edit Mode; no scene or device build.
    /// </summary>
    public sealed class DataSchemaTests
    {
        [Test]
        public void OrganAsset_HasExpectedDefaults()
        {
            var organ = ScriptableObject.CreateInstance<OrganAsset>();
            try
            {
                Assert.AreEqual(1, organ.SchemaVersion, "OrganAsset.SchemaVersion should default to 1.");
                Assert.AreEqual(NodeType.Anatomical, organ.NodeType,
                    "OrganAsset.NodeType should default to Anatomical (v1.1 backward-compat).");
                Assert.IsNull(organ.FmaId, "OrganAsset.FmaId should default to null.");
            }
            finally
            {
                Object.DestroyImmediate(organ);
            }
        }

        [Test]
        public void DiseaseAsset_HasExpectedDefaults()
        {
            var disease = ScriptableObject.CreateInstance<DiseaseAsset>();
            try
            {
                Assert.AreEqual(1, disease.SchemaVersion, "DiseaseAsset.SchemaVersion should default to 1.");
            }
            finally
            {
                Object.DestroyImmediate(disease);
            }
        }
    }
}
