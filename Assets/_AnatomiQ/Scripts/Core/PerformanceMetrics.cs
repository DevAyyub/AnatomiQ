namespace AnatomiQ.Core
{
    /// <summary>
    /// Immutable snapshot of the signals CORE-007 monitors, exposed via
    /// <see cref="IFallbackManager.Metrics"/> for the A.12 performance overlay and the
    /// "metric over budget for 3+ seconds" logging hook.
    ///
    /// This is the data contract for that overlay; the on-screen widget itself is deferred to
    /// CORE-002 (it needs renderer-reported GPU/drawcall/triangle stats and a UI host that does not
    /// exist yet). Fields the logic phase cannot populate on-device today are present but documented
    /// as not-yet-wired, so the consumer's shape is stable from the start.
    ///
    /// A struct (not a class) so reads are allocation-free — <see cref="IFallbackManager.Metrics"/>
    /// can be sampled every frame by a debug overlay without generating GC pressure against the 3 ms
    /// GC budget in A.2.
    /// </summary>
    public readonly struct PerformanceMetrics
    {
        /// <summary>Rolling average frames-per-second over the FPS sample window (A.3 / A.54).</summary>
        public readonly float RollingFps;

        /// <summary>
        /// Current quality tier derived from FPS and thermal. Mirrors
        /// <see cref="IFallbackManager.CurrentTier"/>; included here so a single snapshot read gives
        /// the overlay everything it needs.
        /// </summary>
        public readonly PerformanceTier Tier;

        /// <summary>
        /// Normalized device temperature in [0,1] from Adaptive Performance
        /// (<c>ThermalMetrics.TemperatureLevel</c>): 0 = normal, 1 = throttling. Negative (-1) means
        /// no thermal source is active yet (editor, or before the Android provider is enabled at
        /// CORE-001 — see the deferred thermal-provider item).
        /// </summary>
        public readonly float TemperatureLevel;

        /// <summary>
        /// App heap in megabytes vs the A.4 soft ceiling (1400 MB). Not wired in the logic phase
        /// (RAM-driven degradation lands with CORE-002's mesh/LOD management); -1 until then.
        /// </summary>
        public readonly float RamMegabytes;

        /// <summary>Creates an immutable metrics snapshot.</summary>
        public PerformanceMetrics(float rollingFps, PerformanceTier tier, float temperatureLevel, float ramMegabytes)
        {
            RollingFps = rollingFps;
            Tier = tier;
            TemperatureLevel = temperatureLevel;
            RamMegabytes = ramMegabytes;
        }
    }
}
