using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace GD.Cameras
{
    /// <summary>
    /// Plays requested camera effects, and gives them access to the live camera.
    /// </summary>
    [AddComponentMenu("GD/Camera/Camera Manager")]
    public class CameraManager : MonoBehaviour, ICameraRig
    {
        [SerializeField] private CameraEffectEventChannel effectChannel;
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        // Each camera's field of view, recorded before any effect changes it.
        private readonly Dictionary<CinemachineCamera, float> restingFieldOfViews = new();

        public CinemachineCamera ActiveCamera => brain.ActiveVirtualCamera as CinemachineCamera;

        public CinemachineImpulseSource ImpulseSource => impulseSource;

        public float GetRestingFieldOfView(CinemachineCamera camera)
        {
            if (!restingFieldOfViews.TryGetValue(camera, out float fieldOfView))
            {
                fieldOfView = camera.Lens.FieldOfView;
                restingFieldOfViews[camera] = fieldOfView;
            }

            return fieldOfView;
        }

        private void OnEnable()
        {
            effectChannel.Raised += HandleEffect;
        }

        private void OnDisable()
        {
            effectChannel.Raised -= HandleEffect;
        }

        private void HandleEffect(CameraEffect effect)
        {
            effect.Play(this);
        }
    }
}
