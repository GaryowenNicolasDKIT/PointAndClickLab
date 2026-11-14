using UnityEngine;
using UnityEngine.Events;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="VoidEventChannel"/> is raised,
    /// so designers can react to an event without writing code.
    /// </summary>
    /// <remarks>
    /// Place this on a GameObject that is active when the event can be raised. A listener
    /// on an inactive object has not subscribed, so it misses the event.
    /// </remarks>
    [AddComponentMenu("GD/Events/Void Event Channel Listener")]
    public class VoidEventChannelListener : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        [Tooltip("The channel to listen to.")]
        private VoidEventChannel channel;

        [SerializeField]
        [Tooltip("Called, in order, each time the channel is raised.")]
        private UnityEvent response;

        #endregion Fields

        #region Properties

        /// <summary>The channel this listener currently listens to. Change it with <see cref="SetChannel"/>.</summary>
        public VoidEventChannel Channel => channel;

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
        public void SetChannel(VoidEventChannel newChannel)
        {
            if (newChannel == channel)
                return;

            if (isActiveAndEnabled && channel != null)
                channel.Raised -= OnRaised;

            channel = newChannel;

            if (isActiveAndEnabled && channel != null)
                channel.Raised += OnRaised;
        }

        private void OnRaised()
        {
            response.Invoke();
        }

        #endregion Core Methods
    }
}
