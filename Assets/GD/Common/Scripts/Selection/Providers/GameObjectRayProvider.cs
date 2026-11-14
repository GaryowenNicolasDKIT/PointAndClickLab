using UnityEngine;

namespace GD
{
    /// <summary>
    /// Provides a ray that starts at a target object and points along its forward axis,
    /// for example from a first-person camera or a turret barrel.
    /// </summary>
    [AddComponentMenu("GD/Selection/Providers/Game Object Ray Provider")]
    public class GameObjectRayProvider : MonoBehaviour, IRayProvider
    {
        #region Fields

        [SerializeField]
        [Tooltip("Object the ray starts from. Defaults to this GameObject if left empty")]
        private Transform target;

        [Header("Debug Gizmo Properties")]
        [SerializeField]
        private Color rayColor = Color.yellow;

        [SerializeField]
        [Range(0.1f, 100)]
        private float rayLength = 10;

        #endregion Fields

        #region Properties

        private Transform Origin => target != null ? target : transform;

        #endregion Properties

        #region Unity Messages

        private void OnDrawGizmos()
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(Origin.position, Origin.position + Origin.forward * rayLength);
        }

        #endregion Unity Messages

        #region Core Methods

        public bool TryCreateRay(out Ray ray)
        {
            ray = new Ray(Origin.position, Origin.forward);
            return true;
        }

        #endregion Core Methods
    }
}
