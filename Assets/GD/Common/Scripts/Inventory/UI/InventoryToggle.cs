using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GD.Inventory
{
    /// <summary>
    /// Opens and closes the inventory: unlocks the cursor and stops mouse look while it is open.
    /// Works through the Starter Assets input, so the controller is not edited.
    /// </summary>
    [AddComponentMenu("GD/Inventory/UI/Inventory Toggle")]
    public class InventoryToggle : MonoBehaviour
    {
        [SerializeField, Tooltip("The StarterAssetsInputs on the player")]
        private StarterAssetsInputs starterInputs;

        [SerializeField, Tooltip("Opens and closes the inventory. Owned by this component, so it is enabled here")]
        private InputAction toggleAction = new InputAction("ToggleInventory", InputActionType.Button, "<Keyboard>/tab");

        private bool isOpen;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (starterInputs == null)
            {
                Debug.LogError($"{name}: InventoryToggle needs the player's StarterAssetsInputs.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            toggleAction.Enable();
        }

        private void OnDisable()
        {
            toggleAction.Disable();
        }

        private void Update()
        {
            if (toggleAction.WasPressedThisFrame())
                SetOpen(!isOpen);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;

            // StarterAssetsInputs re-applies cursorLocked when the window regains focus, so keep it in step.
            starterInputs.cursorLocked = !open;

            // While false, StarterAssetsInputs ignores mouse look; clearing look stops the last value turning the view.
            starterInputs.cursorInputForLook = !open;
            if (open)
                starterInputs.LookInput(Vector2.zero);

            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }
    }
}
