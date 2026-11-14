using System;
using System.Collections.Generic;
using DG.Tweening;
using GD.Types;

namespace GD.Flow
{
    /// <summary>
    /// Describes a flow through chained calls, then creates it with Build.
    /// </summary>
    public class FlowBuilder
    {
        private readonly List<IFlowStep> steps = new();

        public FlowBuilder Raise(VoidEventChannel channel)
        {
            return Add(new ActionStep(channel.Raise, $"Raise {channel.name}"));
        }

        public FlowBuilder Raise<T>(EventChannel<T> channel, T value)
        {
            return Add(new ActionStep(() => channel.Raise(value), $"Raise {channel.name}: {value}"));
        }

        public FlowBuilder Do(Action action)
        {
            return Add(new ActionStep(action));
        }

        public FlowBuilder Wait(float seconds)
        {
            return Add(new WaitStep(seconds, false));
        }

        public FlowBuilder WaitRealtime(float seconds)
        {
            return Add(new WaitStep(seconds, true));
        }

        public FlowBuilder WaitFor(VoidEventChannel channel)
        {
            return Add(new WaitForChannelStep(channel));
        }

        public FlowBuilder WaitUntil(Func<bool> condition)
        {
            return Add(new WaitUntilStep(condition));
        }

        public FlowBuilder Tween(Func<Tween> createTween)
        {
            return Add(new TweenStep(createTween));
        }

        public FlowSequence Build()
        {
            // A copy, so that using this builder again cannot change a finished sequence.
            return new FlowSequence(steps.ToArray());
        }

        /// <summary>Adds any step, including your own step types, with no edit to this class.</summary>
        public FlowBuilder Add(IFlowStep step)
        {
            steps.Add(step);
            return this;
        }
    }
}
