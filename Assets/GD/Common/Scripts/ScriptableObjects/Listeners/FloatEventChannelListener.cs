using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Runs responses set in the Inspector when a <see cref="FloatEventChannel"/> is raised,
    /// passing the raised float to each response, for example to set a health bar fill amount.
    /// </summary>
    [AddComponentMenu("GD/Events/Float Event Channel Listener")]
    public class FloatEventChannelListener : EventChannelListener<FloatEventChannel, float>
    {
    }
}
