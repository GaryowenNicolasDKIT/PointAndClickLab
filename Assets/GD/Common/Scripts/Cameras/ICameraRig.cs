using Unity.Cinemachine;

namespace GD.Cameras
{
    /// <summary>
    /// What a camera effect may use. Effects depend on this rather than on CameraManager.
    /// </summary>
    public interface ICameraRig
    {
        /// <summary>The live camera, or null if the Brain has no CinemachineCamera live.</summary>
        CinemachineCamera ActiveCamera { get; }

        CinemachineImpulseSource ImpulseSource { get; }

        /// <summary>The camera's field of view when it is not being changed by an effect.</summary>
        float GetRestingFieldOfView(CinemachineCamera camera);
    }
}
