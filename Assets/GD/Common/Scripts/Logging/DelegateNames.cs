using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GD
{
    /// <summary>
    /// Gives a readable name for a delegate, for debugging tools and step descriptions.
    /// </summary>
    public static class DelegateNames
    {
        private static readonly Regex LocalFunctionName = new(@"g__(\w+)\|");

        /// <summary>
        /// Returns, for example, "C_Won.IsMet" for a method on an asset, "CanSeePlayer" for a
        /// local function, or "(lambda)" for an unnamed method.
        /// </summary>
        public static string Describe(Delegate method)
        {
            if (method == null)
                return "(none)";

            string name = method.Method.Name;
            Match localFunction = LocalFunctionName.Match(name);

            if (localFunction.Success)
                return localFunction.Groups[1].Value;

            // The compiler gives lambdas names that start with an angle bracket.
            if (name.StartsWith("<"))
                return "(lambda)";

            return method.Target is Object owner && owner != null ? $"{owner.name}.{name}" : name;
        }
    }
}
