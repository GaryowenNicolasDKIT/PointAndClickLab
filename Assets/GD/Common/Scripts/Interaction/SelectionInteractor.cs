using UnityEngine;
using UnityEngine.InputSystem;

namespace GD
{
    /// <summary>
    /// When the interact action is pressed, calls Interact on the selected object if it is IInteractable.
    /// Sits beside a SelectionManager, so it works with any ray provider and selector.
    /// </summary>
    [RequireComponent(typeof(SelectionManager))]
    [AddComponentMenu("GD/Interaction/Selection Interactor")]
    public class SelectionInteractor : MonoBehaviour
    {
        [SerializeField, Tooltip("A button action, for example Player/Interact or UI/Click")]
        private InputActionReference interactAction;

        [SerializeField, Tooltip("Passed to Interact as the object doing the interacting, usually the player")]
        private GameObject interactor;

        private SelectionManager selectionManager;

        private void Awake()
        {
            selectionManager = GetComponent<SelectionManager>();

            if (interactAction == null)
            {
                Debug.LogError($"{name}: SelectionInteractor needs an interact action.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (interactAction != null)
                interactAction.action.Enable();
        }

        private void Update()
        {
            // WasPressedThisFrame reacts to the press itself, so it works although Player/Interact has a Hold interaction.
            if (!interactAction.action.WasPressedThisFrame())
                return;

            Transform selection = selectionManager.CurrentSelection;

            if (selection != null && selection.TryGetComponent(out IInteractable interactable))
                interactable.Interact(interactor != null ? interactor : gameObject);
        }
    }
}
