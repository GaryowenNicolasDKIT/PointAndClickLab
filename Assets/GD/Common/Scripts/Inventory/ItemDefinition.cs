using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// Describes one kind of item: what it is called, how it looks in a slot and how many fit in a stack.
    /// The asset itself identifies the item, so code compares assets, not names.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Item", menuName = "GD/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Item";

        [SerializeField, Tooltip("Shown in the slot. Leave empty to show a plain square in Colour")]
        private Sprite icon;

        [SerializeField] private Color colour = Color.white;

        [SerializeField, Min(1), Tooltip("The most of this item the player can hold")]
        private int maxStack = 10;

        public string DisplayName => displayName;

        public Sprite Icon => icon;

        public Color Colour => colour;

        public int MaxStack => maxStack;
    }
}
