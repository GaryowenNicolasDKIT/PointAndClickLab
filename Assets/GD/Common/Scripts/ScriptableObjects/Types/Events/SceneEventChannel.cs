using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a <see cref="SceneDefinition"/> with each raise, for example
    /// a request to load the next level.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneEventChannel", menuName = "GD/Events/Scene", order = 7)]
    public class SceneEventChannel : EventChannel<SceneDefinition>
    {
    }
}
