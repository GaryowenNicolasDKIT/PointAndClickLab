using UnityEngine;

namespace GD.Cameras
{
    /// <summary>
    /// Shakes every listening camera with an impulse.
    /// </summary>
    [CreateAssetMenu(fileName = "ImpulseCameraEffect", menuName = "GD/Camera/Impulse Effect")]
    public class ImpulseCameraEffect : CameraEffect
    {
        [SerializeField, Min(0f), Tooltip("Strength of the impulse")]
        private float force = 1f;

        public override void Play(ICameraRig rig)
        {
            rig.ImpulseSource.GenerateImpulseWithForce(force);
        }
    }
}
