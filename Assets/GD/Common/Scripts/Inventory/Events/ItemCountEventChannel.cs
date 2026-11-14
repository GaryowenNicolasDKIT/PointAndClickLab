using GD.Types;
using UnityEngine;

namespace GD.Inventory
{
    [CreateAssetMenu(fileName = "ItemCountEventChannel", menuName = "GD/Inventory/Events/Item Count")]
    public class ItemCountEventChannel : EventChannel<ItemCount>
    {
    }
}
