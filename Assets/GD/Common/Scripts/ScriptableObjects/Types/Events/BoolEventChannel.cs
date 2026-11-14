using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a bool with each raise, for example whether a switch was turned on or off.
    /// </summary>
    [CreateAssetMenu(fileName = "BoolEventChannel", menuName = "GD/Events/Bool", order = 1)]
    public class BoolEventChannel : EventChannel<bool>
    {
    }
}
