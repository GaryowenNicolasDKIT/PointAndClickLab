using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// AND: met when every condition is met. An empty slot counts as not met.
    /// </summary>
    [CreateAssetMenu(fileName = "AllCondition", menuName = "GD/Conditions/All (AND)")]
    public class AllCondition : ConditionGroup
    {
        public override bool IsMet()
        {
            if (IsEmpty)
                return false;

            foreach (Condition condition in Conditions)
            {
                if (condition == null || !condition.IsMet())
                    return false;
            }

            return true;
        }
    }
}
