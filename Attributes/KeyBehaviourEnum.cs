namespace SujaySarma.Data.SqlServer.Attributes
{
    /// <summary>
    /// Behaviour of the property or field as a KEY
    /// </summary>
    public enum KeyBehaviourEnum
    {
        /// <summary>
        /// Is not a key
        /// </summary>
        None = 0,

        /// <summary>
        /// Primary key. Column will participate only in INSERT operations. During UPDATEs and DELETEs, the column 
        /// will be added to the WHERE clause automatically.
        /// </summary>
        PrimaryKey
    }
}
