using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// A string shared between systems through an asset, for example the current objective text.
    /// </summary>
    [CreateAssetMenu(fileName = "StringVariable", menuName = "GD/Variables/String", order = 4)]
    public class StringVariable : ScriptableVariable<string>
    {
        /// <summary>True when the current value is null or empty.</summary>
        public bool IsNullOrEmpty => string.IsNullOrEmpty(Value);

        /// <summary>Appends <paramref name="text"/> to the current value.</summary>
        public void Append(string text)
        {
            Value += text;
        }

        /// <summary>Appends the current value of another variable.</summary>
        public void Append(StringVariable other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            Append(other.Value);
        }
    }
}
