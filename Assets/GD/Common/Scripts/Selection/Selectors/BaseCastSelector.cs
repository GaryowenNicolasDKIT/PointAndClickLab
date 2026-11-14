using UnityEngine;

namespace GD
{
    /// <summary>
    /// Base class for cast selectors. Holds the shared settings and decides whether a hit
    /// counts as a selection; each child class only decides what shape to cast.
    /// </summary>
    /// <remarks>
    /// <see cref="TrySelect"/> is a Template Method: the selection rule is fixed here,
    /// and the cast step is supplied by <see cref="Cast"/> in the child class.
    /// </remarks>
    public abstract class BaseCastSelector : MonoBehaviour, ISelector
    {
        #region Fields

        [SerializeField]
        [Tooltip("Tag used by selectable objects")]
        protected string selectableTag = "Selectable";

        [SerializeField]
        [Tooltip("Layers the cast can hit")]
        protected LayerMask layerMask = ~0;

        [SerializeField]
        [Range(0.01f, 500)]
        [Tooltip("Maximum distance (metres) over which selection is allowed")]
        protected float maxDistance = 50;

        // The last ray cast, kept so child classes can draw it as a gizmo.
        protected Ray lastRay;

        #endregion Fields

        #region Core Methods

        public bool TrySelect(Ray ray, out SelectionHit hit)
        {
            lastRay = ray;

            if (Cast(ray, out RaycastHit physicsHit) && physicsHit.transform.CompareTag(selectableTag))
            {
                hit = SelectionHit.FromRaycastHit(physicsHit);
                return true;
            }

            hit = default;
            return false;
        }

        /// <summary>
        /// Casts along <paramref name="ray"/> and returns true if anything on
        /// <see cref="layerMask"/> was hit within <see cref="maxDistance"/>.
        /// </summary>
        protected abstract bool Cast(Ray ray, out RaycastHit hit);

        #endregion Core Methods
    }
}
