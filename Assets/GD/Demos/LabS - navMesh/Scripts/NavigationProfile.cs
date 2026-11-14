using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NavigationProfile", menuName = "GD/Enemies/Navigation Profile")]
public class NavigationProfile : ScriptableObject
{
    [SerializeField, Tooltip("Must Match a name on the Areas tab")] //Areas tab part of the navMesh features
    private string trapAreaName = "Trap";

    [SerializeField, Min(1f), Tooltip("How many metres of normal floor one metre of trap is worth")]
    private float trapCost = 20f;

    public void ApplyTo(NavMeshAgent agent)
    {
        int area = NavMesh.GetAreaFromName(trapAreaName); //Retrieves the associated area value for the name given 

        if (area < 0) //If the area is less than zero, its because GetAreaFromName returned -1 (Failed to find)
        {
            Debug.LogError($"{name}: there is no NavMesh area named {trapAreaName}.", this); //Error message for the stupid developer who made this error
            return;
        }

        agent.SetAreaCost(area,trapCost); //If one is found, replaces the NavMesh's stored area cost with the one put in by the user here.
    }
}
