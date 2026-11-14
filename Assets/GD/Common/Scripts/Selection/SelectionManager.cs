using UnityEngine;

namespace GD
{
    /// <summary>
    /// Runs the selection pipeline each frame: the provider creates a ray, the selector
    /// tests it, and every response on this GameObject is told when the selection changes.
    /// The provider, selector and responses are found on this GameObject, so any of them
    /// can be swapped in the Inspector without changing this class.
    /// </summary>
    [AddComponentMenu("GD/Selection/Selection Manager")]
    public class SelectionManager : MonoBehaviour
    {
        #region Fields

        private IRayProvider rayProvider;
        private ISelector selector;
        private ISelectionResponse[] responses;

        private Transform currentSelection;

        #endregion Fields

        #region Properties

        /// <summary>The selected object, or null when nothing is selected.</summary>
        public Transform CurrentSelection => currentSelection;

        #endregion Properties

        #region Unity Messages

        private void Awake()
        {
            // Interfaces cannot be serialized fields, so the parts are found by component lookup.
            rayProvider = GetComponent<IRayProvider>();
            selector = GetComponent<ISelector>();
            responses = GetComponents<ISelectionResponse>();

            if (rayProvider == null || selector == null)
            {
                Debug.LogError($"{name}: SelectionManager needs an IRayProvider and an ISelector on the same GameObject.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Transform newSelection = null;

            if (rayProvider.TryCreateRay(out Ray ray) && selector.TrySelect(ray, out SelectionHit hit))
                newSelection = hit.Transform;

            ChangeSelection(newSelection);
        }

        private void OnDisable()
        {
            // Clear any highlight left behind when the manager is switched off.
            ChangeSelection(null);
        }

        #endregion Unity Messages

        #region Core Methods

        private void ChangeSelection(Transform newSelection)
        {
            // Responses run only when the selection changes, not every frame.
            if (newSelection == currentSelection)
                return;

            if (currentSelection != null)
            {
                foreach (ISelectionResponse response in responses)
                    response.OnDeselect(currentSelection);
            }

            currentSelection = newSelection;

            if (currentSelection != null)
            {
                foreach (ISelectionResponse response in responses)
                    response.OnSelect(currentSelection);
            }
        }

        #endregion Core Methods
    }
}
