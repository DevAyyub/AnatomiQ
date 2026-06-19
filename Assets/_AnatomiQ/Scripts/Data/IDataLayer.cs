using System.Collections.Generic;

namespace AnatomiQ.Data
{
    /// <summary>
    /// Contract for CORE-008 (Data Layer). Lives in the Data assembly and intentionally does
    /// NOT depend on AnatomiQ.Core: Core already references Data (the ServiceRegistry exposes
    /// IDataLayer), so a back-reference from Data to Core would create a circular assembly
    /// dependency, which Unity forbids. The registry therefore registers the data layer through
    /// its own typed entry point (<c>RegisterDataLayer</c>) rather than the generic IService path.
    ///
    /// The Data Layer is a read-only content store: it loads validated organ and disease assets and
    /// exposes id-based lookups. Graph traversal over <see cref="OrganAsset.Connections"/> is NOT
    /// here — that is the Interconnectivity Engine (CORE-005), which consumes <see cref="Organs"/>
    /// and resolves edge targets via <see cref="TryGetOrgan"/>.
    /// </summary>
    public interface IDataLayer
    {
        /// <summary>True once the initial load pass has completed (even if it loaded zero content).</summary>
        bool IsLoaded { get; }

        /// <summary>All successfully loaded organ assets. Read-only; populated once at load.</summary>
        IReadOnlyCollection<OrganAsset> Organs { get; }

        /// <summary>All successfully loaded disease assets. Read-only; populated once at load.</summary>
        IReadOnlyCollection<DiseaseAsset> Diseases { get; }

        /// <summary>Looks up an organ by id. Returns false (and null) if absent.</summary>
        bool TryGetOrgan(string organId, out OrganAsset organ);

        /// <summary>Looks up a disease by id. Returns false (and null) if absent.</summary>
        bool TryGetDisease(string diseaseId, out DiseaseAsset disease);

        /// <summary>Returns the organ with the given id, or null if absent.</summary>
        OrganAsset GetOrgan(string organId);

        /// <summary>Returns the disease with the given id, or null if absent.</summary>
        DiseaseAsset GetDisease(string diseaseId);
    }
}
