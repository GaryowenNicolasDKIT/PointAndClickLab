using TMPro;
using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// Shows the name and count of the slot under the pointer. Called by GameObjectEventChannelListeners.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("GD/Inventory/UI/Item Tooltip")]
    public class ItemTooltip : MonoBehaviour
    {
        private TMP_Text label;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            label.text = string.Empty;
        }

        public void Show(GameObject slotObject)
        {
            if (!slotObject.TryGetComponent(out InventorySlotUI slot))
                return;

            label.text = $"{slot.Item.DisplayName} ({slot.Count}/{slot.Item.MaxStack})";
        }

        // The parameter is unused, but it lets a GameObjectEventChannelListener call this method.
        public void Clear(GameObject slotObject)
        {
            label.text = string.Empty;
        }
    }
}
