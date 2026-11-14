using System.Collections.Generic;
using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// Maps each item to its pre-assigned slot and updates that slot when the item's count changes.
    /// Slots are found among this object's children, so adding a slot needs no code.
    /// </summary>
    [AddComponentMenu("GD/Inventory/UI/Inventory Panel")]
    public class InventoryPanel : MonoBehaviour
    {
        private readonly Dictionary<ItemDefinition, InventorySlotUI> slotsByItem =
            new Dictionary<ItemDefinition, InventorySlotUI>();

        private void Awake()
        {
            foreach (InventorySlotUI slot in GetComponentsInChildren<InventorySlotUI>(true))
            {
                if (slot.Item == null)
                {
                    Debug.LogWarning($"{slot.name}: no item assigned, so this slot is ignored.", slot);
                    continue;
                }

                // TryAdd fails when two slots are assigned the same item.
                if (!slotsByItem.TryAdd(slot.Item, slot))
                    Debug.LogWarning($"{slot.name}: {slot.Item.DisplayName} already has a slot, so this one is ignored.", slot);
            }
        }

        /// <summary>Called by a listener on the Item Count Changed channel.</summary>
        public void ShowCount(ItemCount change)
        {
            if (slotsByItem.TryGetValue(change.Item, out InventorySlotUI slot))
                slot.SetCount(change.Count);
            else
                Debug.LogWarning($"{name}: no slot for {change.Item.DisplayName}.", this);
        }
    }
}
