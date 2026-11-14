using UnityEngine;

namespace GD
{
    /// <summary>
    /// Something the player can use by selecting it and pressing Interact: an item, a door, a switch.
    /// </summary>
    public interface IInteractable
    {
        /// <param name="interactor">The object doing the interacting, usually the player.</param>
        void Interact(GameObject interactor);
    }
}
