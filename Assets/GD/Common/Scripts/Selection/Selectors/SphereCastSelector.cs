using UnityEngine;

namespace GD
{
    /// <summary>
    /// Sweeps a sphere along the ray and selects the first object it hits, if that object is tagged.
    /// The sphere widens the area the ray covers, so selection is more forgiving than
    /// <see cref="RayCastSelector"/> (useful for small or fast-moving targets, or gamepad aiming).
    /// </summary>
    /// <remarks>
    /// A sphere cast does not detect colliders that already overlap the sphere at the ray origin.
    /// Keep the ray origin clear of selectable objects, or they cannot be selected at close range.
    /// </remarks>
    [AddComponentMenu("GD/Selection/Selectors/Sphere Cast Selector")]
    public class SphereCastSelector : BaseCastSelector
    {
        #region Fields

        [Header("Debug Gizmo Properties")]
        [SerializeField]
        [ColorUsage(false)]
        private Color sphereColor = Color.yellow;

        [SerializeField]
        [Range(0.1f, 4)]
        [Tooltip("Radius of the swept sphere. Larger values make selection more forgiving")]
        private float sphereRadius = 0.5f;

        [SerializeField]
        [Range(0, 1)]
        [Tooltip("Where along maxDistance the debug sphere is drawn")]
        private float sphereCastPositionAsProportion;

        #endregion Fields

        #region Unity Messages

        private void OnDrawGizmos()
        {
            Gizmos.color = sphereColor;
            Vector3 pointOnRay = lastRay.origin + lastRay.direction * (maxDistance * sphereCastPositionAsProportion);
            Gizmos.DrawWireSphere(pointOnRay, sphereRadius);
        }

        #endregion Unity Messages

        #region Core Methods

        protected override bool Cast(Ray ray, out RaycastHit hit)
        {
            return Physics.SphereCast(ray, sphereRadius, out hit, maxDistance, layerMask);
        }

        #endregion Core Methods
    }
}
