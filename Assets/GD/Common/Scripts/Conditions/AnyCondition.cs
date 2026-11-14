using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// OR: met when at least one condition is met.
    /// </summary>
    [CreateAssetMenu(fileName = "AnyCondition", menuName = "GD/Conditions/Any (OR)")]
    public class AnyCondition : ConditionGroup
    {
        public override bool IsMet()
        {
            foreach (Condition condition in Conditions)
            {
                if (condition != null && condition.IsMet())
                    return true;
            }

            return false;
        }
    }
}
