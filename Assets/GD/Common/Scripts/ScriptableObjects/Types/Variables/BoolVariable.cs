using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// A bool shared between systems through an asset, for example whether the exit door is unlocked.
    /// </summary>
    [CreateAssetMenu(fileName = "BoolVariable", menuName = "GD/Variables/Bool", order = 1)]
    public class BoolVariable : ScriptableVariable<bool>
    {
        /// <summary>Flips the current value.</summary>
        public void Toggle()
        {
            Value = !Value;
        }
    }
}
