using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


//<summary>
//Reports whether a complete route exists between two points on the NavMesh
//</summary>
public class RouteCheck : MonoBehaviour
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform goal;

    [SerializeField, Min(0.1f), Tooltip("How far a point may be from the NavMesh, in metres")]
    private float sampleDistance = 2f;

    private NavMeshPath path;

    private void Awake()
    {
        path = new NavMeshPath();
    }

    public bool HasCompletedRoute() //Returns if there is a valid, unbroken path between the Start and the Goal
    {
        //Finds the nearest valid point on the NavMesh to the start position (Within the allowed Sample Distance)
        if (!NavMesh.SamplePosition(start.position, out NavMeshHit startHit, sampleDistance, NavMesh.AllAreas)) 
            return false;
        //Same as above but for the goal
        if (!NavMesh.SamplePosition(goal.position, out NavMeshHit goalHit, sampleDistance, NavMesh.AllAreas))
            return false;
        //If the Start or Goal position are off the NavMesh, the method returns false

        //Calculates a path from the (snapped) start point to the (snapped) end point across the NavMesh area.
        //It stores the result in the path object, returning "True" if the caluculation was successfully attempted
        return NavMesh.CalculatePath(startHit.position, goalHit.position, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete; //Combines the calculation check with a check on the path's status, ensuring full completion
    }
}
