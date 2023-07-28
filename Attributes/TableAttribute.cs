using System;

namespace SujaySarma.Data.SqlServer.Attributes
{
    /// <summary>
    /// Provide name of the table the data for the class is stored in.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class TableAttribute : Attribute
    {

        /// <summary>
        /// Name of the table schema
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// Name of the table
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// If set, we use soft-delete by setting the IsDeleted flag to true.
        /// </summary>
        public bool UseSoftDelete { get; set; } = true;

        /// <summary>
        /// Provides information about the table used to contain the data for an object.
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        public TableAttribute(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            TableName = tableName;
        }

        /// <summary>
        /// Provides information about the table used to contain the data for an object.
        /// </summary>
        /// <param name="schemaName">Name of the table schema</param>
        /// <param name="tableName">Name of the table</param>
        public TableAttribute(string schemaName, string tableName)
        {
            if (string.IsNullOrWhiteSpace(schemaName))
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            SchemaName = schemaName;
            TableName = tableName;
        }

        /// <summary>
        /// Returns the schema.table name of the table defined.
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
            => $"[{SchemaName}].[{TableName}]";
    }
}
