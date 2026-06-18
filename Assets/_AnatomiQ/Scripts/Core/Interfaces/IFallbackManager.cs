using System;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Contract for CORE-007. Owns the global <see cref="AppState"/> and is the single
    /// source of truth other systems read. Concrete MonoBehaviour added in Section 6.
    /// </summary>
    public interface IFallbackManager : IService
    {
        /// <summary>The current global application state.</summary>
        AppState CurrentState { get; }

        /// <summary>Raised whenever <see cref="CurrentState"/> changes.</summary>
        event Action<AppState> OnAppStateChanged;
    }
}
