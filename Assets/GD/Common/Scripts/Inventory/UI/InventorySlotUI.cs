using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GD.Inventory
{
    /// <summary>
    /// One inventory slot, pre-assigned to an item in the Inspector. Shows the item's icon and count,
    /// and asks the inventory to use one when it is interacted with. Holds no inventory state of its own.
    /// </summary>
    [AddComponentMenu("GD/Inventory/UI/Inventory Slot UI")]
    public class InventorySlotUI : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("The item this slot shows")]
        private ItemDefinition item;

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countLabel;

        [SerializeField, Range(0f, 1f), Tooltip("Icon opacity while the player has none of the item")]
        private float emptyAlpha = 0.25f;

        [SerializeField, Tooltip("Raised to ask the inventory to use one of this item. Leave empty if the item cannot be used")]
        private ItemRequestEventChannel useRequested;

        public ItemDefinition Item => item;

        public int Count { get; private set; }

        private void Awake()
        {
            if (item == null || icon == null || countLabel == null)
            {
                Debug.LogError($"{name}: InventorySlotUI needs an item, an icon and a count label.", this);
                enabled = false;
                return;
            }

            icon.sprite = item.Icon;
            SetCount(0);
        }

        public void SetCount(int count)
        {
            Count = count;

            Color colour = item.Colour;
            colour.a = count > 0 ? 1f : emptyAlpha;
            icon.color = colour;

            countLabel.text = count > 0 ? count.ToString() : string.Empty;
        }

        public void Interact(GameObject interactor)
        {
            // The slot only asks; the inventory decides whether there is one to use.
            if (useRequested == null || Count == 0)
                return;

            useRequested.Raise(new ItemRequest(item, 1, interactor));
        }
    }
}
