using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a float with each raise, for example the damage from a hit.
    /// </summary>
    [CreateAssetMenu(fileName = "FloatEventChannel", menuName = "GD/Events/Float", order = 3)]
    public class FloatEventChannel : EventChannel<float>
    {
    }
}
