using DG.Tweening;
using UnityEngine;

namespace GD.Extras
{
    /// <summary>
    /// Moves this object up and down forever, eased at each end so it seems to float.
    /// Starts when enabled and stops when disabled, so it works on pooled objects.
    /// </summary>
    /// <remarks>
    /// Put this on a child of the pickup, such as its mesh, not on the root. It moves in local
    /// space, and the pool moves the root to its spawn point after enabling it.
    /// </remarks>
    public class BobTween : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Metres above the resting position at the top of the bob")]
        private float height = 0.25f;

        [SerializeField, Min(0.1f), Tooltip("Seconds to move from the bottom to the top")]
        private float secondsUp = 0.8f;

        [SerializeField, Tooltip("InOutSine slows at the top and bottom, like something floating")]
        private Ease ease = Ease.InOutSine;

        [SerializeField, Tooltip("Start at a random point in the bob, so pickups do not move in step")]
        private bool randomStart = true;

        private Vector3 restingPosition;
        private Tween bob;

        private void Awake()
        {
            restingPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            // Start from rest each time, because a reused copy was disabled part-way through.
            transform.localPosition = restingPosition;

            // Yoyo plays up, then the same tween backwards, so the ease applies at both ends.
            bob = transform
                .DOLocalMoveY(restingPosition.y + height, secondsUp)
                .SetEase(ease)
                .SetLoops(-1, LoopType.Yoyo);

            // One full cycle is up and back down.
            if (randomStart)
                bob.Goto(Random.Range(0f, secondsUp * 2f), true);
        }

        private void OnDisable()
        {
            bob?.Kill();
        }
    }
}
