using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="GameObjectEventChannel"/> is raised,
    /// passing the raised GameObject to each response, for example to highlight the enemy that was defeated.
    /// </summary>
    [AddComponentMenu("GD/Events/Game Object Event Channel Listener")]
    public class GameObjectEventChannelListener : EventChannelListener<GameObjectEventChannel, GameObject>
    {
    }
}
