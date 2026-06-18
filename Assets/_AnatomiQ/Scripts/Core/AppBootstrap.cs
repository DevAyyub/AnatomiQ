using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Scene composition root and documented startup entry point. The FallbackManager (CORE-007)
    /// self-registers first via its execution order; this bootstrap runs slightly later and verifies
    /// that the always-on fallback authority actually came up before features rely on it.
    /// Contains NO feature logic — startup validation only.
    /// </summary>
    [DefaultExecutionOrder(-500)] // After FallbackManager (-1000), before normal components.
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Tooltip("The single ServiceRegistry asset, shared with all services. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        private void Start()
        {
            if (_services == null)
            {
                Debug.LogError("[AppBootstrap] ServiceRegistry not assigned in the inspector.");
                return;
            }

            if (_services.FallbackManager == null)
            {
                Debug.LogError(
                    "[AppBootstrap] FallbackManager (CORE-007) did not register. It must be present " +
                    "in the scene and assigned the same ServiceRegistry asset.");
                return;
            }

            Debug.Log(
                $"[AppBootstrap] Core services ready. AppState = {_services.FallbackManager.CurrentState}.");
        }
    }
}
