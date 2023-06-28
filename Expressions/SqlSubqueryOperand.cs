using System;

namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// An operand that is a T-SQL sub-query that will be evaluated by SQL Server at execution. 
    /// </summary>
    public class SqlSubqueryOperand : SqlOperand
    {
        /// <summary>
        /// The T-SQL sub-query
        /// </summary>
        public string Subquery { get; init; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="subquery">T-SQL sub-query</param>
        public SqlSubqueryOperand(string subquery)
        {
            if (string.IsNullOrWhiteSpace(subquery))
            {
                throw new ArgumentNullException(nameof(subquery), "Subquery cannot be Null or Empty for a SqlSubquery operand.");
            }

            Subquery = subquery;
        }

        /// <summary>
        /// Returns the value of the subquery
        /// </summary>
        /// <returns>Value of subquery as-is</returns>
        public override string ToString()
            => Subquery;
    }
}
