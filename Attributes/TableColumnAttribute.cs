using System;

namespace SujaySarma.Data.SqlServer.Attributes
{
    /// <summary>
    /// Provide the data table column name and other flags used the value for this property or field is stored in or retrieved from.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class TableColumnAttribute : TableColumnPropertiesAttributeBase
    {
        /// <summary>
        /// Name of the column
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// If set and the Clr type of the class/struct field/property is a complex type, it would be run through a Json 
        /// (de-)serialization spin.
        /// </summary>
        public bool JsonSerialize { get; set; } = false;

        /// <summary>
        /// When set, generate the value automatically. Use of this value is supported only for 'DateTime' and 'Guid' at this time.
        /// </summary>
        public bool AutoGenerateValue { get; set; } = false;

        /// <summary>
        /// Behaviour of the column in INSERT and UPDATE operations
        /// </summary>
        public InsertUpdateColumnBehaviourEnum InsertUpdateColumnBehaviour { get; set; } = InsertUpdateColumnBehaviourEnum.InsertAndUpdate;

        /// <summary>
        /// If the property/field is an Enum, then how it is serialized
        /// </summary>
        public EnumSerializationBehaviourEnum EnumSerializationBehaviour { get; set; } = EnumSerializationBehaviourEnum.AsInt;

        /// <summary>
        /// Behaviour of this property/field as a Key of some sort
        /// </summary>
        public KeyBehaviourEnum KeyBehaviour { get; set; } = KeyBehaviourEnum.None;
        

        /// <summary>
        /// Provides information about the table column used to contain the data for an object.
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        public TableColumnAttribute(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentNullException(nameof(columnName));
            }

            ColumnName = columnName;
        }
    }
}
