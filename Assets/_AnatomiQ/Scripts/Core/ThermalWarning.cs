namespace AnatomiQ.Core
{
    /// <summary>
    /// Device thermal warning level, mirrored into the Core assembly so CORE-007's thermal logic
    /// does NOT hard-depend on the Adaptive Performance package namespace
    /// (<c>UnityEngine.AdaptivePerformance.WarningLevel</c>). The deferred
    /// <c>AdaptivePerformanceThermalProvider</c> adapter translates the package's enum into this one;
    /// the signal logic and its tests run against this Core-local type, package-free.
    ///
    /// Values match Adaptive Performance's <c>WarningLevel</c> (stable across package versions 1.x–5.x):
    /// <list type="bullet">
    /// <item><see cref="None"/> = NoWarning — device is within normal thermal range.</item>
    /// <item><see cref="ThrottlingImminent"/> — OS is about to throttle; shed quality now to avoid it.</item>
    /// <item><see cref="Throttling"/> — OS is actively throttling CPU/GPU; maximum reduction.</item>
    /// </list>
    /// </summary>
    public enum ThermalWarning
    {
        /// <summary>NoWarning — normal thermal range.</summary>
        None = 0,

        /// <summary>Throttling is imminent; reduce load to avoid OS intervention.</summary>
        ThrottlingImminent = 1,

        /// <summary>OS is actively thermal-throttling the device.</summary>
        Throttling = 2
    }
}
