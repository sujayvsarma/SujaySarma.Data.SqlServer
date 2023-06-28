namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// An operand that is a T-SQL expression
    /// </summary>
    public class SqlExpressionOperand : SqlOperand
    {

        /// <summary>
        /// The expression
        /// </summary>
        public SqlExpression Expression { get; init; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="expression">The expression</param>
        public SqlExpressionOperand(SqlExpression expression)
        {
            Expression = expression;
        }

        /// <summary>
        /// Returns a T-SQL compatible string
        /// </summary>
        /// <returns>A T-SQL compatible string</returns>
        public override string ToString()
            => $"({Expression})";
    }
}
