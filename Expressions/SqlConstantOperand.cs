namespace SujaySarma.Data.SqlServer.Expressions
{
    /// <summary>
    /// An operand that is a literal constant. Example: "Hello", 42, true.
    /// </summary>
    public class SqlConstantOperand : SqlOperand
    {
        /// <summary>
        /// The value of the constant
        /// </summary>
        public object? Value { get; init; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="value">Value of the constant</param>
        public SqlConstantOperand(object? value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns a properly formatted T-SQL compatible string value
        /// </summary>
        /// <returns>A properly formatted T-SQL compatible string value</returns>
        public override string ToString()
            => Reflection.ReflectionUtils.GetSQLStringValue(Value, Attributes.EnumSerializationBehaviourEnum.AsInt, serializeToJson: false, quotedStrings: true);
    }



}
