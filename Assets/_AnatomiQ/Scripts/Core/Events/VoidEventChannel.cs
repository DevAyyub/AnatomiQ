using System;
using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>A no-payload event channel for simple "something happened" signals.</summary>
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "AnatomiQ/Events/Void Event Channel")]
    public sealed class VoidEventChannel : ScriptableObject
    {
        /// <summary>Raised when <see cref="Raise"/> is called.</summary>
        public event Action OnRaised;

        /// <summary>Publishes the signal to all current subscribers.</summary>
        public void Raise() => OnRaised?.Invoke();
    }
}
