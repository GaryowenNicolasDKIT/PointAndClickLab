using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// Base class for an item in the level. Holds what the item is and how many, asks the inventory
    /// to take it, and removes it when the inventory accepts. Child classes decide what starts the request.
    /// </summary>
    public abstract class WorldItem : MonoBehaviour
    {
        #region Fields

        [Header("Item")]
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;

        [Header("Channels")]
        [SerializeField, Tooltip("Raised to ask the inventory to take this item")]
        private ItemRequestEventChannel collectRequested;

        [SerializeField, Tooltip("The inventory's reply when it takes an item")]
        private ItemRequestEventChannel itemAdded;

        [SerializeField, Tooltip("The inventory's reply when it refuses an item")]
        private ItemRequestEventChannel itemRejected;

        #endregion Fields

        #region Properties

        public ItemDefinition Item => item;

        public int Quantity => quantity;

        #endregion Properties

        #region Unity Messages

        private void Awake()
        {
            if (item == null || collectRequested == null || itemAdded == null || itemRejected == null)
            {
                Debug.LogError($"{name}: WorldItem needs an item and its three channels.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (itemAdded != null)
                itemAdded.Raised += OnItemAdded;

            if (itemRejected != null)
                itemRejected.Raised += OnItemRejected;
        }

        private void OnDisable()
        {
            if (itemAdded != null)
                itemAdded.Raised -= OnItemAdded;

            if (itemRejected != null)
                itemRejected.Raised -= OnItemRejected;
        }

        #endregion Unity Messages

        #region Core Methods

        /// <summary>
        /// Asks the inventory to take this item. The reply arrives before this method returns,
        /// because a channel calls its listeners immediately.
        /// </summary>
        protected void RequestCollect()
        {
            if (!enabled)
                return;

            collectRequested.Raise(new ItemRequest(item, quantity, gameObject));
        }

        /// <summary>Called when the inventory refuses this item. The item stays in the level.</summary>
        protected virtual void OnRejected()
        {
            Debug.Log($"{name}: no room for {item.DisplayName}.", this);
        }

        private void OnItemAdded(ItemRequest request)
        {
            // Every item hears every reply; act only on replies to this item's own request.
            if (request.Source != gameObject)
                return;

            Destroy(gameObject);
        }

        private void OnItemRejected(ItemRequest request)
        {
            if (request.Source != gameObject)
                return;

            OnRejected();
        }

        #endregion Core Methods
    }
}
