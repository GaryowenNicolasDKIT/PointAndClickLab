using System;

namespace GD.Conditions
{
    /// <summary>
    /// How a value is compared with a target.
    /// </summary>
    public enum CompareOperator
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
    }

    /// <summary>Applies a CompareOperator to two values.</summary>
    public static class CompareOperatorExtensions
    {
        /// <summary>Applies this operator to a value and a target.</summary>
        public static bool Evaluate<T>(this CompareOperator compare, T value, T target) where T : IComparable<T>
        {
            int result = value.CompareTo(target);

            return compare switch
            {
                CompareOperator.Equal => result == 0,
                CompareOperator.NotEqual => result != 0,
                CompareOperator.Less => result < 0,
                CompareOperator.LessOrEqual => result <= 0,
                CompareOperator.Greater => result > 0,
                CompareOperator.GreaterOrEqual => result >= 0,
                _ => false
            };
        }
    }
}
