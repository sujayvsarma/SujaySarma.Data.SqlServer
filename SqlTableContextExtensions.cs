using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using SujaySarma.Data.SqlServer.Attributes;
using SujaySarma.Data.SqlServer.Fluid;
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

        /// <summary>
        /// Use the provided <paramref name="tableContext"/> to perform the query in <paramref name="queryBuilder"/>
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="queryBuilder">Instance of SqlQueryBuilder with the necessary parameters</param>
        /// <param name="tableContext">SqlTableContext to use to perform the query</param>
        /// <returns>IEnumerable collection of [T] returned by the query</returns>
        public static IEnumerable<T> Query<T>(this SqlQueryBuilder queryBuilder, SqlTableContext tableContext)
            where T : class
            => tableContext.Select<T>(queryBuilder);

        /// <summary>
        /// Use the provided <paramref name="tableContext"/> to perform the query in <paramref name="queryBuilder"/>
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="queryBuilder">Instance of SqlQueryBuilder with the necessary parameters</param>
        /// <param name="tableContext">SqlTableContext to use to perform the query</param>
        /// <returns>Single instance [T] or NULL as returned by the query</returns>
        public static T? QueryOneOrNull<T>(this SqlQueryBuilder queryBuilder, SqlTableContext tableContext)
            where T : class
            => tableContext.SelectOnlyResultOrNull<T>(queryBuilder);


        /// <summary>
        /// Execute the INSERT query provided in <paramref name="insertBuilder"/> against the <paramref name="tableContext"/>
        /// </summary>
        /// <typeparam name="T">Type of objects in the INSERT</typeparam>
        /// <param name="insertBuilder">Instance of SqlInsertBuilder</param>
        /// <param name="tableContext">SqlTableContext to use to execute the query</param>
        /// <returns>Number of rows affected on the SQL Server</returns>
        public static async Task<int> Execute<T>(this SqlInsertBuilder<T> insertBuilder, SqlTableContext tableContext)
            where T : class
            => await tableContext.ExecuteNonQueryAsync(insertBuilder.Build());

        /// <summary>
        /// Execute the INSERT query provided in <paramref name="insertBuilder"/> against the <paramref name="tableContext"/>
        /// </summary>
        /// <param name="insertBuilder">Instance of SqlInsertFromQueryBuilder</param>
        /// <param name="tableContext">SqlTableContext to use to execute the query</param>
        /// <returns>Number of rows affected on the SQL Server</returns>
        public static async Task<int> Execute(this SqlInsertFromQueryBuilder insertBuilder, SqlTableContext tableContext)
            => await tableContext.ExecuteNonQueryAsync(insertBuilder.Build());

        /// <summary>
        /// Execute the UPDATE query provided in <paramref name="updateBuilder"/> against the <paramref name="tableContext"/>
        /// </summary>
        /// <typeparam name="T">Type of objects in the UPDATE</typeparam>
        /// <param name="updateBuilder">Instance of SqlUpdateBuilder</param>
        /// <param name="tableContext">SqlTableContext to use to execute the query</param>
        /// <returns>Number of rows affected on the SQL Server</returns>
        public static async Task<int> Execute<T>(this SqlUpdateBuilder<T> updateBuilder, SqlTableContext tableContext)
            where T : class
            => await tableContext.ExecuteNonQueryAsync(updateBuilder.Build());

        /// <summary>
        /// Execute the UPDATE query provided in <paramref name="updateBuilder"/> against the <paramref name="tableContext"/>
        /// </summary>
        /// <param name="updateBuilder">Instance of SqlUpdateWithJoinsBuilder</param>
        /// <param name="tableContext">SqlTableContext to use to execute the query</param>
        /// <returns>Number of rows affected on the SQL Server</returns>
        public static async Task<int> Execute(this SqlUpdateWithJoinsBuilder updateBuilder, SqlTableContext tableContext)
            => await tableContext.ExecuteNonQueryAsync(updateBuilder.Build());

        /// <summary>
        /// Execute the DELETE query provided in <paramref name="deleteBuilder"/> against the <paramref name="tableContext"/>
        /// </summary>
        /// <typeparam name="T">Type of objects in the DELETE</typeparam>
        /// <param name="deleteBuilder">Instance of SqlDeleteBuilder</param>
        /// <param name="tableContext">SqlTableContext to use to execute the query</param>
        /// <returns>Number of rows affected on the SQL Server</returns>
        public static async Task<int> Execute<T>(this SqlDeleteBuilder<T> deleteBuilder, SqlTableContext tableContext)
            where T : class
            => await tableContext.ExecuteNonQueryAsync(deleteBuilder.Build());


    }
}
