using GD.Types;
using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// Met when a shared float compares as stated, for example "health at or below 0".
    /// Avoid Equal and NotEqual, because floats are rarely exact.
    /// </summary>
    [CreateAssetMenu(fileName = "FloatVariableCondition", menuName = "GD/Conditions/Float Variable")]
    public class FloatVariableCondition : Condition
    {
        [SerializeField] private FloatVariable variable;
        [SerializeField] private CompareOperator compare = CompareOperator.LessOrEqual;
        [SerializeField] private float target;

        public override bool IsMet()
        {
            return variable != null && compare.Evaluate(variable.Value, target);
        }
    }
}
