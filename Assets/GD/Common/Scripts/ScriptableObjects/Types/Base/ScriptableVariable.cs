using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GD.Types
{
    /// <summary>
    /// Base class for a value that several systems share at runtime through an asset,
    /// for example player health read by the player, the HUD and the audio system.
    /// </summary>
    /// <remarks>
    /// The authored <see cref="InitialValue"/> is never changed by code. The runtime
    /// <see cref="Value"/> is not serialized, so Play mode changes never reach the asset
    /// file. It resets to the initial value at the start of every play session, in the
    /// Editor (with or without domain reload) and in a build.
    /// </remarks>
    /// <typeparam name="T">The type of value held, for example float or int.</typeparam>
    public abstract class ScriptableVariable<T> : ScriptableObject
    {
        #region Fields

        [SerializeField, TextArea(2, 4)]
        [Tooltip("What this variable represents, and which systems read or write it.")]
        private string description = string.Empty;

        [SerializeField]
        [Tooltip("The value at the start of every play session.")]
        private T initialValue;

        // Runtime state. NonSerialized keeps it out of the asset file.
        [NonSerialized]
        private T runtimeValue;

        #endregion Fields

        #region Events

        /// <summary>
        /// Raised after <see cref="Value"/> changes, with the new value.
        /// Subscribe in OnEnable and unsubscribe in OnDisable.
        /// </summary>
        public event Action<T> ValueChanged;

        #endregion Events

        #region Properties

        /// <summary>Designer notes on what this variable is for.</summary>
        public string Description => description;

        /// <summary>The authored value that <see cref="Value"/> starts from.</summary>
        public T InitialValue => initialValue;

        /// <summary>
        /// The current runtime value. Setting a different value raises <see cref="ValueChanged"/>.
        /// </summary>
        public T Value
        {
            get => runtimeValue;
            set
            {
                // Ignore unchanged values so listeners only react to real changes.
                if (EqualityComparer<T>.Default.Equals(runtimeValue, value))
                    return;

                runtimeValue = value;
                ValueChanged?.Invoke(runtimeValue);
            }
        }

        #endregion Properties

        #region Core Methods

        /// <summary>
        /// Sets <see cref="Value"/> back to <see cref="InitialValue"/>, for example when a level restarts.
        /// Raises <see cref="ValueChanged"/> if the value changes.
        /// </summary>
        public void ResetToInitial()
        {
            Value = initialValue;
        }

        #endregion Core Methods

        #region Unity Messages

        protected virtual void OnEnable()
        {
            // Runs when the asset loads, in the Editor and in a build.
            runtimeValue = initialValue;

#if UNITY_EDITOR
            // With domain reload disabled, OnEnable does not run on entering Play mode,
            // so the reset must also happen when Play mode starts.
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

        protected virtual void OnValidate()
        {
            // Keep the runtime value in step with Inspector edits made outside Play mode.
            if (!Application.isPlaying)
                runtimeValue = initialValue;
        }

        #endregion Unity Messages

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Reset before Play starts, and again after it stops so Edit mode shows the authored value.
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode)
                runtimeValue = initialValue;
        }
#endif
    }
}
