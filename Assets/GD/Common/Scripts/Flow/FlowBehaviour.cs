using System.Collections;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// Runs one flow sequence as a coroutine. Child classes describe the sequence.
    /// </summary>
    public abstract class FlowBehaviour : MonoBehaviour
    {
        [SerializeField, Tooltip("Start when the scene starts")]
        private bool playOnStart;

        [SerializeField, Tooltip("Ignore requests after the first")]
        private bool playOnce;

        private FlowSequence sequence;
        private Coroutine running;
        private bool hasPlayed;

        /// <summary>The sequence this flow runs, once built in Awake.</summary>
        public FlowSequence Sequence => sequence;

        /// <summary>True while the sequence is running.</summary>
        public bool IsRunning => running != null;

        /// <summary>True once the flow has started at least once.</summary>
        public bool HasPlayed => hasPlayed;

        private void Awake()
        {
            sequence = BuildSequence();
        }

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        private void OnDisable()
        {
            // Disabling a component does not stop its coroutines, so stop this one here.
            if (running != null)
                StopCoroutine(running);

            running = null;
        }

        /// <summary>Starts the flow, unless it is running or has already played once.</summary>
        public void Play()
        {
            if (running != null || (playOnce && hasPlayed))
                return;

            hasPlayed = true;
            running = StartCoroutine(RunSequence());
        }

        /// <summary>Describes this flow. Called once, in Awake.</summary>
        protected abstract FlowSequence BuildSequence();

        private IEnumerator RunSequence()
        {
            yield return sequence.Run();
            running = null;
        }
    }
}
