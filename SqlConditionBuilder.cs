using System.Text;
using SujaySarma.Data.SqlServer.Expressions;

namespace SujaySarma.Data.SqlServer
{
    /// <summary>
    /// Builds a conditional expression. This is a one-way operation and cannot be decomposed/deconstructed to individual expressions. 
    /// This class uses fluent-style operations, end with a call to the overloaded ToString() to generate the T-SQL.
    /// </summary>
    public class SqlConditionBuilder
    {

        /// <summary>
        /// This is the first call you should make, initializes the builder with the first expression.
        /// </summary>
        /// <param name="expression">Expression to add to the builder</param>
        /// <returns>Reference to the created SqlCondition builder</returns>
        public static SqlConditionBuilder BeginWith(SqlExpression expression)
        {
            SqlConditionBuilder builder = new();
            builder._expression.Append(expression.ToString());
            return builder;
        }

        /// <summary>
        /// Add the next expression
        /// </summary>
        /// <param name="joiningOperator">Operator to join existing expression and the one being added</param>
        /// <param name="nextExpression">Expression to add to the condition</param>
        /// <returns>Reference to self</returns>
        public SqlConditionBuilder Add(SqlConditionalOperatorsEnum joiningOperator, SqlExpression nextExpression)
        {
            _expression.Append(joiningOperator.ToString());
            _expression.Append(nextExpression.ToString());

            return this;
        }

        /// <summary>
        /// Encloses everything added so-far into a grouping parenthesis
        /// </summary>
        /// <returns>Reference to self</returns>
        public SqlConditionBuilder Group()
        {
            _expression.Insert(0, '(');
            _expression.Append(')');

            return this;
        }

        /// <summary>
        /// Returns the entireity of the conditions added thus far
        /// </summary>
        /// <returns>T-SQL compatible condition string</returns>
        public override string ToString()
            => _expression.ToString();


        /// <summary>
        /// Initialize the internal structures
        /// </summary>
        private SqlConditionBuilder() => _expression = new();
        private readonly StringBuilder _expression;

    }
}
