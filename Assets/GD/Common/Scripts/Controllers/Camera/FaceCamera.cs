using UnityEngine;

/// <summary>
/// Rotates an object to face the main camera.
/// </summary>
[AddComponentMenu("GD/Camera/Face Camera")]
public class FaceCamera : MonoBehaviour
{
    private Transform _cameraTransform;

    #region Unity Methods

    /// <summary>
    /// Finds the main camera when the object is initialized.
    /// </summary>
    private void Start()
    {
        _cameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// Rotates the object to face the camera.
    /// </summary>
    private void LateUpdate()
    {
        if (_cameraTransform != null)
        {
            transform.forward = _cameraTransform.forward;
        }
    }

    #endregion
}