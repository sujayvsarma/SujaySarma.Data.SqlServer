using System.Collections.Generic;

namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// An operand that is a list of (literal) constants, typically used in IN or NOT IN expressions.
    /// </summary>
    public class SqlListOfValuesOperand<TValue> : SqlOperand
    {
        /// <summary>
        /// The list of values
        /// </summary>
        public List<TValue> ListOfValues { get; init; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="values">Collection of values</param>
        public SqlListOfValuesOperand(IEnumerable<TValue> values)
        {
            ListOfValues = new();
            ListOfValues.AddRange(values);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="values">Collection of values</param>
        public SqlListOfValuesOperand(params TValue[] values)
        {
            ListOfValues = new();
            ListOfValues.AddRange(values);
        }

        /// <summary>
        /// Add a value
        /// </summary>
        /// <param name="value">Value to add</param>
        public void Add(TValue value)
            => ListOfValues.Add(value);


        /// <summary>
        /// Returns a comma-seperated list of T-SQL compatible string equivalents of the values stored
        /// </summary>
        /// <returns>A comma-seperated list of T-SQL compatible string equivalents of the values stored</returns>
        public override string ToString()
        {
            List<string> values = new();
            foreach(TValue value in ListOfValues)
            {
                values.Add(Reflection.ReflectionUtils.GetSQLStringValue(value, Attributes.EnumSerializationBehaviourEnum.AsInt, serializeToJson: false, quotedStrings: true));
            }

            return string.Join(",", values);
        }
    }
}
