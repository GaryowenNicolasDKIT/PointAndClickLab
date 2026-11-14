using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="Vector3EventChannel"/> is raised,
    /// passing the raised Vector3 to each response, for example to move a marker to a reached checkpoint.
    /// </summary>
    [AddComponentMenu("GD/Events/Vector3 Event Channel Listener")]
    public class Vector3EventChannelListener : EventChannelListener<Vector3EventChannel, Vector3>
    {
    }
}
