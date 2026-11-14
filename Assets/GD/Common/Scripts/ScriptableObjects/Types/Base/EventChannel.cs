using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Base class for an event channel that sends a value with each raise, for example
    /// the points from a collected pickup.
    /// </summary>
    /// <remarks>
    /// Every call to <see cref="Raise"/> notifies listeners, even when the value is the same
    /// as last time. That is the key difference from <see cref="ScriptableVariable{T}.ValueChanged"/>,
    /// which only fires when the stored value changes.
    /// </remarks>
    /// <typeparam name="T">The type of value sent with the event.</typeparam>
    public abstract class EventChannel<T> : EventChannelBase
    {
        #region Events

        /// <summary>
        /// Raised by <see cref="Raise"/>. Subscribe in OnEnable and unsubscribe in OnDisable.
        /// </summary>
        public event Action<T> Raised;  //holds a list of methods that take as parameter a T e.g. HandlePlayerUpgrade(int newLevel)

        #endregion Events

        #region Properties

        /// <inheritdoc/>
        public override int ListenerCount{
            get
            {
               return Raised?.GetInvocationList().Length ?? 0;
            }
        }

        #endregion Properties

        #region Core Methods

        /// <summary>Notifies every current listener, passing <paramref name="value"/>.</summary>
        public void Raise(T value)
        {
            LogRaise($" with {value}");
            Raised?.Invoke(value);  //invoke all listening methods that take a parameter of type T
        }

        #endregion Core Methods
    }
}
