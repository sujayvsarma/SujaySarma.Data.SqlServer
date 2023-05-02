using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// Build an <see cref="IAsyncEnumerable{T}"/> into a <see cref="List{T}"/>
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="e">Instance of <see cref="IAsyncEnumerable{T}"/></param>
        /// <returns><see cref="List{T}"/> with information or an empty list</returns>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> e)
            where T : class
        {
            List<T> list = new();
            await foreach (T item in e)
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Test to see if <see cref="IAsyncEnumerable{T}"/> contains any values that match the provided <paramref name="validation"/>. 
        /// If no <paramref name="validation"/> is provided, then we test if the sequence contains any non-null and non-default values.
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="e">Instance of <see cref="IAsyncEnumerable{T}"/></param>
        /// <param name="validation">The check to perform on each item of <paramref name="e"/></param>
        /// <returns>'True' if sequence contains a non-null and non-default value or passes the <paramref name="validation"/></returns>
        public static async Task<bool> AnyAsync<T>(this IAsyncEnumerable<T> e, Predicate<T>? validation = null)
            where T : class
        {
            validation ??= (t) => ((t != null) && (t != default));
            IAsyncEnumerator<T> en = e.GetAsyncEnumerator();
            while (await en.MoveNextAsync())
            {
                if (validation(en.Current))
                {
                    return true;
                }
            }

            return false;
        }


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

    }
}
