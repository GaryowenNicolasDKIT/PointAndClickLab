using UnityEngine;

namespace GD.Inventory
{
    /// <summary>The new total the player holds of one item.</summary>
    public readonly struct ItemCount
    {
        public ItemCount(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }

        public ItemDefinition Item { get; }

        public int Count { get; }

        public override string ToString() => $"{(Item != null ? Item.name : "no item")} = {Count}";
    }
}
