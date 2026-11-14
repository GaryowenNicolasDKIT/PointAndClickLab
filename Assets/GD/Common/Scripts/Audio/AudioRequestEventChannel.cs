using GD.Types;
using UnityEngine;

namespace GD.Audio
{
    /// <summary>
    /// Carries audio requests from anywhere in the game to whatever is listening.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioRequestEventChannel", menuName = "GD/Audio/Audio Request Event Channel")]
    public class AudioRequestEventChannel : EventChannel<AudioRequest>
    {
    }
}
