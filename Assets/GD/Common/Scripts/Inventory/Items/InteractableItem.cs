using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// An item collected by selecting it and pressing Interact. Needs the Selectable tag and a collider.
    /// </summary>
    [AddComponentMenu("GD/Inventory/Items/Interactable Item")]
    public class InteractableItem : WorldItem, IInteractable
    {
        public void Interact(GameObject interactor)
        {
            // Any interactor may take an item. A locked door would check the interactor here.
            RequestCollect();
        }
    }
}
