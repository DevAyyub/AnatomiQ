namespace AnatomiQ.Core
{
    /// <summary>
    /// Testability seam over the device thermal signal. CORE-007's <c>CheckThermal</c> reads through
    /// this rather than calling Adaptive Performance (<c>Holder.Instance.ThermalStatus.ThermalMetrics</c>)
    /// directly, so PlayMode tests can drive each thermal stage deterministically without a hot
    /// device (the consuming-logic-is-testable, source-is-mocked pattern from the testing standard).
    ///
    /// DECISION B (CORE-007 logic phase): this seam, the WarningLevel/TemperatureLevel → tier
    /// mapping, the thermal hysteresis, and all tests land in the logic phase. The CONCRETE
    /// production implementation (<c>AdaptivePerformanceThermalProvider</c>, reading real
    /// <c>Holder.Instance</c> values) and the Project Settings enablement of the Android provider
    /// subsystem are DEFERRED to the CORE-001 device pass — Adaptive Performance returns no data in
    /// the editor or PlayMode, and that work belongs alongside the on-device ARCore bring-up.
    ///
    /// Until that adapter exists, <see cref="NullThermalProvider"/> is the default: it reports
    /// "no thermal source" (<see cref="IsAvailable"/> = false), so CheckThermal stays inert and the
    /// thermal tier never moves off Nominal — matching how the AR/API/inference checks stay inert
    /// until their sources exist.
    /// </summary>
    public interface IThermalProvider
    {
        /// <summary>
        /// True when a real thermal source is active (Adaptive Performance present + provider enabled
        /// + <c>ap.Active</c>). When false, CheckThermal must not drive any tier change.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>Current device thermal warning level. Meaningful only when <see cref="IsAvailable"/>.</summary>
        ThermalWarning Warning { get; }

        /// <summary>
        /// Finer-grained normalized temperature in [0,1] (0 = normal, 1 = throttling), used to split
        /// <see cref="ThermalWarning.ThrottlingImminent"/> into the Moderate vs Severe stages at the
        /// 0.8 boundary. Meaningful only when <see cref="IsAvailable"/>.
        /// </summary>
        float TemperatureLevel { get; }
    }
}
