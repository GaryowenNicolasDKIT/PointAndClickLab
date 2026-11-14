using System;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// A field that a designer sets in the Inspector either to a constant or to a shared
    /// <see cref="ScriptableVariable{T}"/> asset.
    /// </summary>
    /// <remarks>
    /// Use it where a value is usually local but sometimes needs to be shared. For example,
    /// an enemy's speed is normally a constant, but a difficulty system might drive it from
    /// a shared FloatVariable. Declare fields with the named subclasses below
    /// (FloatReference, IntReference and so on) for a shorter type name.
    /// </remarks>
    /// <typeparam name="T">The type of value, for example float.</typeparam>
    /// <typeparam name="TVariable">The matching variable asset type, for example FloatVariable.</typeparam>
    [Serializable]
    public class VariableReference<T, TVariable> where TVariable : ScriptableVariable<T>
    {
        #region Fields

        [SerializeField]
        private bool useConstant = true;

        [SerializeField]
        private T constantValue;

        [SerializeField]
        private TVariable variable;

        #endregion Fields

        #region Properties

        /// <summary>True when the reference uses its constant value rather than an asset.</summary>
        public bool UseConstant => useConstant;

        /// <summary>
        /// The shared asset, or null when a constant is used.
        /// Subscribe to its ValueChanged event to react to changes.
        /// </summary>
        public TVariable Variable => useConstant ? null : variable;

        /// <summary>
        /// False when the reference is set to use a variable but none is assigned.
        /// Check it in Awake, as you would any other required reference.
        /// </summary>
        public bool IsValid => useConstant || variable != null;

        /// <summary>The constant value, or the variable's current runtime value.</summary>
        public T Value
        {
            get
            {
                if (useConstant)
                    return constantValue;

                if (variable == null)
                    throw new InvalidOperationException(
                        $"{GetType().Name} is set to use a variable, but no {typeof(TVariable).Name} asset is assigned.");

                return variable.Value;
            }
        }

        #endregion Properties
    }

    #region Named References

    [Serializable] public class BoolReference : VariableReference<bool, BoolVariable> { }

    [Serializable] public class IntReference : VariableReference<int, IntVariable> { }

    [Serializable] public class FloatReference : VariableReference<float, FloatVariable> { }

    [Serializable] public class StringReference : VariableReference<string, StringVariable> { }

    [Serializable] public class Vector3Reference : VariableReference<Vector3, Vector3Variable> { }

    [Serializable] public class GameObjectReference : VariableReference<GameObject, GameObjectVariable> { }

    #endregion Named References
}
