using System;
using System.Collections.Generic;

namespace GD.States
{
    /// <summary>
    /// Holds a current state and the transitions between states. Knows nothing about what
    /// any state does, so it works for any object with states.
    /// </summary>
    public partial class StateMachine
    {
        private readonly struct Transition
        {
            public readonly IState To;
            public readonly Func<bool> Condition;

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }

        private static readonly List<Transition> NoTransitions = new();

        private readonly Dictionary<IState, List<Transition>> transitions = new();
        private readonly List<Transition> anyTransitions = new();

        private List<Transition> currentTransitions = NoTransitions;

        /// <param name="owner">Shown in the State and Flow Viewer. Usually <c>this</c>.</param>
        public StateMachine(object owner = null)
        {
            Owner = owner;
            RegisterForDebugging();
        }

        /// <summary>The object this machine belongs to, if one was given.</summary>
        public object Owner { get; }

        /// <summary>The state running now, or null before the first SetState.</summary>
        public IState CurrentState { get; private set; }

        /// <summary>Raised after each change of state, with the new state.</summary>
        public event Action<IState> StateChanged;

        /// <summary>Adds a transition. Transitions are checked in the order they are added.</summary>
        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            if (!transitions.TryGetValue(from, out List<Transition> list))
            {
                list = new List<Transition>();
                transitions[from] = list;
            }

            list.Add(new Transition(to, condition));
        }

        /// <summary>Adds a transition from every state. These are checked first.</summary>
        public void AddAnyTransition(IState to, Func<bool> condition)
        {
            anyTransitions.Add(new Transition(to, condition));
        }

        /// <summary>Exits the current state and enters a new one.</summary>
        public void SetState(IState state)
        {
            if (state == CurrentState)
                return;

            CurrentState?.Exit();
            CurrentState = state;

            if (!transitions.TryGetValue(state, out currentTransitions))
                currentTransitions = NoTransitions;

            CurrentState.Enter();
            RecordStateChange();
            StateChanged?.Invoke(CurrentState);
        }

        /// <summary>Changes state if a condition is true, then ticks the current state.</summary>
        public void Tick()
        {
            if (TryFindNextState(anyTransitions, out IState next) || TryFindNextState(currentTransitions, out next))
                SetState(next);

            CurrentState?.Tick();
        }

        // Implemented in StateMachine.Debug.cs in the Editor, and removed by the compiler in a build.
        partial void RegisterForDebugging();
        partial void RecordStateChange();

        private static bool TryFindNextState(List<Transition> list, out IState next)
        {
            foreach (Transition transition in list)
            {
                if (transition.Condition())
                {
                    next = transition.To;
                    return true;
                }
            }

            next = null;
            return false;
        }
    }
}
