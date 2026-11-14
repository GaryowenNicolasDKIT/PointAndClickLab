#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GD.States
{
    /// <summary>
    /// Editor-only data for the State and Flow Viewer. None of this is in a build.
    /// </summary>
    public partial class StateMachine
    {
        /// <summary>One change of state, for the viewer's history.</summary>
        public readonly struct DebugChange
        {
            public readonly string StateName;
            public readonly float Time;
            public readonly int Frame;

            public DebugChange(string stateName, float time, int frame)
            {
                StateName = stateName;
                Time = time;
                Frame = frame;
            }
        }

        /// <summary>One transition that could fire from the current state.</summary>
        public readonly struct DebugTransition
        {
            public readonly IState To;
            public readonly Func<bool> Condition;
            public readonly bool IsAnyState;

            public DebugTransition(IState to, Func<bool> condition, bool isAnyState)
            {
                To = to;
                Condition = condition;
                IsAnyState = isAnyState;
            }
        }

        private const int MaxDebugChanges = 32;

        private static readonly List<WeakReference<StateMachine>> DebugReferences = new();

        private readonly List<DebugChange> debugChanges = new();
        private float debugStateStartTime;

        /// <summary>Every machine still in use. Machines whose owner was destroyed are removed.</summary>
        public static IEnumerable<StateMachine> DebugInstances
        {
            get
            {
                DebugReferences.RemoveAll(IsFinished);

                foreach (WeakReference<StateMachine> reference in DebugReferences)
                {
                    if (reference.TryGetTarget(out StateMachine machine))
                        yield return machine;
                }
            }
        }

        public IReadOnlyList<DebugChange> DebugChanges => debugChanges;

        public float DebugTimeInState => CurrentState == null ? 0f : Time.unscaledTime - debugStateStartTime;

        /// <summary>The transitions checked from the current state, in the order they are checked.</summary>
        public IEnumerable<DebugTransition> DebugCurrentTransitions()
        {
            foreach (Transition transition in anyTransitions)
                yield return new DebugTransition(transition.To, transition.Condition, true);

            foreach (Transition transition in currentTransitions)
                yield return new DebugTransition(transition.To, transition.Condition, false);
        }

        // Static data survives entering Play mode when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearDebugReferences()
        {
            DebugReferences.Clear();
        }

        private static bool IsFinished(WeakReference<StateMachine> reference)
        {
            if (!reference.TryGetTarget(out StateMachine machine))
                return true;

            // Comparing with null also detects a destroyed Unity object.
            return machine.Owner is Object owner && owner == null;
        }

        partial void RegisterForDebugging()
        {
            DebugReferences.Add(new WeakReference<StateMachine>(this));
        }

        partial void RecordStateChange()
        {
            debugStateStartTime = Time.unscaledTime;

            if (debugChanges.Count == MaxDebugChanges)
                debugChanges.RemoveAt(0);

            debugChanges.Add(new DebugChange(CurrentState.GetType().Name, Time.unscaledTime, Time.frameCount));
        }
    }
}
#endif
