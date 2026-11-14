using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An event channel that sends a string with each raise, for example a line of dialogue to display.
    /// </summary>
    [CreateAssetMenu(fileName = "StringEventChannel", menuName = "GD/Events/String", order = 4)]
    public class StringEventChannel : EventChannel<string>
    {
    }
}
