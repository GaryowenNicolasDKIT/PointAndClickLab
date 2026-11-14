using UnityEngine;

namespace GD.Cameras
{
    /// <summary>
    /// Base class for a camera effect asset. Holds settings only, because assets are shared.
    /// </summary>
    public abstract class CameraEffect : ScriptableObject
    {
        public abstract void Play(ICameraRig rig);
    }
}
