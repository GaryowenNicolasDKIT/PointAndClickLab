using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// A GameObject shared between systems through an asset, for example the player or the current target.
    /// </summary>
    /// <remarks>
    /// The initial value can only be an asset such as a prefab, because an asset cannot store a
    /// reference to an object inside a scene. Set scene objects at runtime instead, for example
    /// from the player's Awake.
    /// </remarks>
    [CreateAssetMenu(fileName = "GameObjectVariable", menuName = "GD/Variables/Game Object", order = 6)]
    public class GameObjectVariable : ScriptableVariable<GameObject>
    {
    }
}
