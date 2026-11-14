using UnityEngine;

namespace GD.Extras
{
    /// <summary>A spawn after a random wait in a range, so the player cannot learn the rhythm.</summary>
    [CreateAssetMenu(fileName = "RandomIntervalTiming", menuName = "GD/Extras/Spawning/Random Interval Timing")]
    public class RandomIntervalTiming : SpawnTiming
    {
        [SerializeField, Min(0.1f), Tooltip("Shortest wait between spawns, in seconds")]
        private float minSeconds = 3f;

        [SerializeField, Min(0.1f), Tooltip("Longest wait between spawns, in seconds")]
        private float maxSeconds = 8f;

        public override float GetNextDelay() => Random.Range(minSeconds, maxSeconds);
    }
}
