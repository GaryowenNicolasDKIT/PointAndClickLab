using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// A float shared between systems through an asset, for example player health or a countdown.
    /// </summary>
    [CreateAssetMenu(fileName = "FloatVariable", menuName = "GD/Variables/Float", order = 3)]
    public class FloatVariable : ScriptableVariable<float>
    {
        /// <summary>Adds <paramref name="amount"/> to the current value. Use a negative amount to subtract.</summary>
        public void Add(float amount)
        {
            Value += amount;
        }

        /// <summary>Adds the current value of another variable.</summary>
        public void Add(FloatVariable other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            Add(other.Value);
        }
    }
}
