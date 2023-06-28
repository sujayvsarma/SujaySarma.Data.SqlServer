using System;

namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// An operand that is a fieldname of a table
    /// </summary>
    public class SqlFieldNameOperand : SqlOperand
    {
        /// <summary>
        /// The name of the field
        /// </summary>
        public string FieldName { get; init; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        public SqlFieldNameOperand(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName), "Field name cannot be Null or Empty for a SqlFieldName operand.");
            }

            FieldName = fieldName;
        }


        /// <summary>
        /// Returns a properly quoted T-SQL compatible fieldname string
        /// </summary>
        /// <returns>A properly quoted T-SQL compatible fieldname string</returns>
        public override string ToString()
            => $"[{FieldName}]";
    }



}
