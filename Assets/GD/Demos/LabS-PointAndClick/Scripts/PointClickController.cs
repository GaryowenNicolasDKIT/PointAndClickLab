using UnityEngine;
using DG.Tweening; //For smooth animations (specifically turning)
using GD;
using GD.Types;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

//Turns a click into an order for the player: walk to the selected object then use it
//OR walk to the clicked floor. Sits beside a SelectionManager and its ray provider

[RequireComponent(typeof(SelectionManager))]
public class PointClickController : MonoBehaviour
{
    #region Fields

    [SerializeField, Tooltip("A button action, for example UI/Click")]
    private InputActionReference clickAction;

    [SerializeField] private PointClickMover player;

    [SerializeField, Tooltip("Layers a click can walk to, usually only floor")]
    private LayerMask walkableLayers;

    [SerializeField, Min(1f)] private float maxClickDistance = 100f;

    [SerializeField, Tooltip("How the player turns to face an object. Leave empty to turn at once")]
    private TweenProfile turnProfile;

    [SerializeField, Tooltip("Raised with each point the player walks to. Leave empty if not needed")]
    private Vector3EventChannel walkTargetChosen;



    private SelectionManager selectionManager;
    private IRayProvider rayProvider;
    private Tween turning;

    #endregion Fields

    #region Unity Messages

    private void Awake()
    {
        //Cache the selectionManager and rayProvider components
        selectionManager = GetComponent<SelectionManager>();
        rayProvider = GetComponent<IRayProvider>();

        //If any vital dependencies are missing
        if (clickAction == null || player == null || rayProvider == null)
        {
            Debug.LogError($"{name}: PointClickController needs a click action, a player and an IRayProvider.", this);
            enabled = false; //Disables the script entirely
        }
    }

    private void OnEnable()
    {
        //The UI input module shares this action, so this component enables it but never disables it
        if (clickAction != null) 
            clickAction.action.Enable(); //Enables the click input action
    }

    private void Update()
    {
        if (!clickAction.action.WasPressedThisFrame()) //If the click input was not triggered this frame
            return; //Exit

        //A click on a button must not also move the player
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) //Checks pointer not over UI element (Seen through Event System)
            return;

        //Any new order cancels a turn in progress, and the use that would follow it.
        if (turning != null && turning.IsActive())
            turning.Kill(); //STOPS any active rotation tween

        Transform selection = selectionManager.CurrentSelection; //Retrieves the currently selected target objcet

        //Checks if the sleection exists and has the Interactable component
        if (selection != null && selection.TryGetComponent(out IInteractable interactable))
            WalkToAndUse(selection, interactable); //If so, calls walk to and use
        else
            WalkToFloor(); //If no, calls walk to 
    }

    #endregion Unity Messages

    #region Core Methods

    private void WalkToFloor() //Handling moving to empty floor locations
    {
        if (!rayProvider.TryCreateRay(out Ray ray)) //Tries to generate a screen ray
            return; //Exits if this fails

        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, walkableLayers)) //Casts a ray against walkable layers
            return; //Exits if ray doesnt hit a walkable surface

        //Commands the player to move to the hit point and checks if the walk target channel exists
        if (player.TryMoveTo(hit.point) && walkTargetChosen != null)
            walkTargetChosen.Raise(hit.point); //Broadcasts the chosen destination coordinate to the Vector3 channel
    }

    private void WalkToAndUse(Transform target, IInteractable interactable)
    {
        //Determines the standing position using an interaction spot if possible, otherwise defaulting to the target's position
        Vector3 standPosition = target.TryGetComponent(out InteractionSpot spot)
            ? spot.StandPosition : 
            target.position;

        //Callback only runs if player arrives, so another click before therefore cancels the use
        //This commands the player to move to the stand position of the object with a callback to face and use the object upon arrival
        if (player.TryMoveTo(standPosition, () => FaceThenUse(target, interactable)) && walkTargetChosen != null)
            walkTargetChosen.Raise(standPosition); //Broadcasts the chosen standing coordinate
    }

    //Likely not needed for our game
    private void FaceThenUse(Transform target, IInteractable interactable)
    {
        //The object may have been destroyed during the walk
        if (target == null)
            return;

        //Calculates the directional vector from the player to the target
        Vector3 toTarget = target.position - player.transform.position; 
        toTarget.y = 0f; //Flattens the vector to the horizontal plane (disregards Vertical)

        //Checks if the player is already facing the target (or if no turn profile is defined)
        if(toTarget.sqrMagnitude < 0.0001f || turnProfile == null)
        {
            //Checks if there is a valid definition to look at
            if (toTarget.sqrMagnitude >= 0.0001f) 

                //Instantly snaps the player's rotation towards the target
                player.transform.rotation = Quaternion.LookRotation(toTarget); 

            //Triggers the interaction method on the target
            interactable.Interact(player.gameObject); 
            return;
        }

        //Calculates the target rotation Quaternion
        Quaternion facing = Quaternion.LookRotation(toTarget);

        //Applies smooth rotation using DOTween and caches the tween reference
        turning = turnProfile
            .Apply(player.transform.DORotateQuaternion(facing, turnProfile.Duration))
            .OnComplete(() => //Chains a callback to run when the rotation tween finishes
            {
                if (target != null) //Re-verifies the target still exists after turning
                    interactable.Interact(player.gameObject); //Triggers the interaction once rotation completes
            });
    }

    #endregion Core Methods

}
