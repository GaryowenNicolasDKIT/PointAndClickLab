using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="IntEventChannel"/> is raised,
    /// passing the raised int to each response, for example to update a score display.
    /// </summary>
    [AddComponentMenu("GD/Events/Int Event Channel Listener")]
    public class IntEventChannelListener : EventChannelListener<IntEventChannel, int>
    {
    }
}
