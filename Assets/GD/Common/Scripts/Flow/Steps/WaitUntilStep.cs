using System;
using System.Collections;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// Waits until a condition becomes true.
    /// </summary>
    public class WaitUntilStep : IFlowStep
    {
        private readonly Func<bool> condition;

        public WaitUntilStep(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override string ToString()
        {
            return $"Wait until {DelegateNames.Describe(condition)}";
        }

        public IEnumerator Run()
        {
            yield return new WaitUntil(condition);
        }
    }
}
