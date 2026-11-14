using UnityEngine;

namespace GD.Extras
{
    /// <summary>One spawn every N seconds, for example a health pack every 10 seconds.</summary>
    [CreateAssetMenu(fileName = "FixedIntervalTiming", menuName = "GD/Extras/Spawning/Fixed Interval Timing")]
    public class FixedIntervalTiming : SpawnTiming
    {
        [SerializeField, Min(0.1f), Tooltip("Seconds between spawns")]
        private float seconds = 10f;

        public override float GetNextDelay() => seconds;
    }
}
