using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="SceneEventChannel"/> is raised,
    /// passing the raised SceneDefinition to each response, for example to show the name of the level being loaded.
    /// </summary>
    [AddComponentMenu("GD/Events/Scene Event Channel Listener")]
    public class SceneEventChannelListener : EventChannelListener<SceneEventChannel, SceneDefinition>
    {
    }
}
