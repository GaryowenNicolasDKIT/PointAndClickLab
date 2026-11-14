using UnityEngine;

namespace GD.Audio
{
    /// <summary>
    /// A request to play a cue at a world position.
    /// </summary>
    public readonly struct AudioRequest
    {
        public readonly AudioCue Cue;
        public readonly Vector3 Position;

        public AudioRequest(AudioCue cue, Vector3 position)
        {
            Cue = cue;
            Position = position;
        }
    }
}
