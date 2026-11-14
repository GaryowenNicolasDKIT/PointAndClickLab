using UnityEngine;
using GD.Types;
using UnityEngine.AI;

//Walks to a goal across a level that can change at any time

[RequireComponent(typeof(NavMeshAgent))]
public class DungeonWalker : MonoBehaviour, IExplosionTarget
{
    [SerializeField] private NavigationProfile profile;
    [SerializeField] private VoidEventChannel navMeshUpdated;

    [SerializeField, Min(0.1f), Tooltip("Seconds between path updates when nothing else requests one")]
    private float repathSeconds = 1f;

    [SerializeField, Min(0.1f), Tooltip("How far the agent may be from the NavMesh and still return to it")]
    private float recoverDistance = 2f;

    private NavMeshAgent agent;
    private Transform goal;
    private float repathTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        profile.ApplyTo(agent);
    }

    private void OnEnable()
    {
        navMeshUpdated.Raised += Repath;
    }
    private void OnDisable()
    {
        navMeshUpdated.Raised -= Repath;
    }

    public void SetGoal(Transform newGoal)
    {
        goal = newGoal;
        Repath();
    }

    public void HandleExplosion()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
            Repath();
    }

    private void Repath()
    {
        repathTimer = repathSeconds;

        if (goal == null)
            return;

        //The ground under this enemy may have been removed.
        if (!agent.isOnNavMesh && !TryReturnToNavMesh())
            return;

        agent.SetDestination(goal.position);
    }

    private bool TryReturnToNavMesh()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, recoverDistance, NavMesh.AllAreas))
            return false;

        return agent.Warp(hit.position);
    }
}
