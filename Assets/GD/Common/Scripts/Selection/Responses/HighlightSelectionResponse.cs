using UnityEngine;

namespace GD
{
    /// <summary>
    /// Swaps the selected object's material for a highlight material, and restores it on deselect.
    /// </summary>
    /// <remarks>
    /// Only the first material slot is swapped, so objects with several materials are only
    /// partly highlighted. A MaterialPropertyBlock could tint the object without swapping
    /// materials, but it makes the renderer incompatible with the URP SRP Batcher, so measure
    /// both before choosing.
    /// </remarks>
    [AddComponentMenu("GD/Selection/Responses/Highlight Selection Response")]
    public class HighlightSelectionResponse : MonoBehaviour, ISelectionResponse
    {
        #region Fields

        [SerializeField]
        private Material highlightMaterial;

        // The renderer currently highlighted, and the material it had before.
        private Renderer highlightedRenderer;
        private Material originalMaterial;

        #endregion Fields

        #region Core Methods

        public void OnSelect(Transform selection)
        {
            if (!selection.TryGetComponent(out Renderer renderer))
                return;

            highlightedRenderer = renderer;

            // sharedMaterial swaps the reference only. Using .material would create
            // a new material copy on every selection, and those copies are never freed.
            originalMaterial = renderer.sharedMaterial;
            renderer.sharedMaterial = highlightMaterial;
        }

        public void OnDeselect(Transform selection)
        {
            // The highlighted object may have been destroyed since it was selected.
            if (highlightedRenderer != null)
                highlightedRenderer.sharedMaterial = originalMaterial;

            highlightedRenderer = null;
            originalMaterial = null;
        }

        #endregion Core Methods
    }
}
