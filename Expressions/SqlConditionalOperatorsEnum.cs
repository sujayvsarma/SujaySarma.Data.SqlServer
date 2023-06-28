using System;

namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// Operators that join two expressions to form a cascading condition
    /// </summary>
    public enum SqlConditionalOperatorsEnum
    {
        /// <summary>
        /// No condition -- is the only portion of the condition
        /// </summary>
        None,

        /// <summary>
        /// AND condition
        /// </summary>
        And,

        /// <summary>
        /// OR condition
        /// </summary>
        Or
    }


    /// <summary>
    /// Extension methods that process operator enum constants
    /// </summary>
    public static partial class OperatorExtensions
    {

        /// <summary>
        /// Returns the T-SQL string representation of the operator
        /// </summary>
        /// <param name="conditionalOperator">Conditional operator</param>
        /// <returns>T-SQL string representation</returns>
        public static string ToString(this SqlConditionalOperatorsEnum conditionalOperator)
            => conditionalOperator switch
            {
                SqlConditionalOperatorsEnum.And => " AND ",
                SqlConditionalOperatorsEnum.Or => " OR ",

                _ => string.Empty
            };

        /// <summary>
        /// Pick the first non-empty expression
        /// </summary>
        /// <param name="expressions">List of expressions</param>
        /// <returns>First non-empty expression or string.Empty</returns>
        public static string PickFirstNonEmpty(params string?[] expressions)
        {
            foreach(string? expression in expressions)
            {
                if (! string.IsNullOrWhiteSpace(expression))
                {
                    return expression;
                }
            }

            return string.Empty;
        }
    }
}
