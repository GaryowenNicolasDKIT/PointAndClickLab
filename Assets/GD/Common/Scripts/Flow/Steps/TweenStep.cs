using System;
using System.Collections;
using DG.Tweening;

namespace GD.Flow
{
    /// <summary>
    /// Creates a tween when this step starts, and waits for it to finish.
    /// </summary>
    public class TweenStep : IFlowStep
    {
        private readonly Func<Tween> createTween;

        public TweenStep(Func<Tween> createTween)
        {
            this.createTween = createTween;
        }

        public override string ToString()
        {
            return "Tween, and wait for it to finish";
        }

        public IEnumerator Run()
        {
            // Created here, not when the sequence is built, because a tween starts at creation.
            Tween tween = createTween();
            yield return tween.WaitForCompletion();
        }
    }
}
