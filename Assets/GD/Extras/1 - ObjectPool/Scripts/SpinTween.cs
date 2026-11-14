using DG.Tweening;
using UnityEngine;

namespace GD.Extras
{
    /// <summary>
    /// Spins this object around a local axis forever, at a steady speed.
    /// Starts when enabled and stops when disabled, so it works on pooled objects.
    /// </summary>
    public class SpinTween : MonoBehaviour
    {
        [SerializeField, Tooltip("Local axis to spin around. Up gives the usual pickup turn")]
        private Vector3 axis = Vector3.up;

        [SerializeField, Min(0.1f), Tooltip("Seconds for one full turn")]
        private float secondsPerTurn = 2f;

        private Tween spin;

        private void OnEnable()
        {
            // Add 360 degrees, then restart from the same pose. Linear, because a spin that eases looks like it stutters.
            spin = transform
                .DOLocalRotate(axis.normalized * 360f, secondsPerTurn, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        private void OnDisable()
        {
            spin?.Kill();
        }
    }
}
