using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AnatomiQ.Core;
using AnatomiQ.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnatomiQ.Tests.PlayMode
{
    /// <summary>
    /// CORE-008 chunk 3 gate. Verifies the <see cref="DataLayer"/> service self-registers with a
    /// <see cref="ServiceRegistry"/>, loads + validates content from a <see cref="ContentManifest"/>,
    /// and resolves known ids — mirroring the FallbackManager self-registration pattern.
    ///
    /// The manifest is populated by parsing the minimal fixtures through <see cref="ContentImporter"/>,
    /// so this exercises the real importer → validator → lookup path. Private serialized fields are
    /// injected via reflection on an inactive GameObject, then Awake runs synchronously on activation.
    /// Runs in the editor's Play Mode (fixtures are read from the Assets folder).
    /// </summary>
    public sealed class DataLayerTests
    {
        private const string FixturesDir = "_AnatomiQ/Tests/EditMode/Fixtures";

        private GameObject _go;
        private ServiceRegistry _registry;
        private ContentManifest _manifest;
        private readonly List<Object> _spawned = new List<Object>();

        private static string ReadFixture(string fileName)
            => File.ReadAllText(Path.Combine(Application.dataPath, FixturesDir, fileName));

        private static void SetPrivate(object target, string field, object value)
            => target.GetType()
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<ServiceRegistry>();
            _manifest = ScriptableObject.CreateInstance<ContentManifest>();

            var organs = ContentImporter.ParseOrgans(ReadFixture("minimal_organ_graph.json"), out _);
            var disease = ContentImporter.ParseDisease(ReadFixture("minimal_disease.json"), out _);

            _manifest.Organs = organs;
            _manifest.Diseases = new List<DiseaseAsset> { disease };

            _spawned.AddRange(organs);
            _spawned.Add(disease);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
                _go = null;
            }

            foreach (var obj in _spawned)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _spawned.Clear();

            if (_manifest != null)
            {
                Object.DestroyImmediate(_manifest);
            }

            if (_registry != null)
            {
                Object.DestroyImmediate(_registry);
            }
        }

        private DataLayer SpawnDataLayer()
        {
            _go = new GameObject("DataLayer");
            _go.SetActive(false);                      // delay Awake until fields are injected
            var dataLayer = _go.AddComponent<DataLayer>();
            SetPrivate(dataLayer, "_services", _registry);
            SetPrivate(dataLayer, "_manifest", _manifest);
            _go.SetActive(true);                       // triggers Awake -> register + load
            return dataLayer;
        }

        [Test]
        public void Awake_RegistersItselfAsDataLayer()
        {
            var dataLayer = SpawnDataLayer();
            Assert.IsNotNull(_registry.DataLayer, "DataLayer should register via RegisterDataLayer.");
            Assert.AreSame(dataLayer, _registry.DataLayer);
        }

        [Test]
        public void Load_BuildsLookupsFromManifest()
        {
            var dataLayer = SpawnDataLayer();
            Assert.IsTrue(dataLayer.IsLoaded);
            Assert.AreEqual(3, dataLayer.Organs.Count);
            Assert.AreEqual(1, dataLayer.Diseases.Count);
        }

        [Test]
        public void TryGet_ResolvesKnownIdsAndRejectsUnknown()
        {
            var dataLayer = SpawnDataLayer();

            Assert.IsTrue(dataLayer.TryGetOrgan("test_node_a", out var organ));
            Assert.AreEqual("Test Node A", organ.DisplayName);

            Assert.IsTrue(dataLayer.TryGetDisease("test_disease", out var disease));
            Assert.AreEqual("Test Disease", disease.DisplayName);

            Assert.IsFalse(dataLayer.TryGetOrgan("does_not_exist", out _));
            Assert.IsNull(dataLayer.GetDisease("does_not_exist"));
        }

        [Test]
        public void Load_SkipsInvalidOrgan_KeepsValidOnes()
        {
            // An anatomical node with no meshId is invalid (§ organ rules) and must be skipped,
            // while the rest of the manifest still loads.
            var bad = ScriptableObject.CreateInstance<OrganAsset>();
            bad.OrganId = "bad_node";
            bad.DisplayName = "Bad";
            bad.Description = "Missing mesh on an anatomical node.";
            bad.NodeType = NodeType.Anatomical;
            bad.MeshId = null;
            _manifest.Organs.Add(bad);
            _spawned.Add(bad);

            // The skip path logs an error; tell the test runner that error is expected.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Skipping organ 'bad_node'"));

            var dataLayer = SpawnDataLayer();

            Assert.AreEqual(3, dataLayer.Organs.Count, "Only the three valid nodes should load.");
            Assert.IsFalse(dataLayer.TryGetOrgan("bad_node", out _));
        }
    }
}
