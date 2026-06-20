namespace AnatomiQ.Core
{
    /// <summary>
    /// Quality/degradation severity, owned and published by CORE-007 (FallbackManager).
    /// This is the SECOND, orthogonal signal to <see cref="AppState"/>: where AppState encodes
    /// AR/connectivity context, PerformanceTier encodes how aggressively the renderer must shed
    /// quality (render scale, LOD, shadow distance, post-processing, inference frequency) to stay
    /// inside the frame and thermal budget on the target device.
    ///
    /// The two axes are independent and both authoritative — a device can be
    /// <see cref="AppState.AR_ACTIVE"/> while at <see cref="Critical"/> (tracking fine but hot), or
    /// <see cref="AppState.OFFLINE_MODE"/> while at <see cref="Nominal"/> (no network, cool device).
    ///
    /// Members are ordered by ASCENDING severity on purpose: the FPS axis and the thermal axis each
    /// produce a tier, and CORE-007 publishes the more severe of the two via
    /// <c>(PerformanceTier)System.Math.Max((int)fpsTier, (int)thermalTier)</c>. Do not reorder.
    ///
    /// Consumed by CORE-002 (3D Body Model Renderer), which translates each tier into concrete URP
    /// levers per AnatomiQ_Performance_And_Models.md A.10/A.11. No consumer exists yet at the time
    /// CORE-007's logic phase ships; the signal is published regardless (the contract is real).
    /// </summary>
    public enum PerformanceTier
    {
        /// <summary>Full quality. Render scale 0.9, all LODs/effects as authored. No throttling.</summary>
        Nominal = 0,

        /// <summary>
        /// Light reduction. Maps to thermal "Moderate" (A.10 stage 1): render scale ~0.85,
        /// post-processing off. Also the first FPS-driven step when the rolling average sits below
        /// the scenario minimum.
        /// </summary>
        Reduced = 1,

        /// <summary>
        /// Heavy reduction. Maps to thermal "Severe" (A.10 stage 2): on-device inference frequency
        /// halved, shadow distance reduced to ~8m, plus the user-visible "getting warm" warning.
        /// </summary>
        Aggressive = 2,

        /// <summary>
        /// Maximum reduction. Maps to thermal "Critical" (A.10 stage 3): everything forced to LOD 2,
        /// AR session frozen briefly, and the "taking a quick break" overlay shown. Last stop before
        /// the OS would throttle or kill the app.
        /// </summary>
        Critical = 3
    }
}
