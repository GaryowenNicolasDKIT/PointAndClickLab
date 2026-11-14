using System.Collections.Generic;
using UnityEngine;

namespace GD.Conditions
{
    /// <summary>
    /// A condition made from other conditions, which may themselves be groups.
    /// An empty group is never met, so that an unfinished asset cannot end the game.
    /// </summary>
    public abstract class ConditionGroup : Condition
    {
        [SerializeField] private List<Condition> conditions = new();

        protected override IEnumerable<Condition> Children => conditions;

        protected IReadOnlyList<Condition> Conditions => conditions;

        protected bool IsEmpty => conditions.Count == 0;
    }
}
