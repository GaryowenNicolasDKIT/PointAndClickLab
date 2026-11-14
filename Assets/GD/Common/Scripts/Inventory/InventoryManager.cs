using System.Collections.Generic;
using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// Holds how many of each item the player has, applies the stack rule, and announces every change.
    /// Requests arrive through event channel listeners; replies and changes go out through channels.
    /// </summary>
    [AddComponentMenu("GD/Inventory/Inventory Manager")]
    public class InventoryManager : MonoBehaviour
    {
        #region Fields

        [Header("Replies")]
        [SerializeField, Tooltip("Raised with the request when a collect request is accepted")]
        private ItemRequestEventChannel itemAdded;

        [SerializeField, Tooltip("Raised with the request when a collect request is refused")]
        private ItemRequestEventChannel itemRejected;

        [Header("Changes")]
        [SerializeField, Tooltip("Raised with the new total whenever an item's count changes")]
        private ItemCountEventChannel countChanged;

        private readonly Dictionary<ItemDefinition, int> counts = new Dictionary<ItemDefinition, int>();

        #endregion Fields

        #region Properties

        /// <summary>Every item held and its count. Read-only, so the stack rule cannot be bypassed.</summary>
        public IReadOnlyDictionary<ItemDefinition, int> Counts => counts;

        #endregion Properties

        #region Unity Messages

        private void Awake()
        {
            if (itemAdded == null || itemRejected == null || countChanged == null)
            {
                Debug.LogError($"{name}: InventoryManager needs its reply and change channels.", this);
                enabled = false;
            }
        }

        #endregion Unity Messages

        #region Event Handlers

        /// <summary>Called by a listener on the Collect Requested channel.</summary>
        public void HandleCollectRequest(ItemRequest request)
        {
            // UnityEvents still call methods on a disabled component.
            if (!enabled)
                return;

            if (TryAdd(request.Item, request.Quantity))
                itemAdded.Raise(request);
            else
                itemRejected.Raise(request);
        }

        /// <summary>Called by a listener on the Use Requested channel.</summary>
        public void HandleUseRequest(ItemRequest request)
        {
            if (!enabled)
                return;

            TryRemove(request.Item, request.Quantity);
        }

        #endregion Event Handlers

        #region Core Methods

        public int GetCount(ItemDefinition item)
        {
            return item != null && counts.TryGetValue(item, out int count) ? count : 0;
        }

        public bool TryAdd(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            int current = GetCount(item);

            // All or nothing: a request that would overflow the stack is refused whole.
            if (current + quantity > item.MaxStack)
                return false;

            SetCount(item, current + quantity);
            return true;
        }

        public bool TryRemove(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            int current = GetCount(item);
            if (current < quantity)
                return false;

            SetCount(item, current - quantity);
            return true;
        }

        private void SetCount(ItemDefinition item, int count)
        {
            // Remove empty entries, so the dictionary only holds items the player actually has.
            if (count == 0)
                counts.Remove(item);
            else
                counts[item] = count;

            countChanged.Raise(new ItemCount(item, count));
        }

        #endregion Core Methods
    }
}
