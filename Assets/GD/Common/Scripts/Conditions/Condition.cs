using System.Collections.Generic;
using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// One question about the current state of the game. Reading it must change nothing.
    /// </summary>
    public abstract class Condition : ScriptableObject
    {
        /// <summary>True if this condition is met right now.</summary>
        public abstract bool IsMet();

        /// <summary>The conditions directly inside this one. Leaves have none.</summary>
        protected virtual IEnumerable<Condition> Children => System.Array.Empty<Condition>();

        /// <summary>Describes this condition and any children, for debugging.</summary>
        public virtual string Describe(string indent = "")
        {
            string text = $"{indent}{name}: {(IsMet() ? "met" : "not met")}";

            foreach (Condition child in Children)
            {
                if (child != null)
                    text += "\n" + child.Describe(indent + "    ");
            }

            return text;
        }

        [ContextMenu("Log State")]
        private void LogState()
        {
            Debug.Log(Describe(), this);
        }

        private void OnValidate()
        {
            if (Reaches(this, new HashSet<Condition>()))
                Debug.LogError($"{name} contains itself, which would overflow the stack.", this);
        }

        private bool Reaches(Condition target, HashSet<Condition> visited)
        {
            foreach (Condition child in Children)
            {
                if (child == null)
                    continue;

                if (child == target)
                    return true;

                if (visited.Add(child) && child.Reaches(target, visited))
                    return true;
            }

            return false;
        }
    }
}
