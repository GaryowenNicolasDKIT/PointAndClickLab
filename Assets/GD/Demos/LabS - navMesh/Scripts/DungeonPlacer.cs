using System.Collections.Generic;
using UnityEngine;
using GD;
using GD.Types;
using UnityEngine.InputSystem;


//Places pieces on a grid. A piece that would seal the route is removed again.
public class DungeonPlacer : MonoBehaviour
{
    [SerializeField] private InputAction placeAction = new("Place", binding: "<Mouse>/leftButton");
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private RouteCheck routeCheck;

    [SerializeField, Tooltip("Layers a piece can be placed on")]
    private LayerMask buildLayers;

    [SerializeField, Min(0.5f), Tooltip("Size of one grid cell, in metres")]
    private float cellSize = 4f;

    [Header("Channels")] //Broadcasts event whenever...
    [SerializeField] private VoidEventChannel levelChanged; //The level's layout changes
    [SerializeField] private VoidEventChannel navMeshUpdated; //When the baked navMesh finishes updating. Also listens to other calls on this channel.
    [SerializeField] private VoidEventChannel placementRefused; //Piece rejected due to blocking route

    private readonly Dictionary<Vector2Int, GameObject> pieces = new(); //Tracks all placed pieces on the grid using grid coords (Vector2Int) as keys.

    private IRayProvider rayProvider; //Interface reference used to generate screen-to-world rays.
    private GameObject pendingPiece; //Temp holder of the reference to most recently placed piece while awaiting validation
    private Vector2Int pendingCell; //Stores grid location of currently held piece

    private void Awake()
    {
        rayProvider = GetComponent<IRayProvider>(); //Caches the IRayProvider component attached to the GameObject
    }

    private void OnEnable()
    {
        navMeshUpdated.Raised += HandleNavMeshUpdated; //Subscribes the HandleNavMeshUpdated Method to the navMeshUpdated event channel
        placeAction.performed += HandlePlacePressed; //Subscribes the HandlePlacePressed Method to the input action trigger
        placeAction.Enable(); //Enables the input action
    }

    private void OnDisable() //When object is disabled or Destroyed
    {
        navMeshUpdated.Raised -= HandleNavMeshUpdated; //Unsubscribes from event channel to prevent memory leaks
        placeAction.performed -= HandlePlacePressed; //Unsubscribes from the Input Action
        placeAction.Disable(); //Disables input action
    }

    //Wire to UI Buttons, to choose what the next click places
    public void SelectPiece(GameObject prefab)
    {
        piecePrefab = prefab;
    }

    private void HandlePlacePressed(InputAction.CallbackContext context)
    {
        //Only allows one piece at a time as each single piece must pass the route check

        //Ensures there is no piece currently awaiting validation and that a valid ray can be cast from the mouse position
        if (pendingPiece != null || !rayProvider.TryCreateRay(out Ray ray)) 
            return;

        //Casts a ray up to 200 units
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, buildLayers))
            return; //Exits if the ray does not hit anything buildable

        //Converts the ray's hit point into discrete grid cell coordinates based on cell size
        Vector2Int cell = new(Mathf.FloorToInt(hit.point.x / cellSize), Mathf.FloorToInt(hit.point.z / cellSize));

        //Checks if there is already a piece at this specific coordinate
        if (pieces.ContainsKey(cell))
            return; //Aborts if the cell is already full (Could put message here for "Tile Already Full" or something)

        //Calculates the exact center world-position for the piece within that grid cell.
        Vector3 position = new((cell.x + 0.5f) * cellSize, hit.point.y, (cell.y + 0.5f) * cellSize);

        //Spawns an instance of the piece prefab into the world
        pendingPiece = Instantiate(piecePrefab,position, Quaternion.identity);
        pendingCell = cell; //Stores the cell coordinates as pending validation
        pieces[cell] = pendingPiece; //Registers the piece in the dictionary tracker

        levelChanged.Raise(); //Triggers level changed event (Rebakes the NavMesh)
    }

    private void HandleNavMeshUpdated() //Listens for when navigation mesh finishes baking/updating after change
    {
        if (pendingPiece == null) //Checks if there is a piece currently awaiting verification
            return; //Exits if not
        //This code is needed as there can be an instance where the navMeshUpdated Channel gets flagged for non-Placing reasons

        GameObject piece = pendingPiece; //Temp store of the pending piece
        pendingPiece = null; //Clears pending reference

        if (routeCheck.HasCompletedRoute()) //Asks the RouteCheck component if a valid, unbroken path still exists from the start to the goal
            return; //If Valid, stops here

        pieces.Remove(pendingCell); //If Invalid, removes the cell from the dictionary (clearing the space again)

        //Deactivate now because destroy waits until the end of the grame and the next update must NOT collect this piece
        piece.SetActive(false); //Deactivates game object to prevent any further interaction
        Destroy(piece); //Destroys instantiated piece object to fully roll back its placement

        levelChanged.Raise(); //As this rechanges the level to what it was before, the processes need to be called again (So channel is flagged)
        placementRefused.Raise(); //Broadcast an even signaling placement was rejected (useful for error sounds or UI warnings)
    }
}
