using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// An int shared between systems through an asset, for example score or lives.
    /// </summary>
    [CreateAssetMenu(fileName = "IntVariable", menuName = "GD/Variables/Int", order = 2)]
    public class IntVariable : ScriptableVariable<int>
    {
        /// <summary>Adds <paramref name="amount"/> to the current value. Use a negative amount to subtract.</summary>
        public void Add(int amount)
        {
            Value += amount;
        }

        /// <summary>Adds the current value of another variable.</summary>
        public void Add(IntVariable other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            Add(other.Value);
        }
    }
}
