using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnatomiQ.Data
{
    /// <summary>
    /// CORE-008 runtime schema for an anatomical node (Data Schemas §2). An "organ" is any
    /// anatomically meaningful node — a whole organ, a sub-structure, or (via <see cref="NodeType"/>)
    /// a measurable physiological state. The graph of organs and their <see cref="OrganConnection"/>s
    /// is the substrate the Interconnectivity Engine (CORE-005) traverses.
    ///
    /// Authoring source of truth is JSON in Content/organs/; these ScriptableObjects are imported at
    /// build time. Field names and order mirror the JSON for a 1:1 importer mapping. Public fields
    /// (not private+property) are intentional here so the inspector and JSON importer can bind them.
    /// </summary>
    [CreateAssetMenu(fileName = "Organ", menuName = "AnatomiQ/Organ")]
    public class OrganAsset : ScriptableObject
    {
        /// <summary>Schema version. Always 1 for the current schema; increment on breaking changes.</summary>
        public int SchemaVersion = 1;

        /// <summary>Unique, lowercase snake_case identifier (e.g. "pancreas_beta_cells").</summary>
        public string OrganId;

        /// <summary>Human-readable name shown in UI.</summary>
        public string DisplayName;

        /// <summary>
        /// v1.1: whether this node is an anatomical structure or a measurable physiological state
        /// (blood glucose, blood pressure, etc.). Defaults to <see cref="NodeType.Anatomical"/> so
        /// existing organ entries remain valid. For physiological states, MeshId/ParentOrganId are
        /// optional and Region is typically Systemic.
        /// </summary>
        public NodeType NodeType = NodeType.Anatomical;

        /// <summary>
        /// v1.1 (optional): Foundational Model of Anatomy ID where available (e.g. "FMA62630").
        /// Anatomical organs have FMA IDs; physiological states do not (left null).
        /// </summary>
        public string FmaId;

        /// <summary>ID of the parent node if this is a sub-structure; null if top-level.</summary>
        public string ParentOrganId;

        /// <summary>The body system this node belongs to.</summary>
        public BodySystem System;

        /// <summary>Maps to a mesh component in the 3D body model (CORE-002 resolves it).</summary>
        public string MeshId;

        /// <summary>1–3 sentence anatomical description.</summary>
        [TextArea(2, 4)] public string Description;

        /// <summary>Anatomical layer driving the layer toggle (CORE-003).</summary>
        public AnatomyLayer Layer;

        /// <summary>Coarse body region for spatial queries.</summary>
        public AnatomicalRegion Region;

        /// <summary>Outgoing edges to other nodes — the graph structure CORE-005 walks.</summary>
        public List<OrganConnection> Connections;

        /// <summary>Provenance and review tracking.</summary>
        public OrganMetadata Metadata;
    }

    /// <summary>A directed edge from this organ to another, with its physiological meaning.</summary>
    [Serializable]
    public class OrganConnection
    {
        /// <summary>The destination organ's ID. Must exist in organ data.</summary>
        public string ToOrganId;

        /// <summary>The nature of the relationship.</summary>
        public ConnectionType Type;

        /// <summary>Canonical key naming the mechanism (e.g. "insulin_secretion").</summary>
        public string Mechanism;

        /// <summary>Short human-readable description of the connection.</summary>
        [TextArea(1, 3)] public string Description;
    }

    /// <summary>Provenance metadata for an organ node (mirrors the JSON "metadata" object).</summary>
    [Serializable]
    public class OrganMetadata
    {
        /// <summary>Citation strings for the anatomical/physiological claims.</summary>
        public List<string> Sources;

        /// <summary>ISO date of last review, or null if unreviewed.</summary>
        public string LastReviewed;
    }

    /// <summary>The twelve body systems an organ node can belong to (Data Schemas §2.3).</summary>
    public enum BodySystem
    {
        Skeletal, Muscular, Vascular, Nervous, Lymphatic, Endocrine,
        Digestive, Respiratory, Urinary, Reproductive, Integumentary, Sensory
    }

    /// <summary>Anatomical layers for the layer toggle system (CORE-003).</summary>
    public enum AnatomyLayer
    {
        Skeletal, Muscular, Vascular, Nervous, Lymphatic, Organs
    }

    /// <summary>Coarse body regions for spatial queries (Data Schemas §2.5).</summary>
    public enum AnatomicalRegion
    {
        Head, Neck, ChestAnterior, ChestPosterior, AbdomenUpper, AbdomenLower,
        Pelvis, ArmLeft, ArmRight, LegLeft, LegRight, Systemic
    }

    /// <summary>The kinds of edges between organ nodes (Data Schemas §2.4).</summary>
    public enum ConnectionType
    {
        Regulates, Signals, SuppliesBlood, DrainsBlood, Innervates,
        MechanicallySupports, Produces, Metabolizes, Filters, Contains
    }

    /// <summary>v1.1: distinguishes anatomical structures from measurable physiological states.</summary>
    public enum NodeType
    {
        Anatomical,
        PhysiologicalState
    }
}
