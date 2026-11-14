using DG.Tweening;
using GD.Types;
using UnityEngine;
using UnityEngine.Audio;

namespace GD.Audio
{
    /// <summary>
    /// Lowers an exposed mixer volume when a cue that ducks the music is requested, then restores it.
    /// </summary>
    [AddComponentMenu("GD/Audio/Music Ducker")]
    public class MusicDucker : MonoBehaviour
    {
        [SerializeField, Tooltip("Channel to watch for cues that duck the music. Use the same asset as the AudioManager")]
        private AudioRequestEventChannel requestChannel;

        [SerializeField, Tooltip("The mixer that holds the exposed music volume parameter")]
        private AudioMixer mixer;

        [SerializeField, Tooltip("Exposed mixer parameter for the music group's volume")]
        private string volumeParameter = "MusicVolume";

        [Header("Levels (dB)")]
        [SerializeField, Tooltip("Music volume in dB when nothing is ducking it. 0 leaves the group at its mixer level")]
        private float normalVolume = 0f;

        [SerializeField, Tooltip("Music volume in dB while ducked. About -10 dB sounds half as loud")]
        private float duckedVolume = -12f;

        [Header("Timing")]
        [SerializeField, Tooltip("How quickly the music drops")]
        private TweenProfile attackProfile;

        [SerializeField, Tooltip("How slowly the music returns")]
        private TweenProfile releaseProfile;

        private Tween duckTween;

        private void OnEnable()
        {
            requestChannel.Raised += HandleRequest;
        }

        private void OnDisable()
        {
            requestChannel.Raised -= HandleRequest;
        }

        private void HandleRequest(AudioRequest request)
        {
            if (request.Cue.DucksMusic)
                Duck(request.Cue.DuckHoldSeconds);
        }

        private void Duck(float holdSeconds)
        {
            // A second duck replaces the first, starting from the current level.
            if (duckTween != null && duckTween.IsActive())
                duckTween.Kill();

            duckTween = DOTween.Sequence()
                .Append(attackProfile.Apply(mixer.DOSetFloat(volumeParameter, duckedVolume, attackProfile.Duration)))
                .AppendInterval(holdSeconds)
                .Append(releaseProfile.Apply(mixer.DOSetFloat(volumeParameter, normalVolume, releaseProfile.Duration)))
                .SetLink(gameObject);
        }
    }
}
