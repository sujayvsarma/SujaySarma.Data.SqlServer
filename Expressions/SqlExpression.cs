namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// An expression is a fragment that contains at least one operand and an operator. In the context of this library, a SqlExpressions are purely 
    /// conditional expressions. Each operand may in-turn be another expression (nested expressions). Examples: (A = B), (A LIKE 'Hello') and so on.
    /// </summary>
    public class SqlExpression
    {

        /// <summary>
        /// The operator joining the two operands
        /// </summary>
        public SqlExpressionOperatorsEnum Operator { get; init; }

        /// <summary>
        /// Left-side operand
        /// </summary>
        public SqlOperand? LeftOperand { get; init; }

        /// <summary>
        /// Right-side operand
        /// </summary>
        public SqlOperand? RightOperand { get; init; }


        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="leftOperand">Left-side operand</param>
        /// <param name="operator">The operator</param>
        /// <param name="rightOperand">Right-side operand</param>
        protected SqlExpression(SqlOperand? leftOperand, SqlExpressionOperatorsEnum @operator, SqlOperand? rightOperand)
        {
            LeftOperand = leftOperand;
            Operator = @operator;
            RightOperand = rightOperand;
        }

        /// <summary>
        /// Returns a T-SQL compatible string representation of the expression
        /// </summary>
        /// <returns>A T-SQL compatible string representation of the expression</returns>
        public override string ToString()
          => Operator.Join(LeftOperand?.ToString(), RightOperand?.ToString());
    }

}
