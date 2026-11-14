using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a Vector3 with each raise, for example the position of a reached checkpoint.
    /// </summary>
    [CreateAssetMenu(fileName = "Vector3EventChannel", menuName = "GD/Events/Vector3", order = 5)]
    public class Vector3EventChannel : EventChannel<Vector3>
    {
    }
}
