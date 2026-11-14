using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// XOR: met when exactly one condition is met. With three or more conditions this differs
    /// from a chained XOR, which would also be true when an odd number are met.
    /// </summary>
    [CreateAssetMenu(fileName = "XorCondition", menuName = "GD/Conditions/Exactly One (XOR)")]
    public class XorCondition : ConditionGroup
    {
        public override bool IsMet()
        {
            int metCount = 0;

            foreach (Condition condition in Conditions)
            {
                if (condition != null && condition.IsMet())
                    metCount++;

                if (metCount > 1)
                    return false;
            }

            return metCount == 1;
        }
    }
}
