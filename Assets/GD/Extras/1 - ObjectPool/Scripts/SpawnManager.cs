using System.Collections;
using UnityEngine;

namespace GD.Extras
{
    /// <summary>
    /// Spawns one pool type at one of several spawn points, at times chosen by a
    /// <see cref="SpawnTiming"/> strategy. Add one manager per pickup type.
    /// </summary>
    /// <remarks>
    /// The manager decides when and where; the <see cref="ObjectPoolProvider"/> decides whether a
    /// copy is available. A spawn is skipped when the cap is reached or every point is occupied.
    /// </remarks>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private ObjectPoolProvider provider;
        [SerializeField] private PoolType type;

        [SerializeField, Tooltip("When to spawn. Swap the asset to change the rule")]
        private SpawnTiming timing;

        [SerializeField, Tooltip("Where copies may appear. One copy per point at a time")]
        private Transform[] spawnPoints;

        // The copy last spawned at each point, so an occupied point is not used again.
        private Transform[] occupants;

        private void OnEnable()
        {
            if (provider == null || timing == null || spawnPoints.Length == 0)
            {
                Debug.LogError($"{name}: assign the provider, the timing and at least one spawn point.", this);
                enabled = false;
                return;
            }

            occupants = new Transform[spawnPoints.Length];
            StartCoroutine(SpawnLoop());
        }

        private void OnDisable()
        {
            // Disabling a component does not stop its coroutines, so stop them here.
            StopAllCoroutines();
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(timing.GetNextDelay());
                TrySpawn();
            }
        }

        private void TrySpawn()
        {
            int index = FindFreePoint();
            if (index < 0)
                return;

            Transform point = spawnPoints[index];
            if (!provider.TrySpawn(type, point.position, point.rotation, out Transform instance))
                return;

            // The pool reuses copies, so this copy may still be recorded at its old point.
            for (int i = 0; i < occupants.Length; i++)
            {
                if (occupants[i] == instance)
                    occupants[i] = null;
            }

            occupants[index] = instance;
        }

        // A random free point, or -1 if all are occupied. Starts at a random index and
        // wraps round, so every free point has a chance without building a list.
        private int FindFreePoint()
        {
            int start = Random.Range(0, spawnPoints.Length);

            for (int step = 0; step < spawnPoints.Length; step++)
            {
                int index = (start + step) % spawnPoints.Length;
                Transform occupant = occupants[index];

                // Free if nothing was spawned here, or that copy was collected (deactivated).
                if (occupant == null || !occupant.gameObject.activeSelf)
                    return index;
            }

            return -1;
        }
    }
}
