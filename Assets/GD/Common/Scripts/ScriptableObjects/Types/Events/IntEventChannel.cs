using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a int with each raise, for example the points from a collected pickup.
    /// </summary>
    [CreateAssetMenu(fileName = "IntEventChannel", menuName = "GD/Events/Int", order = 2)]
    public class IntEventChannel : EventChannel<int>
    {
    }
}
