using UnityEngine.Profiling;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Swappable source of the app's current memory footprint in megabytes, mirroring the other
    /// CORE-007 signal seams (<see cref="IConnectivityProvider"/> / <see cref="IFrameClock"/> /
    /// <see cref="IThermalProvider"/>). The real implementation reads Unity's Profiler; PlayMode
    /// tests inject a fake so the RAM → <see cref="PerformanceMetrics.RamMegabytes"/> wiring is
    /// verifiable without a live Profiler. Internal: a test-only seam, not part of Core's public API.
    /// </summary>
    internal interface IMemoryProbe
    {
        /// <summary>
        /// Current memory footprint in megabytes, or -1 if no source is available (mirrors the
        /// "-1 means not sampled" convention used by the thermal and RAM fields).
        /// </summary>
        float SampleMegabytes();
    }

    /// <summary>
    /// Production <see cref="IMemoryProbe"/>. Reports Unity's internally-tracked allocated memory via
    /// <see cref="Profiler.GetTotalAllocatedMemoryLong"/>.
    ///
    /// IMPORTANT — what this number is and is NOT: it is the Unity allocator heap, not the OS process
    /// RSS. It EXCLUDES ARCore's native footprint (~100-150 MB per Performance doc A.1) and the
    /// graphics-driver allocation, so it will read LOWER than Android's reported app memory. It is a
    /// Unity-heap proxy against the A.4 soft ceiling (1400 MB) — adequate for the A.12 dev overlay
    /// (decision F), not a true RSS figure. The memory Profiler methods carry NO Development-Build
    /// requirement, so this reads correctly on the normal release APK at the chunk-6 device gate.
    /// </summary>
    internal sealed class UnityMemoryProbe : IMemoryProbe
    {
        private const float BYTES_PER_MEGABYTE = 1024f * 1024f;

        /// <inheritdoc />
        public float SampleMegabytes()
        {
            long bytes = Profiler.GetTotalAllocatedMemoryLong();
            // GetTotalAllocatedMemoryLong returns 0 when the Profiler is unavailable; map that to the
            // -1 "no sample" sentinel rather than reporting a misleading 0 MB.
            return bytes <= 0L ? -1f : bytes / BYTES_PER_MEGABYTE;
        }
    }
}
