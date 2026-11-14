using GD.Types;
using UnityEngine;

namespace GD.Cameras
{
    /// <summary>
    /// Carries camera effect requests from anywhere in the game to the CameraManager.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraEffectEventChannel", menuName = "GD/Camera/Camera Effect Event Channel")]
    public class CameraEffectEventChannel : EventChannel<CameraEffect>
    {
    }
}
