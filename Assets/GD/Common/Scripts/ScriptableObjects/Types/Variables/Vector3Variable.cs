using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// A Vector3 shared between systems through an asset, for example the last checkpoint position.
    /// </summary>
    [CreateAssetMenu(fileName = "Vector3Variable", menuName = "GD/Variables/Vector3", order = 5)]
    public class Vector3Variable : ScriptableVariable<Vector3>
    {
        /// <summary>Adds <paramref name="offset"/> to the current value.</summary>
        public void Add(Vector3 offset)
        {
            Value += offset;
        }

        /// <summary>Adds the current value of another variable.</summary>
        public void Add(Vector3Variable other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            Add(other.Value);
        }
    }
}
