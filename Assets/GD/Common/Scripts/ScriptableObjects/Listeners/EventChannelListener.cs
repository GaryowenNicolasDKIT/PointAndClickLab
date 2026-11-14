using UnityEngine;
using UnityEngine.Events;

namespace GD.Types
{
    /// <summary>
    /// Base class for a listener that runs responses set in the Inspector when an
    /// <see cref="EventChannel{T}"/> is raised, passing the raised value to each response.
    /// </summary>
    /// <remarks>
    /// Unity cannot add a generic component to a GameObject, so each channel type has a small
    /// concrete subclass, for example <see cref="IntEventChannelListener"/>. Using the concrete
    /// channel type for <typeparamref name="TChannel"/> means the Inspector only accepts the
    /// matching channel asset.
    /// Place the listener on a GameObject that is active when the event can be raised. A listener
    /// on an inactive object has not subscribed, so it misses the event.
    /// </remarks>
    /// <typeparam name="TChannel">The channel asset type to listen to.</typeparam>
    /// <typeparam name="T">The type of value the channel sends.</typeparam>
    public abstract class EventChannelListener<TChannel, T> : MonoBehaviour
        where TChannel : EventChannel<T>
    {
        #region Fields

        [SerializeField]
        [Tooltip("The channel to listen to.")]
        private TChannel channel;

        [SerializeField]
        [Tooltip("Called, in order, each time the channel is raised. Choose a Dynamic entry to receive the raised value.")]
        private UnityEvent<T> response;

        #endregion Fields

        #region Properties

        /// <summary>The channel this listener currently listens to. Change it with <see cref="SetChannel"/>.</summary>
        public TChannel Channel => channel;

        #endregion Properties

        #region Unity Messages

        private void OnEnable()
        {
            if (channel == null)
            {
                Debug.LogError($"{name}: assign the channel to listen to.", this);
                enabled = false;
                return;
            }

            channel.Raised += OnRaised;
        }

        private void OnDisable()
        {
            if (channel != null)
                channel.Raised -= OnRaised;
        }

        #endregion Unity Messages

        #region Core Methods

        /// <summary>
        /// Switches this listener to <paramref name="newChannel"/> at runtime, for example when a
        /// pooled object is reused for a different team.
        /// </summary>
        /// <remarks>
        /// Assigning the field directly would leave the handler subscribed to the old channel, so
        /// this method unsubscribes first. It only subscribes to the new channel while the listener
        /// is active and enabled, matching <c>OnEnable</c>; a disabled listener subscribes when it is
        /// next enabled. Pass <c>null</c> to stop listening without disabling the component.
        /// </remarks>
        public void SetChannel(TChannel newChannel)
        {
            if (newChannel == channel)
                return;

            if (isActiveAndEnabled && channel != null)
                channel.Raised -= OnRaised;

            channel = newChannel;

            if (isActiveAndEnabled && channel != null)
                channel.Raised += OnRaised;
        }

        private void OnRaised(T value)
        {
            response.Invoke(value);
        }

        #endregion Core Methods
    }
}
