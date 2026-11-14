using System.Collections.Generic;
using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// NOT: met when the inner condition is not met. An empty slot counts as not met,
    /// so that an unfinished asset cannot end the game.
    /// </summary>
    [CreateAssetMenu(fileName = "NotCondition", menuName = "GD/Conditions/Not")]
    public class NotCondition : Condition
    {
        [SerializeField] private Condition condition;

        protected override IEnumerable<Condition> Children => new[] { condition };

        public override bool IsMet()
        {
            return condition != null && !condition.IsMet();
        }
    }
}
