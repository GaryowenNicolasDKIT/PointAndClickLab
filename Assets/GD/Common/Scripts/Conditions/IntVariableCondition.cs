using GD.Types;
using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// Met when a shared int compares as stated, for example "at least 5 items".
    /// </summary>
    [CreateAssetMenu(fileName = "IntVariableCondition", menuName = "GD/Conditions/Int Variable")]
    public class IntVariableCondition : Condition
    {
        [SerializeField] private IntVariable variable;
        [SerializeField] private CompareOperator compare = CompareOperator.GreaterOrEqual;
        [SerializeField] private int target;

        public override bool IsMet()
        {
            return variable != null && compare.Evaluate(variable.Value, target);
        }
    }
}
