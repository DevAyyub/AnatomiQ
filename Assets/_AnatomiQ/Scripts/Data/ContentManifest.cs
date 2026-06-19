using System.Collections.Generic;
using UnityEngine;

namespace AnatomiQ.Data
{
    /// <summary>
    /// CORE-008 runtime content index. Per the load-source decision (build-time import → .asset +
    /// manifest, not runtime JSON parsing), the Editor import tool writes the validated organ and
    /// disease <c>.asset</c> files and lists them here. The DataLayer references one manifest and
    /// loads its content from these lists, so it never touches the filesystem at runtime.
    ///
    /// Plain serialized lists keep the manifest inspector-visible and easy to swap for Addressables
    /// later. The lists are the authoring/import surface; the DataLayer re-validates their contents
    /// at load and skips anything malformed.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentManifest", menuName = "AnatomiQ/Content Manifest")]
    public sealed class ContentManifest : ScriptableObject
    {
        /// <summary>All imported organ assets to load.</summary>
        public List<OrganAsset> Organs = new List<OrganAsset>();

        /// <summary>All imported disease assets to load.</summary>
        public List<DiseaseAsset> Diseases = new List<DiseaseAsset>();
    }
}
