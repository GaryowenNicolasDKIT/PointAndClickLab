using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends no value, for example "player died" or "level complete".
    /// </summary>
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "GD/Events/Void", order = 0)]
    public class VoidEventChannel : EventChannelBase
    {
        #region Events

        /// <summary>
        /// Raised by <see cref="Raise"/>. Subscribe in OnEnable and unsubscribe in OnDisable.
        /// </summary>
        public event Action Raised;

        #endregion Events

        #region Properties

        /// <inheritdoc/>
        public override int ListenerCount => Raised?.GetInvocationList().Length ?? 0;

        #endregion Properties

        #region Core Methods

        /// <summary>
        /// Notifies every current listener. Also available from the asset's context menu,
        /// so you can test listeners during Play without triggering the real cause.
        /// </summary>
        [ContextMenu("Raise")]
        public void Raise()
        {
            LogRaise(string.Empty);
            Raised?.Invoke();
        }

        #endregion Core Methods
    }
}
