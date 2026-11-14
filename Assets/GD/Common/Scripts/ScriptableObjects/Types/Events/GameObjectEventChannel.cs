using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a GameObject with each raise, for example the enemy that was defeated.
    /// </summary>
    [CreateAssetMenu(fileName = "GameObjectEventChannel", menuName = "GD/Events/Game Object", order = 6)]
    public class GameObjectEventChannel : EventChannel<GameObject>
    {
    }
}
