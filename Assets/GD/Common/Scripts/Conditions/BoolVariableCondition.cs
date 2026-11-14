using GD.Types;
using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// Met when a shared bool has the expected value, for example "has the key".
    /// </summary>
    [CreateAssetMenu(fileName = "BoolVariableCondition", menuName = "GD/Conditions/Bool Variable")]
    public class BoolVariableCondition : Condition
    {
        [SerializeField] private BoolVariable variable;
        [SerializeField] private bool expected = true;

        public override bool IsMet()
        {
            return variable != null && variable.Value == expected;
        }
    }
}
