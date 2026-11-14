using UnityEngine;
using System;
using UnityEngine.AI;


//Walks a NavMeshAgent to a point then calls back when it gets there
//This script knows nothing about clicks, cameras or the objects in the room

[RequireComponent(typeof(NavMeshAgent))]
public class PointClickMover : MonoBehaviour
{
    [SerializeField, Min(0.1f), Tooltip("How far a requested point may be from the NavMesh, in metres")]
    private float sampleDistance = 1f;

    private NavMeshAgent agent;
    private Action onArrived;
    private bool isMoving;

    public bool IsMoving => isMoving;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    //Starts walking to the point on the NavMesh nearest to <paremref name = "point"/>
    //A new order replaces the current one, the old callback is dropped
    //Returns false if there is no NavMesh near the point

    public bool TryMoveTo(Vector3 point, Action arrived = null)
    {
        //Drop the old move order first, thus a refused order can also cancel it
        onArrived = null;
        isMoving = false;

        if (!agent.isOnNavMesh) //Ensures agent is currently standing on valid NavMesh terrain
            return false; //If not, aborts

        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, sampleDistance, agent.areaMask)) //Snaps the area clicked onto the nearest valid NavMesh area
            return false; //Aborts if there is none within the allowed sampleDistance

        if (!agent.SetDestination(hit.position)) //Tells the NavMeshAgent to pathfind toward the snapped position
            return false; //Returns false if setting the destination fails

        onArrived = arrived; //Assigns the new arrival callback
        isMoving = true; //Sets movement status to active (Useful for animations)
        return true; //Confirms that the movement order was successfully accepted
    }

    public void Stop() //Halts the agent immediately
    {
        onArrived = null; //Clears pending arrival callback so that it wont fire
        isMoving = false; //Sets movement status to false

        if (agent.isOnNavMesh) //Checks if the agent is validly on the NavMesh
            agent.ResetPath(); //Clears current path calculation, stopping the agent
    }

    private void Update()
    {
        //remainingDistance is only meaningful once the path has been calculated
        //Checks agent isnt moving, still calculating path data or hasnt reached its stopping threshold yet
        if (!isMoving || agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            return; //Skips the rest of the frame if the agent has not arrived yet

        isMoving = false; //Marks the agent as having finished its movement (bowel hehe)

        //A partial path ends as close as the agent can get, which is not the place it was sent.

        bool reached = agent.pathStatus == NavMeshPathStatus.PathComplete; //Confirms path is fully complete and unobstructed

        //Clear the field before the call, because the callback may give a new order
        Action arrived = onArrived; //Temporarily stores the callback reference
        onArrived = null; //resets the field

        if (reached) //Checks if destination was successfully reached with a complete path
            arrived?.Invoke(); //Safely triggers the callback function if it isnt null
    }

}
