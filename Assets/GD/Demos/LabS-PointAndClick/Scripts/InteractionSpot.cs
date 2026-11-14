using UnityEngine;

//Marks where the player stands to use this object. The player faces the object itself. Not in our actual game just in this.

public class InteractionSpot : MonoBehaviour
{
    [SerializeField, Tooltip("A Child placed on the floor in front of the object")]
    private Transform standPoint;

    //A public expression-bodied read-only property
    //If a standPoint has been assigned, it returns that point's world position
    //Otherwise it safely falls back on the object's own position
    public Vector3 StandPosition => standPoint != null ? standPoint.position : transform.position;

    private void OnDrawGizmosSelected() //Rendering for Debugging purposes. Scene View only.
    {
        if(standPoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(standPoint.position, 0.25f);
        Gizmos.DrawLine(standPoint.position, transform.position);
    }
}
