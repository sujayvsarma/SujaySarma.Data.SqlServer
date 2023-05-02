namespace SujaySarma.Data.SqlServer.Attributes
{
    /// <summary>
    /// If the value is an Enum, how we serialize it to SQL
    /// </summary>
    public enum EnumSerializationBehaviourEnum
    {
        /// <summary>
        /// As its integer value
        /// </summary>
        AsInt = 0,

        /// <summary>
        /// As its string value
        /// </summary>
        AsString
    }
}
