using System;
using System.Collections.Generic;
using GD.Pool;
using UnityEngine;

namespace GD.Audio
{
    /// <summary>
    /// Plays requested cues on pooled AudioSources, with one pool per mixer group, and returns
    /// each source when it finishes.
    /// </summary>
    /// <remarks>
    /// Each group has its own source prefab. The prefab's Output sets the mixer group, and its other
    /// settings set 2D or 3D, rolloff and priority, so everything about how a group sounds is in one
    /// place. Cues only name a <see cref="MixerGroupType"/>.
    /// </remarks>
    [AddComponentMenu("GD/Audio/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        #region Types

        /// <summary>One row of the group table, as set in the Inspector.</summary>
        [Serializable]
        private class GroupMapping
        {
            [Tooltip("The group that cues name")]
            public MixerGroupType group;

            [Tooltip("Prefab with an AudioSource, with Play On Awake unticked. Its Output is this group's mixer group")]
            public AudioSource sourcePrefab;

            [Min(1), Tooltip("Sources created in advance for this group")]
            public int initialSources = 4;
        }

        /// <summary>A playing source, and the pool it must go back to.</summary>
        private readonly struct ActiveSource
        {
            public ActiveSource(AudioSource source, ObjectPool<AudioSource> pool)
            {
                Source = source;
                Pool = pool;
            }

            public AudioSource Source { get; }

            public ObjectPool<AudioSource> Pool { get; }
        }

        #endregion Types

        #region Fields

        [SerializeField, Tooltip("Channel that gameplay raises requests on. Every AudioCuePlayer must use the same asset")]
        private AudioRequestEventChannel requestChannel;

        [SerializeField, Tooltip("One row per group. A cue whose group has no row plays through the Master row")]
        private List<GroupMapping> groups = new();

        [SerializeField, Min(1), Tooltip("Requests beyond this many sounds at once, across all groups, are dropped")]
        private int maxVoices = 16;

        // Unity cannot serialize a dictionary, so the Inspector list is turned into one in Awake.
        private readonly Dictionary<MixerGroupType, ObjectPool<AudioSource>> poolsByGroup = new();
        private readonly List<ActiveSource> activeSources = new();
        private readonly HashSet<MixerGroupType> reportedMissing = new();

        #endregion Fields

        #region Unity Messages

        private void Awake()
        {
            foreach (GroupMapping mapping in groups)
            {
                if (mapping.sourcePrefab == null)
                {
                    Debug.LogWarning($"{name}: the {mapping.group} row has no source prefab, so it is ignored.", this);
                    continue;
                }

                if (poolsByGroup.ContainsKey(mapping.group))
                {
                    Debug.LogWarning($"{name}: {mapping.group} has more than one row, so only the first is used.", this);
                    continue;
                }

                // With no Output, sources play straight to the listener and no fader affects them.
                if (mapping.sourcePrefab.outputAudioMixerGroup == null)
                    Debug.LogWarning($"{name}: {mapping.sourcePrefab.name} has no Output, so {mapping.group} sounds bypass the mixer.", this);

                // A child per group keeps the hierarchy readable during Play.
                Transform parent = new GameObject(mapping.group.ToString()).transform;
                parent.SetParent(transform, false);

                poolsByGroup.Add(mapping.group, new ObjectPool<AudioSource>(mapping.sourcePrefab, mapping.initialSources, parent));
            }

            if (requestChannel == null || poolsByGroup.Count == 0)
            {
                Debug.LogError($"{name}: AudioManager needs a request channel and at least one group row.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (requestChannel != null)
                requestChannel.Raised += HandleRequest;
        }

        private void OnDisable()
        {
            if (requestChannel != null)
                requestChannel.Raised -= HandleRequest;
        }

        private void Update()
        {
            // Backwards, because removing an item moves the ones after it.
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                ActiveSource active = activeSources[i];

                if (active.Source.isPlaying)
                    continue;

                active.Pool.ReturnToPool(active.Source);
                activeSources.RemoveAt(i);
            }
        }

        #endregion Unity Messages

        #region Core Methods

        private void HandleRequest(AudioRequest request)
        {
            if (request.Cue == null || activeSources.Count >= maxVoices)
                return;

            if (!TryGetPool(request.Cue.Group, out ObjectPool<AudioSource> pool))
                return;

            AudioSource source = pool.Get();

            if (!request.Cue.TryApplyTo(source))
            {
                pool.ReturnToPool(source);
                return;
            }

            source.transform.position = request.Position;
            source.Play();
            activeSources.Add(new ActiveSource(source, pool));
        }

        private bool TryGetPool(MixerGroupType group, out ObjectPool<AudioSource> pool)
        {
            if (poolsByGroup.TryGetValue(group, out pool))
                return true;

            // Report each missing group once, not on every request.
            if (reportedMissing.Add(group))
                Debug.LogWarning($"{name}: no row for {group}, so its cues play through Master, or not at all if Master has no row.", this);

            return poolsByGroup.TryGetValue(MixerGroupType.Master, out pool);
        }

        #endregion Core Methods
    }
}
