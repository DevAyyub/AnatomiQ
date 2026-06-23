namespace AnatomiQ.Core
{
    /// <summary>
    /// Contract CORE-001's ARSessionManager exposes so CORE-007 can read AR tracking status WITHOUT
    /// the Core assembly referencing AR Foundation (Core must not depend on the AR pillar). The
    /// ARSessionManager translates AR Foundation's session / tracking state into the Core-local
    /// <see cref="ArTrackingStatus"/> and registers itself through the <see cref="ServiceRegistry"/>;
    /// CORE-007's <c>CheckArTracking</c> PULLS this on each monitoring pass and maps it to
    /// <see cref="AppState"/>.
    ///
    /// PULL, not push (CORE-001 decision): CheckArTracking reads this provider exactly as
    /// CheckConnectivity / CheckThermal read theirs, which keeps the tracking → AppState POLICY inside
    /// CORE-007 (the single owner of AppState) rather than letting the AR pillar drive global state
    /// directly. The ~1s sampling latency this implies is fine for the global AppState; the INSTANT
    /// visual reaction (freeze + lock-to-screen-center on tracking loss) keys off ARSessionManager's
    /// own change-only tracking event, not this slow path.
    ///
    /// Extends <see cref="IService"/> so it registers via the standard
    /// <see cref="ServiceRegistry.Register"/> switch (unlike IDataLayer, which cannot extend IService
    /// for cycle reasons — IArTrackingProvider lives in Core and the AR assembly references Core, so
    /// there is no cycle). Because an AR scene can load and unload during a session, the implementer
    /// MUST clear its registry slot on teardown via
    /// <see cref="ServiceRegistry.ClearArTrackingProvider"/> — an interface reference to a destroyed
    /// MonoBehaviour does NOT read as Unity-null, so a stale provider would otherwise keep being read.
    /// </summary>
    public interface IArTrackingProvider : IService
    {
        /// <summary>
        /// The current AR tracking status, already translated into the Core-local enum. CORE-007 maps
        /// this to <see cref="AppState"/>; a UI layer may also read it for finer-grained messaging.
        /// </summary>
        ArTrackingStatus Status { get; }
    }
}
