using UnityEngine;
using GD.Types;
using Unity.AI.Navigation;
using System.Collections;

[RequireComponent(typeof(NavMeshSurface))] //Automatically adds the required dependencies
public class NavMeshRebuilder : MonoBehaviour
{
    [SerializeField, Tooltip("Raise this after changing the level")]
    private VoidEventChannel levelChanged;

    [SerializeField, Tooltip("Raised when the NavMesh matches the level")]
    private VoidEventChannel navMeshUpdated;

    private NavMeshSurface surface;
    private Coroutine updating;
    private bool updateAgain;

    private void Awake()
    {
        surface = GetComponent<NavMeshSurface>(); //Gets the initial NavMeshSurface and stores it in the rebuilder
    }

    private void OnEnable()
    {
        levelChanged.Raised += HandleLevelChanged;
    }

    private void OnDisable()
    {
        levelChanged.Raised -= HandleLevelChanged;
    }

    private void Start()
    {
        //Since UpdateNavMesh needs initial data, we build once first
        surface.BuildNavMesh();
        navMeshUpdated.Raise();
    }

    private void HandleLevelChanged() //When this method is called 
    {
        if(updating != null) //if Updating isnt currently happening (null IEnumerator)
        {
            updateAgain = true; //Make updateAgain true
            return;
        }

        updating = StartCoroutine(UpdateUntilCurrent()); //Start the Coroutine. Returns an IEnumerator
    }

    private IEnumerator UpdateUntilCurrent()
    {

        do //If the system has been told to update again
        {
            updateAgain = false; //Disable the need to update again (as currently updating)
            yield return surface.UpdateNavMesh(surface.navMeshData); //Rebuilds the NavMesh with the new data
        }
        while (updateAgain); 

        updating = null; //Once done, clears the data of the Coroutine (Served its purpose)
        navMeshUpdated.Raise(); //Raises to the Event Channel that the navMesh has been updated. Will appear in log as we ticked it.
    }
}
