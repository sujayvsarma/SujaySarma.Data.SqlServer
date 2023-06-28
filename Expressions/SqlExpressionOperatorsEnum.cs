using System;

namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// Operators used in SqlExpressions
    /// </summary>
    public enum SqlExpressionOperatorsEnum
    {
        /// <summary>
        /// "="
        /// </summary>
        EqualTo,

        /// <summary>
        /// "&lt;&gt;"
        /// </summary>
        NotEqualTo,

        /// <summary>
        /// "&lt;"
        /// </summary>
        LessThan,

        /// <summary>
        /// "&lt;="
        /// </summary>
        LessThanOrEqualTo,

        /// <summary>
        /// "&gt;"
        /// </summary>
        GreaterThan,

        /// <summary>
        /// "&gt;="
        /// </summary>
        GreaterThanOrEqualTo,

        /// <summary>
        /// "LIKE"
        /// </summary>
        Like,

        /// <summary>
        /// "NOT LIKE"
        /// </summary>
        NotLike,

        /// <summary>
        /// "IN"
        /// </summary>
        In,

        /// <summary>
        /// "NOT IN"
        /// </summary>
        NotIn,

        /// <summary>
        /// "IS NULL"
        /// </summary>
        IsNull,

        /// <summary>
        /// "IS NOT NULL"
        /// </summary>
        IsNotNull

    }

    /// <summary>
    /// Extension methods that process operator enum constants
    /// </summary>
    public static partial class OperatorExtensions
    {
        /// <summary>
        /// Returns the T-SQL string representation of the operator
        /// </summary>
        /// <param name="expressionOperator">Expression operator</param>
        /// <returns>T-SQL string representation</returns>
        public static string ToString(this SqlExpressionOperatorsEnum expressionOperator)
            => expressionOperator switch
            {
                SqlExpressionOperatorsEnum.EqualTo => " = ",
                SqlExpressionOperatorsEnum.NotEqualTo => " <> ",
                SqlExpressionOperatorsEnum.LessThan => " < ",
                SqlExpressionOperatorsEnum.LessThanOrEqualTo => " <= ",
                SqlExpressionOperatorsEnum.GreaterThan => " > ",
                SqlExpressionOperatorsEnum.GreaterThanOrEqualTo => " >= ",
                SqlExpressionOperatorsEnum.Like => " LIKE ",
                SqlExpressionOperatorsEnum.NotLike => " NOT LIKE ",
                SqlExpressionOperatorsEnum.In => " IN ",
                SqlExpressionOperatorsEnum.NotIn => " NOT IN ",
                SqlExpressionOperatorsEnum.IsNull => " IS NULL",

                _ => " IS NOT NULL"
            };


        /// <summary>
        /// Join two operands
        /// </summary>
        /// <param name="expressionOperator">Expression operator joining the operands</param>
        /// <param name="leftOperand">Left-side Expression</param>
        /// <param name="rightOperand">Right-side Expression</param>
        /// <returns>T-SQL condition fragment</returns>
        public static string Join(this SqlExpressionOperatorsEnum expressionOperator, string? leftOperand, string? rightOperand)
        {
            if (string.IsNullOrWhiteSpace(leftOperand) && string.IsNullOrWhiteSpace(rightOperand))
            {
                return string.Empty;
            }

            switch (expressionOperator)
            {
                case SqlExpressionOperatorsEnum.EqualTo: 
                case SqlExpressionOperatorsEnum.NotEqualTo:
                case SqlExpressionOperatorsEnum.LessThan:
                case SqlExpressionOperatorsEnum.LessThanOrEqualTo:
                case SqlExpressionOperatorsEnum.GreaterThan:
                case SqlExpressionOperatorsEnum.GreaterThanOrEqualTo:
                case SqlExpressionOperatorsEnum.Like:
                case SqlExpressionOperatorsEnum.NotLike:
                    if (string.IsNullOrWhiteSpace(leftOperand))
                    {
                        throw new ArgumentNullException(nameof(leftOperand), $"Both expressions must be provided for '{expressionOperator}'.");
                    }

                    if (string.IsNullOrWhiteSpace(rightOperand))
                    {
                        throw new ArgumentNullException(nameof(rightOperand), $"Both expressions must be provided for '{expressionOperator}'.");
                    }

                    return $"({leftOperand}{ToString(expressionOperator)}{rightOperand})";

                case SqlExpressionOperatorsEnum.In:
                case SqlExpressionOperatorsEnum.NotIn:
                    if (string.IsNullOrWhiteSpace(leftOperand))
                    {
                        throw new ArgumentNullException(nameof(leftOperand), $"Both expressions must be provided for '{expressionOperator}'.");
                    }

                    if (string.IsNullOrWhiteSpace(rightOperand))
                    {
                        throw new ArgumentNullException(nameof(rightOperand), $"Both expressions must be provided for '{expressionOperator}'.");
                    }

                    return $"({leftOperand}{ToString(expressionOperator)}({rightOperand}))";

                case SqlExpressionOperatorsEnum.IsNull:
                case SqlExpressionOperatorsEnum.IsNotNull:
                    return $"({PickFirstNonEmpty(leftOperand, rightOperand)}{ToString(expressionOperator)})";

                default:
                    break;
            }


            return string.Empty;
        }

    }
}
