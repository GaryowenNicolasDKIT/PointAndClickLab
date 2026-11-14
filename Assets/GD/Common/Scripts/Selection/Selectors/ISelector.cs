using UnityEngine;

namespace GD
{
    /// <summary>
    /// Any class that implements this interface decides what a ray selects.
    /// </summary>
    /// <see cref="RayCastSelector"/>
    /// <see cref="SphereCastSelector"/>
    public interface ISelector
    {
        /// <summary>
        /// Tests <paramref name="ray"/>. Returns true when a selectable object was found,
        /// with the details in <paramref name="hit"/>.
        /// </summary>
        bool TrySelect(Ray ray, out SelectionHit hit);
    }
}
