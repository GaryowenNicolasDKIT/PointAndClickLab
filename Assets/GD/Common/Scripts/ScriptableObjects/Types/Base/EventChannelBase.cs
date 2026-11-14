using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Base class for every event channel asset. Holds the members all channel types share:
    /// designer notes and optional logging of each raise.
    /// </summary>
    /// <remarks>
    /// An event channel announces that something happened. Unlike a
    /// <see cref="ScriptableVariable{T}"/>, it stores no value: a listener that subscribes
    /// after an event was raised does not receive it.
    /// </remarks>
    public abstract class EventChannelBase : ScriptableObject
    {
        #region Fields

        [SerializeField, TextArea(2, 4)]
        [Tooltip("What this event means, which systems raise it, and which systems listen.")]
        private string description = string.Empty;

        [SerializeField]
        [Tooltip("Log every raise to the Console. Clicking the log entry selects this asset.")]
        private bool logRaises;

        #endregion Fields

        #region Properties

        /// <summary>Designer notes on what this event means.</summary>
        public string Description => description;

        /// <summary>
        /// The number of handlers currently subscribed. Useful in the debugger to check
        /// whether a listener has subscribed yet.
        /// </summary>
        public abstract int ListenerCount { get; }

        #endregion Properties

        #region Core Methods

        /// <summary>
        /// Writes a Console message for a raise when <c>logRaises</c> is ticked.
        /// </summary>
        /// <param name="payload">Text describing the value sent, or an empty string for no value.</param>
        protected void LogRaise(string payload)
        {
            if (logRaises)
                Debug.Log($"[{name}] raised{payload} ({ListenerCount} listeners)", this);
        }

        #endregion Core Methods
    }
}
