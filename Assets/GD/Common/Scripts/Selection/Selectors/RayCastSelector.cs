using UnityEngine;

namespace GD
{
    /// <summary>
    /// Casts a thin ray and selects the first object it hits, if that object is tagged.
    /// </summary>
    /// <remarks>
    /// The ray stops at the first collider on <c>layerMask</c>. An untagged wall in front of
    /// a selectable object blocks selection, which is intended: you cannot select through walls.
    /// </remarks>
    [AddComponentMenu("GD/Selection/Selectors/Ray Cast Selector")]
    public class RayCastSelector : BaseCastSelector
    {
        #region Fields

        [Header("Debug Gizmo Properties")]
        [SerializeField]
        [ColorUsage(false)]
        private Color rayColor = Color.yellow;

        #endregion Fields

        #region Unity Messages

        private void OnDrawGizmos()
        {
            Gizmos.color = rayColor;
            Gizmos.DrawLine(lastRay.origin, lastRay.origin + lastRay.direction * maxDistance);
        }

        #endregion Unity Messages

        #region Core Methods

        protected override bool Cast(Ray ray, out RaycastHit hit)
        {
            return Physics.Raycast(ray, out hit, maxDistance, layerMask);
        }

        #endregion Core Methods
    }
}
