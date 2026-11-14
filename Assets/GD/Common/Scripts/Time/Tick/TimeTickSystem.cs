using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GD.Tick
{
    /// <summary>
    /// Sends a tick every baseTickIntervalSecs. Handlers register for a rate
    /// (every 1, 2, 4 or 8 ticks) and are called at that rate instead of every frame.
    /// </summary>
    /// <remarks>
    /// Within one tick, faster rates are called before slower ones, and handlers
    /// of one rate are called in the order they registered.
    /// </remarks>
    /// <see cref="IHandleTicks"/>
    /// <see cref="TickBehaviour"/>
    [DisallowMultipleComponent]
    [AddComponentMenu("GD/Time/Time Tick System")]
    public class TimeTickSystem : Singleton<TimeTickSystem>
    {
        /// <summary>
        /// How many base ticks pass between calls. Values are powers of two,
        /// so every slower rate fires on a tick where the faster rates also fire,
        /// unless Spread Handlers is on.
        /// </summary>
        public enum TickRate
        {
            Every1 = 1,
            Every2 = 2,
            Every4 = 4,
            Every8 = 8
        }

        /// <summary>
        /// One registered handler. The phase moves the ticks it is called on,
        /// so handlers of one rate can be spread across several ticks.
        /// </summary>
        private struct Registration
        {
            public IHandleTicks Handler;
            public uint Phase;
        }

        // Every value in TickRate, cached so the tick loop does not allocate.
        private static readonly TickRate[] AllRates =
            { TickRate.Every1, TickRate.Every2, TickRate.Every4, TickRate.Every8 };

        // Cached so removing entries after a tick does not allocate.
        private static readonly Predicate<Registration> IsRemoved =
            registration => registration.Handler == null;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Seconds between base ticks. At 0.1, Every8 handlers run every 0.8 s")]
        private float baseTickIntervalSecs = 0.1f;

        [SerializeField]
        [Tooltip("If true, ticks continue while Time.timeScale is 0 (for example, during a pause menu)")]
        private bool useUnscaledTime;

        [SerializeField]
        [Range(1, 10)]
        [Tooltip("Most ticks sent in one frame to catch up after a long frame. Ticks still owed after that are dropped")]
        private int maxTicksPerFrame = 3;

        [SerializeField]
        [Tooltip("If true, handlers of one rate are shared out across that many ticks, so an Every8 handler " +
                 "does not run on the same tick as every other Every8 handler")]
        private bool spreadHandlers;

        // Created with the object, not in Awake, because a handler's OnEnable can run
        // before this object's Awake when both are in the same scene.
        private readonly Dictionary<TickRate, List<Registration>> registrations = CreateRegistrationLists();
        private readonly Dictionary<TickRate, uint> nextPhase = new();

        private float tickTimer;
        private bool isSending;
        private bool hasRemovals;

        /// <summary>The number of base ticks sent since the system started.</summary>
        public uint Tick { get; private set; }

        /// <summary>Seconds between base ticks.</summary>
        public float BaseTickInterval => baseTickIntervalSecs;

        /// <summary>
        /// Seconds between calls at a rate, for example to turn a per-second
        /// amount into a per-tick amount.
        /// </summary>
        public float SecondsPerTick(TickRate rate)
        {
            return baseTickIntervalSecs * (int)rate;
        }

        /// <summary>
        /// The number of handlers registered at a rate, for debugging.
        /// </summary>
        public int HandlerCount(TickRate rate)
        {
            int count = 0;

            foreach (Registration registration in registrations[rate])
            {
                if (registration.Handler != null)
                    count++;
            }

            return count;
        }

        private void Update()
        {
            tickTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            for (int sent = 0; sent < maxTicksPerFrame && tickTimer >= baseTickIntervalSecs; sent++)
            {
                tickTimer -= baseTickIntervalSecs;
                SendTick();
            }

            // After a long frame (a hitch or a breakpoint), the ticks still owed are
            // dropped rather than sent in a burst. The remainder keeps the rhythm.
            if (tickTimer >= baseTickIntervalSecs)
                tickTimer %= baseTickIntervalSecs;
        }

        private void SendTick()
        {
            Tick++;
            isSending = true;

            foreach (TickRate rate in AllRates)
                SendTo(registrations[rate], (uint)rate);

            isSending = false;

            if (!hasRemovals)
                return;

            foreach (TickRate rate in AllRates)
                registrations[rate].RemoveAll(IsRemoved);

            hasRemovals = false;
        }

        private void SendTo(List<Registration> list, uint rate)
        {
            // A handler registered during this tick is added past this count,
            // so its first call is on a later tick.
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                Registration registration = list[i];

                // Unregistered earlier in this tick.
                if (registration.Handler == null)
                    continue;

                if ((Tick + registration.Phase) % rate != 0)
                    continue;

                // A handler destroyed without unregistering compares equal to null
                // (Unity's null check), so it is removed instead of called.
                if (registration.Handler is Object unityObject && unityObject == null)
                {
                    MarkRemoved(list, i);
                    continue;
                }

                try
                {
                    registration.Handler.HandleTick();
                }
                catch (Exception exception)
                {
                    // One failing handler must not stop the others for this tick.
                    Debug.LogException(exception, registration.Handler as Object);
                }
            }
        }

        /// <summary>
        /// Registers a handler to be called at the given rate. Call from OnEnable.
        /// </summary>
        public void Register(IHandleTicks handler, TickRate rate)
        {
            if (handler == null || IndexOf(registrations[rate], handler) >= 0)
                return;

            uint phase = 0;

            if (spreadHandlers)
            {
                nextPhase.TryGetValue(rate, out phase);
                nextPhase[rate] = (phase + 1) % (uint)rate;
            }

            registrations[rate].Add(new Registration { Handler = handler, Phase = phase });
        }

        /// <summary>
        /// Unregisters a handler from the given rate. Call from OnDisable, guarded by
        /// HasInstance, so a handler disabled during shutdown does not create a new system.
        /// </summary>
        public void Unregister(IHandleTicks handler, TickRate rate)
        {
            List<Registration> list = registrations[rate];
            int index = IndexOf(list, handler);

            if (index < 0)
                return;

            // Removing from a list that is being looped over would shift the
            // handlers after it, so during a tick the entry is cleared instead.
            if (isSending)
                MarkRemoved(list, index);
            else
                list.RemoveAt(index);
        }

        private void MarkRemoved(List<Registration> list, int index)
        {
            list[index] = default;
            hasRemovals = true;
        }

        private static int IndexOf(List<Registration> list, IHandleTicks handler)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i].Handler, handler))
                    return i;
            }

            return -1;
        }

        private static Dictionary<TickRate, List<Registration>> CreateRegistrationLists()
        {
            Dictionary<TickRate, List<Registration>> lists = new();

            foreach (TickRate rate in AllRates)
                lists[rate] = new List<Registration>();

            return lists;
        }
    }
}
