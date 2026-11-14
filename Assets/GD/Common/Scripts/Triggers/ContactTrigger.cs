using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Fires events when a matching object touches this object and when it leaves.
/// Works with CharacterController and Rigidbody objects.
///
/// Setup:
///   - Put a Collider on this object.
///   - Tick "Is Trigger" on the Collider. This is the setting that works for BOTH
///     CharacterController and Rigidbody (see the note on OnCollisionEnter below).
///   - Choose which Layers to react to, and optionally a Tag.
///   - Hook up responses in the On Enter and On Exit events in the Inspector.
/// </summary>
[AddComponentMenu("GD/Triggers/Contact Trigger")]
public class ContactTrigger : MonoBehaviour
{
    [Header("Filter")]
    // Only objects on these layers are detected. Defaults to Everything.
    [SerializeField] private LayerMask layers = ~0;

    // Leave empty to accept any tag
    [SerializeField] private string requiredTag = "";

    [Header("Response")]
    // Choose what happens in the Inspector, e.g. play a sound,
    // open a door, or call SetValue on a SetMaterialFloat script
    [SerializeField] private UnityEvent onEnter;

    // Choose what happens when the object leaves, e.g. close a door.
    // Not called if the object is destroyed or deactivated while touching.
    [SerializeField] private UnityEvent onExit;

    // Called when something enters a collider with "Is Trigger" ticked.
    // Works for CharacterController AND Rigidbody objects.
    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other.gameObject);
    }

    // Called when something physically bumps a NON-trigger collider.
    // Only works for Rigidbody objects: a CharacterController does not
    // send collision messages, so it will never call this.
    private void OnCollisionEnter(Collision collision)
    {
        HandleContact(collision.gameObject);
    }

    // Called when something leaves a collider with "Is Trigger" ticked.
    private void OnTriggerExit(Collider other)
    {
        HandleExit(other.gameObject);
    }

    // Called when something stops touching a NON-trigger collider.
    // As with OnCollisionEnter, this only works for Rigidbody objects.
    private void OnCollisionExit(Collision collision)
    {
        HandleExit(collision.gameObject);
    }

    private void HandleContact(GameObject other)
    {
        if (!Matches(other))
            return;

        Debug.Log($"{other.name} touched {name}", this);
        onEnter.Invoke();
    }

    private void HandleExit(GameObject other)
    {
        if (!Matches(other))
            return;

        onExit.Invoke();
    }

    private bool Matches(GameObject other)
    {
        // A LayerMask stores one bit per layer. Shift 1 left by the object's
        // layer number to get its bit, then check if that bit is set in the mask.
        bool layerMatches = (layers.value & (1 << other.layer)) != 0;

        // An empty tag means "any tag is fine"
        bool tagMatches = string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);

        return layerMatches && tagMatches;
    }
}
