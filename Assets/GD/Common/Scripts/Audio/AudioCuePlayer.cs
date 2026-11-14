using UnityEngine;

namespace GD.Audio
{
    /// <summary>
    /// Requests a cue at this object's position. Wire Play to any Inspector event.
    /// </summary>
    [AddComponentMenu("GD/Audio/Audio Cue Player")]
    public class AudioCuePlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("The sound to play when Play is called")]
        private AudioCue cue;

        [SerializeField, Tooltip("Channel the AudioManager listens on. Use the same asset as the AudioManager")]
        private AudioRequestEventChannel requestChannel;

        [ContextMenu("Play")]
        public void Play()
        {
            requestChannel.Raise(new AudioRequest(cue, transform.position));
        }
    }
}
