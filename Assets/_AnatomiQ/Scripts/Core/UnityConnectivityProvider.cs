using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Production <see cref="IConnectivityProvider"/> backed by Unity's
    /// <see cref="Application.internetReachability"/>. Treats both carrier-data and LAN/Wi-Fi as
    /// reachable; only <see cref="NetworkReachability.NotReachable"/> counts as offline.
    ///
    /// Note: this reports whether a network INTERFACE is up, not whether the AI API is actually
    /// answering — true API health is CORE-006's job via <c>CheckApiAvailability</c> (still a stub).
    /// A device can be "reachable" here yet have a dead API; that distinction is intentional.
    /// </summary>
    public sealed class UnityConnectivityProvider : IConnectivityProvider
    {
        /// <inheritdoc />
        public bool IsReachable =>
            Application.internetReachability != NetworkReachability.NotReachable;
    }
}
