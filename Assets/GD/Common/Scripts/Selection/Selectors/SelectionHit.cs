using UnityEngine;

namespace GD
{
    /// <summary>
    /// What a selector found: the selected object, and where it was hit when that is known.
    /// </summary>
    /// <remarks>
    /// Physics selectors build it from a <see cref="RaycastHit"/> with <see cref="FromRaycastHit"/>.
    /// Selectors that do not use physics, such as a UI selector, build it from their own results.
    /// <see cref="ISelector"/> returns this type rather than <see cref="RaycastHit"/> because only a
    /// physics cast can create a RaycastHit, so an interface that demanded one could only ever be
    /// implemented with physics.
    /// </remarks>
    public readonly struct SelectionHit
    {
        #region Properties

        /// <summary>The selected object.</summary>
        public Transform Transform { get; }

        /// <summary>The world-space point that was hit, or <see cref="Vector3.zero"/> if not known.</summary>
        public Vector3 Point { get; }

        /// <summary>The surface normal at <see cref="Point"/>, or <see cref="Vector3.zero"/> if not known.</summary>
        public Vector3 Normal { get; }

        /// <summary>The distance from the ray origin to <see cref="Point"/>, or 0 if not known.</summary>
        public float Distance { get; }

        #endregion Properties

        #region Constructors

        public SelectionHit(Transform transform, Vector3 point, Vector3 normal, float distance)
        {
            Transform = transform;
            Point = point;
            Normal = normal;
            Distance = distance;
        }

        #endregion Constructors

        #region Core Methods

        /// <summary>Copies the selected object and hit details from a physics hit.</summary>
        public static SelectionHit FromRaycastHit(RaycastHit hit)
        {
            return new SelectionHit(hit.transform, hit.point, hit.normal, hit.distance);
        }

        #endregion Core Methods
    }
}
