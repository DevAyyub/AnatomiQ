namespace AnatomiQ.Core
{
    /// <summary>
    /// Testability seam over the device's reachability signal. CORE-007's <c>CheckConnectivity</c>
    /// reads through this rather than calling <see cref="UnityEngine.Application.internetReachability"/>
    /// directly, so PlayMode tests can drive offline/online transitions deterministically without a
    /// real network (the consuming-logic-is-testable, source-is-mocked pattern from the testing
    /// standard).
    ///
    /// The production implementation is <see cref="UnityConnectivityProvider"/>; tests supply a fake.
    /// </summary>
    public interface IConnectivityProvider
    {
        /// <summary>True when the device reports any reachable network (carrier or LAN/Wi-Fi).</summary>
        bool IsReachable { get; }
    }
}
