using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="StringEventChannel"/> is raised,
    /// passing the raised string to each response, for example to show a line of dialogue.
    /// </summary>
    [AddComponentMenu("GD/Events/String Event Channel Listener")]
    public class StringEventChannelListener : EventChannelListener<StringEventChannel, string>
    {
    }
}
