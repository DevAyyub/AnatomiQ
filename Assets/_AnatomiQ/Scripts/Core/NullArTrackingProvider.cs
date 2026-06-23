namespace AnatomiQ.Core
{
    /// <summary>
    /// Default <see cref="IArTrackingProvider"/> behaviour for when no AR provider is registered —
    /// before any AR scene loads, on a device with no ARCore, or after the AR scene unloads. Reports
    /// <see cref="ArTrackingStatus.Unavailable"/>, which CORE-007 maps to the safe
    /// <see cref="AppState.AR_VIEWER_MODE"/> baseline (the 3D viewer always works).
    ///
    /// Note the difference from <see cref="NullThermalProvider"/>: an absent thermal source leaves the
    /// thermal check INERT (it must not invent a tier), whereas an absent AR source is itself
    /// meaningful — "no AR" legitimately means viewer mode. CheckArTracking therefore maps a missing
    /// provider's status normally (resolving null to <see cref="ArTrackingStatus.Unavailable"/>)
    /// rather than early-returning.
    ///
    /// CORE-007 does not hold an instance of this (it resolves a missing provider to
    /// <see cref="ArTrackingStatus.Unavailable"/> inline). It exists for clarity, for tests that want
    /// an explicit "AR unavailable" source, and for any caller that prefers a non-null default object.
    /// </summary>
    public sealed class NullArTrackingProvider : IArTrackingProvider
    {
        /// <inheritdoc />
        public ArTrackingStatus Status => ArTrackingStatus.Unavailable;
    }
}
