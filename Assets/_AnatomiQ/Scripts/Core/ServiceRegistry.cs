using AnatomiQ.Data;
using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Central, asset-based access point for cross-cutting services (Build Environment Part D).
    /// Every feature holds a <c>[SerializeField] ServiceRegistry _services</c> and reaches shared
    /// systems through it as interfaces — never via singletons, FindObjectOfType, or direct scene
    /// references.
    ///
    /// Because a ScriptableObject asset cannot persist serialized references to scene MonoBehaviours,
    /// services REGISTER THEMSELVES AT RUNTIME from Awake; AppBootstrap (Section 6) guarantees the
    /// FallbackManager registers first. The registry holds interface references only.
    ///
    /// Note on IDataLayer: it lives in the Data assembly and does NOT extend <see cref="IService"/>,
    /// because Data must not reference Core (Core already references Data → that would be a cycle).
    /// It is therefore registered via the dedicated <see cref="RegisterDataLayer"/> entry point
    /// rather than the generic <see cref="Register"/> path.
    /// </summary>
    [CreateAssetMenu(fileName = "ServiceRegistry", menuName = "AnatomiQ/Core/Service Registry")]
    public sealed class ServiceRegistry : ScriptableObject
    {
        [System.NonSerialized] private IFallbackManager _fallbackManager;
        [System.NonSerialized] private IAIOrchestrator _aiOrchestrator;
        [System.NonSerialized] private IBodyModelRenderer _bodyRenderer;
        [System.NonSerialized] private IDataLayer _dataLayer;

        /// <summary>CORE-007. Null until the FallbackManager registers (first, at startup).</summary>
        public IFallbackManager FallbackManager => _fallbackManager;

        /// <summary>CORE-006. Null until the AI Orchestrator registers.</summary>
        public IAIOrchestrator AIOrchestrator => _aiOrchestrator;

        /// <summary>CORE-002. Null until the Body Model Renderer registers.</summary>
        public IBodyModelRenderer BodyRenderer => _bodyRenderer;

        /// <summary>CORE-008. Null until the Data Layer registers.</summary>
        public IDataLayer DataLayer => _dataLayer;

        /// <summary>
        /// Registers a Core service under whichever known interface(s) it implements.
        /// Called by each service from Awake. Last-write-wins, with a warning on replacement.
        /// </summary>
        public void Register(IService service)
        {
            if (service == null)
            {
                Debug.LogError("[ServiceRegistry] Attempted to register a null service.");
                return;
            }

            switch (service)
            {
                case IFallbackManager fallback:
                    WarnIfReplacing(_fallbackManager, nameof(IFallbackManager));
                    _fallbackManager = fallback;
                    break;
                case IAIOrchestrator ai:
                    WarnIfReplacing(_aiOrchestrator, nameof(IAIOrchestrator));
                    _aiOrchestrator = ai;
                    break;
                case IBodyModelRenderer body:
                    WarnIfReplacing(_bodyRenderer, nameof(IBodyModelRenderer));
                    _bodyRenderer = body;
                    break;
                default:
                    Debug.LogWarning(
                        $"[ServiceRegistry] '{service.GetType().Name}' implements no known " +
                        "service interface; nothing registered.");
                    break;
            }
        }

        /// <summary>
        /// Registers the Data Layer (CORE-008). Separate from <see cref="Register"/> because
        /// <see cref="IDataLayer"/> deliberately does not extend <see cref="IService"/> (cycle avoidance).
        /// </summary>
        public void RegisterDataLayer(IDataLayer dataLayer)
        {
            if (dataLayer == null)
            {
                Debug.LogError("[ServiceRegistry] Attempted to register a null data layer.");
                return;
            }

            WarnIfReplacing(_dataLayer, nameof(IDataLayer));
            _dataLayer = dataLayer;
        }

        /// <summary>Clears all registered services so stale scene refs never leak between play sessions.</summary>
        public void Clear()
        {
            _fallbackManager = null;
            _aiOrchestrator = null;
            _bodyRenderer = null;
            _dataLayer = null;
        }

        private static void WarnIfReplacing(object existing, string interfaceName)
        {
            if (existing != null)
            {
                Debug.LogWarning(
                    $"[ServiceRegistry] Replacing an already-registered {interfaceName}. " +
                    "Expected exactly one instance per service.");
            }
        }

        private void OnEnable() => Clear();
    }
}
