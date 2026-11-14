using UnityEngine;

namespace GD.Inventory
{
    /// <summary>
    /// An item collected when the player walks into it. Needs a trigger collider on the same GameObject.
    /// </summary>
    [AddComponentMenu("GD/Inventory/Items/Collectible Item")]
    public class CollectibleItem : WorldItem
    {
        [SerializeField, Tooltip("Only objects with this tag collect the item")]
        private string collectorTag = "Player";

        private void Reset()
        {
            // Editor only: runs when the component is added, so the collider starts as a trigger.
            if (TryGetComponent(out Collider itemCollider))
                itemCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // The Starter Assets player is a CharacterController, which enters triggers (Note 08).
            if (other.CompareTag(collectorTag))
                RequestCollect();
        }
    }
}
