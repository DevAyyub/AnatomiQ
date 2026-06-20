namespace AnatomiQ.Core
{
    /// <summary>
    /// Testability seam over per-frame timing. CORE-007's FPS collector reads frame durations
    /// through this instead of calling <see cref="UnityEngine.Time.unscaledDeltaTime"/> directly, so
    /// PlayMode tests can feed synthetic frame times (e.g. a steady 20 FPS, or a jittery burst) and
    /// assert the rolling-average + sustain logic deterministically, without having to actually run
    /// the editor at a given frame rate.
    ///
    /// <see cref="UnscaledDeltaTime"/> is used (not scaled deltaTime) so the FPS signal reflects real
    /// wall-clock frame cost and is unaffected by Time.timeScale — pausing or slow-mo must not look
    /// like a performance problem.
    ///
    /// The production implementation is <see cref="UnityFrameClock"/>; tests supply a fake.
    /// </summary>
    public interface IFrameClock
    {
        /// <summary>Seconds elapsed for the most recent frame, unaffected by time scale.</summary>
        float UnscaledDeltaTime { get; }
    }
}
