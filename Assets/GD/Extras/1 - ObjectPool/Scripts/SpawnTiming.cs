using UnityEngine;

namespace GD.Extras
{
    /// <summary>
    /// Strategy for when the next spawn happens. <see cref="SpawnManager"/> depends only on this
    /// class, so a new rule is a new subclass and asset, with no change to the manager.
    /// </summary>
    public abstract class SpawnTiming : ScriptableObject
    {
        /// <summary>Seconds to wait before the next spawn.</summary>
        public abstract float GetNextDelay();
    }
}
