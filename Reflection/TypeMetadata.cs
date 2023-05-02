using System;
using System.Collections.Generic;
using System.Reflection;

using SujaySarma.Data.SqlServer.Attributes;

namespace SujaySarma.Data.SqlServer.Reflection
{
    /// <summary>
    /// Metadata collected about a business object
    /// </summary>
    internal class TypeMetadata
    {

        /// <summary>
        /// Table schema
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// Name of the table for the object
        /// </summary>
        public string TableName { get; set; } = default!;

        /// <summary>
        /// If the table uses soft-delete: TRUE if not set.
        /// </summary>
        public bool UseSoftDelete { get; set; } = true;

        /// <summary>
        /// Other properties of the class
        /// </summary>
        public List<MemberInfo> Members { get; set; } = new();

        /// <summary>
        /// Discover an object's metadata using reflection
        /// </summary>
        /// <typeparam name="TObject">Type of business object</typeparam>
        /// <returns>Type metadata</returns>
        /// <exception cref="TypeLoadException">Exceptions are thrown if object is missing key attributes or values</exception>
        public static TypeMetadata Discover<TObject>()
            where TObject : class
            => Discover(typeof(TObject));


        /// <summary>
        /// Discover an object's metadata using reflection
        /// </summary>
        /// <param name="TObject">Type of object</param>
        /// <returns>Type metadata</returns>
        /// <exception cref="TypeLoadException">Exceptions are thrown if object is missing key attributes or values</exception>
        public static TypeMetadata Discover(Type TObject)
        {
            Type classType = TObject;
            string cacheKeyName = classType.FullName ?? classType.Name;

            TableAttribute? tableAttribute = classType.GetCustomAttribute<TableAttribute>(true);
            if ((tableAttribute == default) || string.IsNullOrWhiteSpace(tableAttribute.TableName))
            {
                throw new TypeLoadException($"The type '{classType.Name}' does not have a [Table] attribute.");
            }

            TypeMetadata meta = new()
            {
                SchemaName = tableAttribute.SchemaName,
                TableName = tableAttribute.TableName,
                UseSoftDelete = tableAttribute.UseSoftDelete
            };

            foreach (MemberInfo member in classType.GetMembers(MEMBER_SEARCH_FLAGS))
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
                if (columnAttribute != null)
                {
                    // one small check
                    Type type = member.GetFieldOrPropertyType();
                    if (columnAttribute.AutoGenerateValue && (type != typeof(Guid)) && (type != typeof(DateTime)))
                    {
                        throw new InvalidOperationException($"'AutoGenerateValue' is supported only for 'Guid' and 'DateTime' types (Property/field: '{classType.FullName}.{member.Name}').");
                    }

                    if (columnAttribute.KeyBehaviour == KeyBehaviourEnum.PrimaryKey)
                    {
                        if (columnAttribute.InsertUpdateColumnBehaviour == InsertUpdateColumnBehaviourEnum.InsertAndUpdate)
                        {
                            // no updates allowed on p/f marked as PK
                            columnAttribute.InsertUpdateColumnBehaviour = InsertUpdateColumnBehaviourEnum.OnlyInsert;
                        }
                    }

                    meta.Members.Add(member);
                }
            }

            return meta;
        }

        private static readonly BindingFlags MEMBER_SEARCH_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public const string ISDELETED_COLUMN_NAME = "IsDeleted";
    }
}
