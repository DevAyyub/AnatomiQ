using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Contract for CORE-002 (3D Body Model Renderer). Minimal at this build step: it exposes the
    /// model's clean root transform (so a later AR-placement feature — ATLAS-003 — can anchor or
    /// reparent the body WITHOUT the AR pillar referencing the Anatomy pillar) and a readiness flag
    /// that distinguishes the real model from the placeholder fallback.
    ///
    /// Deliberately does NOT yet expose layer visibility (CORE-003) or organ show/hide/recolor
    /// (CORE-004). Those members are added when their consuming features are built, so the renderer
    /// never grows an API surface with no caller (decision A, CORE-002 build plan).
    /// </summary>
    public interface IBodyModelRenderer : IService
    {
        /// <summary>
        /// The root transform that owns the rendered body hierarchy. Stable and reparentable: a later
        /// AR-placement feature anchors THIS transform rather than the individual meshes, so baked
        /// model offsets don't fight world placement. Never null once the service has initialized
        /// (it points at the placeholder root if the real model failed to load).
        /// </summary>
        Transform ModelRoot { get; }

        /// <summary>
        /// True when the real anatomical model is present and renderable; false when the load guard
        /// fell back to the placeholder (CORE-002 fallback rule). Consumers that need the full model
        /// (organ selection, cascades) should check this before acting on specific organs.
        /// </summary>
        bool IsModelReady { get; }
    }
}
