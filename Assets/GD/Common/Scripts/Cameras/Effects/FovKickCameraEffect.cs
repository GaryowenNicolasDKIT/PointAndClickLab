using DG.Tweening;
using Unity.Cinemachine;
using GD.Types;
using UnityEngine;

namespace GD.Cameras
{
    /// <summary>
    /// Briefly widens the live camera's field of view, then returns it to rest.
    /// </summary>
    [CreateAssetMenu(fileName = "FovKickCameraEffect", menuName = "GD/Camera/FOV Kick Effect")]
    public class FovKickCameraEffect : CameraEffect
    {
        [SerializeField, Tooltip("Degrees added to the field of view at the peak")]
        private float kick = 6f;

        [SerializeField, Tooltip("Timing of the move to the peak")]
        private TweenProfile attackProfile;

        [SerializeField, Tooltip("Timing of the return to rest")]
        private TweenProfile releaseProfile;

        public override void Play(ICameraRig rig)
        {
            CinemachineCamera camera = rig.ActiveCamera;

            // The Brain can be mid-change, or showing something other than a CinemachineCamera.
            if (camera == null)
                return;

            float resting = rig.GetRestingFieldOfView(camera);

            // The newest kick replaces any kick still running on this camera.
            DOTween.Kill(camera);

            DOTween.Sequence()
                .Append(attackProfile.Apply(TweenFieldOfView(camera, resting + kick, attackProfile.Duration)))
                .Append(releaseProfile.Apply(TweenFieldOfView(camera, resting, releaseProfile.Duration)))
                .SetTarget(camera)
                .SetLink(camera.gameObject);
        }

        private static Tween TweenFieldOfView(CinemachineCamera camera, float fieldOfView, float duration)
        {
            return DOTween.To(() => camera.Lens.FieldOfView, value => camera.Lens.FieldOfView = value,
                fieldOfView, duration);
        }
    }
}
