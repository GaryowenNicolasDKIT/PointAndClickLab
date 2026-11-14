using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// A fixed list of steps that runs in order. Can be run many times.
    /// </summary>
    public class FlowSequence
    {
        private readonly IFlowStep[] steps;

        public FlowSequence(IFlowStep[] steps)
        {
            this.steps = steps;
        }

        /// <summary>The steps, in order, so that a debugging tool can list them.</summary>
        public IReadOnlyList<IFlowStep> Steps => steps;

        /// <summary>The step running now, or -1 before the first run and after a run finishes.</summary>
        public int CurrentStepIndex { get; private set; } = -1;

        /// <summary>When the current step started, in unscaled time.</summary>
        public float CurrentStepStartTime { get; private set; }

        public IEnumerator Run()
        {
            for (int i = 0; i < steps.Length; i++)
            {
                CurrentStepIndex = i;
                CurrentStepStartTime = Time.unscaledTime;
                yield return steps[i].Run();
            }

            CurrentStepIndex = -1;
        }
    }
}
