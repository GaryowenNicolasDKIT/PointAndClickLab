using UnityEngine;

namespace GD.Tick
{
    /// <summary>
    /// Base class for a component that runs on ticks rather than every frame.
    /// It registers with TimeTickSystem when enabled and unregisters when
    /// disabled, so a subclass only writes HandleTick.
    /// </summary>
    /// <see cref="TimeTickSystem"/>
    public abstract class TickBehaviour : MonoBehaviour, IHandleTicks
    {
        [SerializeField]
        [Tooltip("How often HandleTick runs, in base ticks. Changes take effect the next time the component is enabled")]
        private TimeTickSystem.TickRate tickRate = TimeTickSystem.TickRate.Every1;

        // The rate actually registered with. Unregistering must use the same rate,
        // even if tickRate was edited in the Inspector while enabled.
        private TimeTickSystem.TickRate registeredRate;

        /// <summary>The rate this component is registered at.</summary>
        protected TimeTickSystem.TickRate Rate => registeredRate;

        /// <summary>
        /// Seconds between calls to HandleTick, for turning a per-second amount
        /// into a per-tick amount.
        /// </summary>
        protected float SecondsPerTick => TimeTickSystem.Instance.SecondsPerTick(registeredRate);

        protected virtual void OnEnable()
        {
            registeredRate = tickRate;
            TimeTickSystem.Instance.Register(this, registeredRate);
        }

        protected virtual void OnDisable()
        {
            // During shutdown the system may be destroyed first, and asking for
            // Instance then would create a new one.
            if (TimeTickSystem.HasInstance)
                TimeTickSystem.Instance.Unregister(this, registeredRate);
        }

        /// <inheritdoc/>
        public abstract void HandleTick();
    }
}
