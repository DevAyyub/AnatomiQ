using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Production <see cref="IFrameClock"/> backed by <see cref="Time.unscaledDeltaTime"/>.
    /// </summary>
    public sealed class UnityFrameClock : IFrameClock
    {
        /// <inheritdoc />
        public float UnscaledDeltaTime => Time.unscaledDeltaTime;
    }
}
