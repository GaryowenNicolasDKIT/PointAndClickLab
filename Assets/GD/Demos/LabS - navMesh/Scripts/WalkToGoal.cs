using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))] //Can only go above Class Definitions. Its like the using <T> thing from C++
public class WalkToGoal : MonoBehaviour
{
    [SerializeField] private Transform goal; //Where the thing draws the NavPath to...

    private void Start()
    {
        GetComponent<NavMeshAgent>().SetDestination(goal.position);
    }
}
