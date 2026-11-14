using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="BoolEventChannel"/> is raised,
    /// passing the raised bool to each response, for example to switch a light on or off.
    /// </summary>
    [AddComponentMenu("GD/Events/Bool Event Channel Listener")]
    public class BoolEventChannelListener : EventChannelListener<BoolEventChannel, bool>
    {
    }
}
