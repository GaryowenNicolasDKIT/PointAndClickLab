using System;
using System.Collections.Generic;
using GD.Pool;
using UnityEngine;

namespace GD.Extras
{
    /// <summary>
    /// Holds one pool per <see cref="PoolType"/>, each with a cap on how many copies may be
    /// active at once (for example 5 ammo boxes, 2 health packs).
    /// </summary>
    /// <remarks>
    /// To return a copy, deactivate it, for example with ContactTrigger's On Enter calling
    /// GameObject.SetActive(false) on a pickup. The provider takes inactive copies back into
    /// their pool before each spawn, so pooled objects need no code of their own.
    /// </remarks>
    public class ObjectPoolProvider : MonoBehaviour
    {
        /// <summary>One Inspector row, plus the pool built from it at runtime.</summary>
        [Serializable]
        public class PoolEntry
        {
            public PoolType type;
            public Transform prefab;
            [Min(0)] public int initialSize = 2;
            [Min(1)] public int maxActive = 5;

            [NonSerialized] public ObjectPool<Transform> pool;
            [NonSerialized] public readonly List<Transform> active = new();
        }

        [SerializeField, Tooltip("One row per pool type. Each type may appear once")]
        private List<PoolEntry> pools = new();

        private readonly Dictionary<PoolType, PoolEntry> poolMap = new();

        private void Awake()
        {
            foreach (PoolEntry entry in pools)
            {
                if (entry.prefab == null || poolMap.ContainsKey(entry.type))
                {
                    Debug.LogError($"{name}: the {entry.type} row has no prefab or is a duplicate, so it is skipped.", this);
                    continue;
                }

                int initialSize = Mathf.Min(entry.initialSize, entry.maxActive);
                entry.pool = new ObjectPool<Transform>(entry.prefab, initialSize, transform);
                poolMap.Add(entry.type, entry);
            }
        }

        /// <summary>
        /// Spawns a copy of <paramref name="type"/> at a position and rotation. Returns false if
        /// there is no pool for the type, or its cap has been reached.
        /// </summary>
        public bool TrySpawn(PoolType type, Vector3 position, Quaternion rotation, out Transform instance)
        {
            instance = null;

            if (!poolMap.TryGetValue(type, out PoolEntry entry))
            {
                Debug.LogWarning($"{name}: there is no pool for {type}.", this);
                return false;
            }

            ReclaimInactive(entry);

            if (entry.active.Count >= entry.maxActive)
                return false;

            instance = entry.pool.Get();
            instance.SetPositionAndRotation(position, rotation);
            entry.active.Add(instance);
            return true;
        }

        /// <summary>Copies of <paramref name="type"/> currently active.</summary>
        public int GetActiveCount(PoolType type)
        {
            if (!poolMap.TryGetValue(type, out PoolEntry entry))
                return 0;

            ReclaimInactive(entry);
            return entry.active.Count;
        }

        // Returns copies that have been deactivated since the last check to their pool.
        private static void ReclaimInactive(PoolEntry entry)
        {
            // Backwards, so removing an item does not skip the next one.
            for (int i = entry.active.Count - 1; i >= 0; i--)
            {
                Transform copy = entry.active[i];

                // Destroyed by other code: it cannot go back to the pool.
                if (copy == null)
                    entry.active.RemoveAt(i);
                else if (!copy.gameObject.activeSelf)
                {
                    entry.pool.ReturnToPool(copy);
                    entry.active.RemoveAt(i);
                }
            }
        }
    }
}
