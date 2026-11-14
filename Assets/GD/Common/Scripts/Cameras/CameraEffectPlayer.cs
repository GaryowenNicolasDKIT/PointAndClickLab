using UnityEngine;

namespace GD.Cameras
{
    /// <summary>
    /// Requests a camera effect. Wire Play to any Inspector event.
    /// </summary>
    [AddComponentMenu("GD/Camera/Camera Effect Player")]
    public class CameraEffectPlayer : MonoBehaviour
    {
        [SerializeField] private CameraEffect effect;
        [SerializeField] private CameraEffectEventChannel effectChannel;

        [ContextMenu("Play")]
        public void Play()
        {
            effectChannel.Raise(effect);
        }
    }
}
