using UnityEngine;

namespace GD
{
    /// <summary>
    /// Any class that implements this interface can provide the ray used for selection.
    /// </summary>
    /// <see cref="MouseScreenRayProvider"/>
    /// <see cref="GameObjectRayProvider"/>
    public interface IRayProvider
    {
        /// <summary>
        /// Creates this frame's ray. Returns false when no ray is available, for example
        /// when no pointer device is connected, so the caller clears the selection.
        /// </summary>
        bool TryCreateRay(out Ray ray);
    }
}
