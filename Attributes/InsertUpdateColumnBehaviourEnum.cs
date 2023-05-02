namespace SujaySarma.Data.SqlServer.Attributes
{
    /// <summary>
    /// How this column interacts with INSERT and UPDATE operations
    /// </summary>
    public enum InsertUpdateColumnBehaviourEnum
    {
        /// <summary>
        /// (Default) Participates in INSERT and UPDATE
        /// </summary>
        InsertAndUpdate = 0,

        /// <summary>
        /// Can only be inserted (Primary Keys)
        /// </summary>
        OnlyInsert,

        /// <summary>
        /// Do not include this column in INSERT or UPDATE (Calculated columns, ROWGUID, IDENTITY, etc)
        /// </summary>
        NeitherInsertNorUpdate
    }
}
