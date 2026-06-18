using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Concrete <see cref="EventChannel{T}"/> carrying <see cref="AppState"/> values.
    /// FallbackManager (CORE-007) raises this when global state changes.
    /// </summary>
    [CreateAssetMenu(fileName = "AppStateEventChannel", menuName = "AnatomiQ/Events/App State Event Channel")]
    public sealed class AppStateEventChannel : EventChannel<AppState>
    {
    }
}
