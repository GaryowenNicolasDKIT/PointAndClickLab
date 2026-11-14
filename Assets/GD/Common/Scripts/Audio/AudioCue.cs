using UnityEngine;

namespace GD.Audio
{
    /// <summary>
    /// Describes one sound in the game: which clips, how they vary, and which mixer group it belongs to.
    /// </summary>
    /// <remarks>
    /// The cue names a group, not a mixer asset. The AudioManager maps the group to a source prefab,
    /// which sets the mixer group and the spatial settings, so changing the mixer changes no cue.
    /// </remarks>
    [CreateAssetMenu(fileName = "AudioCue", menuName = "GD/Audio/Audio Cue")]
    public class AudioCue : ScriptableObject
    {
        [SerializeField, TextArea(2, 4), Tooltip("What this cue is for and where it plays, for the team. Not shown in the game")]
        private string description;

        [SerializeField, Tooltip("One clip is chosen at random each time")]
        private AudioClip[] clips;

        [SerializeField, Tooltip("The group this sound plays through. Its source prefab sets the mixer group, 2D or 3D, rolloff and priority")]
        private MixerGroupType group = MixerGroupType.SFX;

        [SerializeField, Range(0f, 1f), Tooltip("Volume before random variation. 1 plays the clip at its recorded level")]
        private float volume = 1f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Random reduction in volume, as a fraction")]
        private float volumeVariation;

        [SerializeField, Range(0f, 0.5f), Tooltip("Random change in pitch, up or down, as a fraction")]
        private float pitchVariation = 0.05f;

        [Header("Mix")]
        [SerializeField, Tooltip("Lower the music while this sound plays")]
        private bool ducksMusic;

        [SerializeField, Min(0f), Tooltip("Seconds the music stays lowered")]
        private float duckHoldSeconds = 0.5f;

        public string Description => description;
        public MixerGroupType Group => group;
        public bool DucksMusic => ducksMusic;
        public float DuckHoldSeconds => duckHoldSeconds;

        /// <summary>
        /// Sets every value this cue controls, so a reused source keeps nothing from its last cue.
        /// Values the source prefab controls, such as spatial blend and rolloff, are left alone.
        /// Returns false if the cue has no clips.
        /// </summary>
        public bool TryApplyTo(AudioSource source)
        {
            if (clips == null || clips.Length == 0)
                return false;

            source.clip = clips[Random.Range(0, clips.Length)];
            source.volume = volume * Random.Range(1f - volumeVariation, 1f);
            source.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            source.loop = false;
            return true;
        }
    }
}
