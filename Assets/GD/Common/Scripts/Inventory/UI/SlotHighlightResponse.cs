using UnityEngine;
using UnityEngine.UI;

namespace GD.Inventory
{
    /// <summary>
    /// Tints the selected slot's Image, and restores its colour when it is deselected.
    /// The UI equivalent of HighlightSelectionResponse, which swaps a Renderer's material.
    /// </summary>
    [AddComponentMenu("GD/Inventory/UI/Slot Highlight Response")]
    public class SlotHighlightResponse : MonoBehaviour, ISelectionResponse
    {
        [SerializeField] private Color highlightColour = new Color(1f, 0.85f, 0.3f);

        // The image currently tinted, and the colour it had before.
        private Image highlightedImage;
        private Color originalColour;

        public void OnSelect(Transform selection)
        {
            if (!selection.TryGetComponent(out Image image))
                return;

            highlightedImage = image;
            originalColour = image.color;
            image.color = highlightColour;
        }

        public void OnDeselect(Transform selection)
        {
            if (highlightedImage != null)
                highlightedImage.color = originalColour;

            highlightedImage = null;
        }
    }
}
