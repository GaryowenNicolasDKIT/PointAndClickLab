using UnityEngine;

namespace GD.Inventory
{
    /// <summary>A request to add or use an amount of one item, and the object that asked.</summary>
    public readonly struct ItemRequest
    {
        public ItemRequest(ItemDefinition item, int quantity, GameObject source)
        {
            Item = item;
            Quantity = quantity;
            Source = source;
        }

        public ItemDefinition Item { get; }

        public int Quantity { get; }

        public GameObject Source { get; }

        // Used by the channel's Log Raises option.
        public override string ToString() =>
            $"{Quantity} x {(Item != null ? Item.name : "no item")} from {(Source != null ? Source.name : "nothing")}";
    }
}
