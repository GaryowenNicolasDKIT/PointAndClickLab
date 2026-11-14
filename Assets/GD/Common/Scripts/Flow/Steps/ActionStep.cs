using System;
using System.Collections;

namespace GD.Flow
{
    /// <summary>
    /// Runs an action at once, then moves to the next step.
    /// </summary>
    public class ActionStep : IFlowStep
    {
        private readonly Action action;
        private readonly string description;

        public ActionStep(Action action, string description = null)
        {
            this.action = action;
            this.description = description;
        }

        public override string ToString()
        {
            return description ?? $"Do {DelegateNames.Describe(action)}";
        }

        public IEnumerator Run()
        {
            action();
            yield break;
        }
    }
}
