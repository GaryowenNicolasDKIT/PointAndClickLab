using DG.Tweening;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Reusable tween timing: a duration, a delay and either a named ease or a drawn curve.
    /// </summary>
    /// <remarks>
    /// The asset holds behaviour as well as data, because <see cref="Apply{T}"/> decides how the
    /// settings reach a tween. Code using a profile does not need to know which ease it holds.
    /// </remarks>
    [CreateAssetMenu(fileName = "TweenProfile", menuName = "GD/Tweens/Tween Profile")]
    public class TweenProfile : ScriptableObject
    {
        #region Fields

        [SerializeField, Min(0f)]
        [Tooltip("Seconds the tween takes")]
        private float duration = 0.25f;

        [SerializeField, Min(0f)]
        [Tooltip("Seconds to wait before it starts")]
        private float delay;

        [SerializeField]
        [Tooltip("Use the curve below instead of the named ease")]
        private bool useCurve;

        [SerializeField]
        private Ease ease = Ease.OutCubic;

        [SerializeField]
        private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion Fields

        #region Properties

        /// <summary>Seconds the tween takes. Pass this when you create the tween.</summary>
        public float Duration => duration;

        #endregion Properties

        #region Core Methods

        /// <summary>Applies this profile's delay and easing, and returns the tween for chaining.</summary>
        public T Apply<T>(T tween) where T : Tween
        {
            tween.SetDelay(delay);
            return useCurve ? tween.SetEase(curve) : tween.SetEase(ease);
        }

        #endregion Core Methods
    }
}
