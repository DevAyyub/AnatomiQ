// DEFERRED TO CORE-001 (Decision B). This is the CONCRETE thermal provider that reads real device
// values through Adaptive Performance. It is compiled out until the Android provider subsystem is
// installed and enabled, at which point CORE-001 defines ANATOMIQ_ADAPTIVE_PERFORMANCE (Project
// Settings ▸ Player ▸ Scripting Define Symbols, Android) to activate it, and wires it into
// FallbackManager in place of NullThermalProvider.
//
// Why guarded rather than just written: referencing UnityEngine.AdaptivePerformance before the
// package/provider is present would not compile. The guard lets this file live in the repo now (so
// the device-pass work is a one-line define + one-line wiring, not "go write the adapter") while
// keeping today's build package-free.
//
// Tracked in bugs_and_decisions.md as a CORE-007→CORE-001 carry-forward, NOT a silent TODO.
//
// Enablement checklist for CORE-001 (on the Poco X5 Pro device pass):
//   1. Install the Adaptive Performance Android (Google) provider (bundled with Unity 6.3).
//   2. Enable it in Project Settings ▸ Adaptive Performance ▸ Android.
//   3. Add ANATOMIQ_ADAPTIVE_PERFORMANCE to Android scripting define symbols.
//   4. In FallbackManager, default/configure _thermal = new AdaptivePerformanceThermalProvider();
//   5. Verify on-device against a real throttling scenario (sustained AR + cascade load).

#if ANATOMIQ_ADAPTIVE_PERFORMANCE
using UnityEngine.AdaptivePerformance;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Production <see cref="IThermalProvider"/> backed by Unity Adaptive Performance. Translates the
    /// package's <c>WarningLevel</c> into the Core-local <see cref="ThermalWarning"/> and surfaces
    /// <c>TemperatureLevel</c>. Guarded by ANATOMIQ_ADAPTIVE_PERFORMANCE — see file header.
    /// </summary>
    public sealed class AdaptivePerformanceThermalProvider : IThermalProvider
    {
        private static IAdaptivePerformance Ap => Holder.Instance;

        /// <inheritdoc />
        public bool IsAvailable => Ap != null && Ap.Active;

        /// <inheritdoc />
        public ThermalWarning Warning
        {
            get
            {
                if (!IsAvailable)
                {
                    return ThermalWarning.None;
                }

                switch (Ap.ThermalStatus.ThermalMetrics.WarningLevel)
                {
                    case WarningLevel.Throttling:        return ThermalWarning.Throttling;
                    case WarningLevel.ThrottlingImminent: return ThermalWarning.ThrottlingImminent;
                    default:                              return ThermalWarning.None;
                }
            }
        }

        /// <inheritdoc />
        public float TemperatureLevel =>
            IsAvailable ? Ap.ThermalStatus.ThermalMetrics.TemperatureLevel : 0f;
    }
}
#endif
