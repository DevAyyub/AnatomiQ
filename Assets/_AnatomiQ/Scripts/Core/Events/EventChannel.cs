using System;
using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Generic ScriptableObject event channel. Decouples publishers from subscribers so
    /// systems reference a channel asset rather than each other. Subscribe in OnEnable,
    /// unsubscribe in OnDisable.
    /// </summary>
    /// <typeparam name="T">Payload type carried by this channel.</typeparam>
    public abstract class EventChannel<T> : ScriptableObject
    {
        /// <summary>Raised when <see cref="Raise"/> is called, carrying the payload.</summary>
        public event Action<T> OnRaised;

        /// <summary>Publishes <paramref name="value"/> to all current subscribers.</summary>
        public void Raise(T value) => OnRaised?.Invoke(value);
    }
}
