using GD.Types;
using UnityEngine;

namespace GD.Inventory
{
    [CreateAssetMenu(fileName = "ItemRequestEventChannel", menuName = "GD/Inventory/Events/Item Request")]
    public class ItemRequestEventChannel : EventChannel<ItemRequest>
    {
    }
}
