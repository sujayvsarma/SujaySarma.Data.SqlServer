using System;
using System.Data;
using System.Reflection;
using System.Text.Json;

using SujaySarma.Data.SqlServer.Attributes;
using SujaySarma.Data.SqlServer.Reflection;

namespace SujaySarma.Data.SqlServer
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class SqlTableContextExtensions
    {

        /// <summary>
        /// Create an instance of the given <paramref name="TObject"/> type and populate it using the provided <see cref="DataRow"/>
        /// </summary>
        /// <param name="TObject">Type of business object</param>
        /// <param name="row"><see cref="DataRow"/> containing data to populate into the instance</param>
        /// <returns>Instance of object. Never NULL</returns>
        public static object HydrateFrom(Type TObject, DataRow row)
        {
            if ((row.Table == default) || (row.Table.Columns.Count == 0))
            {
                throw new TypeLoadException($"The DataRow passed is not attached to a table, or the table has no schema. Object: '{TObject.Name}'");
            }

            TypeMetadata metadata = TypeMetadata.Discover(TObject);
            object instance = Activator.CreateInstance(TObject) ?? new TypeLoadException($"Unable to instantiate object of type '{TObject.Name}'.");

            foreach (MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>();
                if ((columnAttribute != null) && (row.Table.Columns.Contains(columnAttribute.ColumnName)))
                {
                    object? value = row[columnAttribute.ColumnName];
                    if ((value is DBNull) || (value == DBNull.Value))
                    {
                        value = null;
                    }

                    if (columnAttribute.JsonSerialize)
                    {
                        value = JsonSerializer.Deserialize($"{value ?? string.Empty}", member.GetFieldOrPropertyType());
                    }

                    ReflectionUtils.SetValue(instance, member, value);
                }
            }            

            return instance;
        }


        /// <summary>
        /// Create an instance of the given <typeparamref name="T"/> type and populate it using the provided <see cref="DataRow"/>
        /// </summary>
        /// <typeparam name="T">Type of business object</typeparam>
        /// <param name="row"><see cref="DataRow"/> containing data to populate into the instance</param>
        /// <returns>Instance of object. Never NULL</returns>
        public static T? HydrateFrom<T>(DataRow row)
            where T : class
            => (T?)HydrateFrom(typeof(T), row);

    }
}
