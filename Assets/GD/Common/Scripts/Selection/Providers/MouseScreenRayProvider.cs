using UnityEngine;
using UnityEngine.InputSystem;

namespace GD
{
    /// <summary>
    /// Provides a ray from the camera through the pointer (mouse, pen or touch) position.
    /// </summary>
    [AddComponentMenu("GD/Selection/Providers/Mouse Screen Ray Provider")]
    public class MouseScreenRayProvider : MonoBehaviour, IRayProvider
    {
        #region Fields

        [SerializeField]
        [Tooltip("Camera used to create the ray. Defaults to Camera.main if left empty")]
        private Camera currentCamera;

        [SerializeField]
        [Tooltip("A Vector2 screen position action, for example UI/Point in InputSystem_Actions")]
        private InputActionReference pointAction;

        #endregion Fields

        #region Unity Messages

        private void Awake()
        {
            if (currentCamera == null)
                currentCamera = Camera.main;

            if (currentCamera == null || pointAction == null)
                Debug.LogError($"{name}: MouseScreenRayProvider needs a camera and a point action.", this);
        }

        private void OnEnable()
        {
            if (pointAction == null)
                return;

            // The UI input module may share this action, so this component
            // enables it but never disables it.
            pointAction.action.Enable();
        }

        #endregion Unity Messages

        #region Core Methods

        public bool TryCreateRay(out Ray ray)
        {
            // Missing references were reported in Awake. Returning false here means
            // the selection is cleared instead of an exception being thrown every frame.
            if (currentCamera == null || pointAction == null)
            {
                ray = default;
                return false;
            }

            Vector2 screenPosition = pointAction.action.ReadValue<Vector2>();
            ray = currentCamera.ScreenPointToRay(screenPosition);
            return true;
        }

        #endregion Core Methods
    }
}
