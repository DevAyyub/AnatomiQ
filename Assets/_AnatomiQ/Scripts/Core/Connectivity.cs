namespace AnatomiQ.Core
{
    /// <summary>
    /// Network reachability, owned and published by CORE-007 (FallbackManager) as a THIRD global
    /// signal alongside <see cref="AppState"/> and <see cref="PerformanceTier"/>.
    ///
    /// CORE-001 design decision: connectivity was lifted off the <see cref="AppState"/> axis onto its
    /// own signal, completing the separation the two-axis (AppState / PerformanceTier) decision began.
    /// Reasoning: "no network" is no more an AR *mode* than "low FPS" is — fusing it onto AppState
    /// meant a single enum slot could not represent a device that is AR-active AND offline at the
    /// same time. With connectivity independent, <see cref="AppState"/> becomes a pure function of AR
    /// tracking (its single writer is CheckArTracking), and all three signals are individually
    /// truthful: a device can be <see cref="AppState.AR_ACTIVE"/> + <see cref="Offline"/> +
    /// <see cref="PerformanceTier.Reduced"/> simultaneously, each read correctly.
    ///
    /// SCOPE: this is NETWORK reachability only — "is there a usable network at all". API / service
    /// health ("the network is up but the AI endpoint is failing") is a SEPARATE concern that CORE-006
    /// adds via CheckApiAvailability. Do not fold API health into this enum.
    /// </summary>
    public enum Connectivity
    {
        /// <summary>A reachable network is present (carrier or LAN / Wi-Fi).</summary>
        Online = 0,

        /// <summary>No reachable network; AI features fall back to cached / pre-baked data.</summary>
        Offline = 1
    }
}
