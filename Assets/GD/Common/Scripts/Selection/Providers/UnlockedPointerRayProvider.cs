using UnityEngine;
using UnityEngine.InputSystem;

namespace GD
{
    /// <summary>
    /// Provides a ray from the camera through the pointer, but only while the cursor is unlocked,
    /// for example while an inventory is open. While the cursor is locked, it provides no ray,
    /// so nothing is selected.
    /// </summary>
    [AddComponentMenu("GD/Selection/Providers/Unlocked Pointer Ray Provider")]
    public class UnlockedPointerRayProvider : MonoBehaviour, IRayProvider
    {
        [SerializeField, Tooltip("Camera used to create the ray. Defaults to Camera.main if left empty")]
        private Camera viewCamera;

        [SerializeField, Tooltip("A Vector2 screen position action, for example UI/Point in InputSystem_Actions")]
        private InputActionReference pointAction;

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;

            if (viewCamera == null || pointAction == null)
                Debug.LogError($"{name}: UnlockedPointerRayProvider needs a camera and a point action.", this);
        }

        private void OnEnable()
        {
            // The UI input module shares this action, so this component enables it but never disables it.
            if (pointAction != null)
                pointAction.action.Enable();
        }

        public bool TryCreateRay(out Ray ray)
        {
            // A locked cursor means first person look is active, so the UI is not being pointed at.
            if (Cursor.lockState == CursorLockMode.Locked || viewCamera == null || pointAction == null)
            {
                ray = default;
                return false;
            }

            Vector2 screenPosition = pointAction.action.ReadValue<Vector2>();
            ray = viewCamera.ScreenPointToRay(screenPosition);
            return true;
        }
    }
}
