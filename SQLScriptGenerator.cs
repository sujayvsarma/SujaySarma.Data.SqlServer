using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.Data.SqlClient;

using SujaySarma.Data.SqlServer.Attributes;
using SujaySarma.Data.SqlServer.Reflection;

namespace SujaySarma.Data.SqlServer
{
    /// <summary>
    /// Generates SQL scripts
    /// </summary>
    public static class SQLScriptGenerator
    {
        /// <summary>
        /// Generate SELECT statement - Can generate only simple (without JOINs etc) SELECT statements. If no parameters are provided, selects all rows as per 
        /// the database-driven sorting order.
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="parameters">The parameters for the WHERE clause. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="sorting">Sorting for columns. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="rowCount">Number of rows (TOP ??) to select. Zero or NULL for all rows.</param>
        /// <returns>SQL SELECT string</returns>
        public static string GetSelectStatement<TObject>(IDictionary<string, object?>? parameters = null, IDictionary<string, SortOrderEnum>? sorting = null, int? rowCount = null) 
            where TObject : class
        {
            List<string> whereClause = new();

            if ((parameters != null) && (parameters != default) && (parameters != default(IDictionary<string, object?>)) && (parameters.Count > 0))
            {
                foreach(string colName in parameters.Keys)
                {
                    whereClause.Add($"([{colName}] = {ReflectionUtils.GetSQLStringValue(parameters[colName])})");
                }
            }

            return string.Join(" AND ", whereClause);
        }

        /// <summary>
        /// Generate SELECT statement - Can generate only simple (without JOINs etc) SELECT statements. If no parameters are provided, selects all rows as per 
        /// the database-driven sorting order.
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="whereClause">Pre-composed WHERE clause -- without the "WHERE" word</param>
        /// <param name="sorting">Sorting for columns. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="rowCount">Number of rows (TOP ??) to select. Zero or NULL for all rows.</param>
        /// <returns>SQL SELECT string</returns>
        public static string GetSelectStatement<TObject>(string? whereClause = null, IDictionary<string, SortOrderEnum>? sorting = null, int? rowCount = null)
            where TObject : class
        {
            List<string> columnNames = new(), sortClause = new();
            TypeMetadata metadata = TypeMetadata.Discover<TObject>();
            foreach (MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
                if ((columnAttribute == null) || (columnAttribute.InsertUpdateColumnBehaviour == InsertUpdateColumnBehaviourEnum.NeitherInsertNorUpdate))
                {
                    continue;
                }

                columnNames.Add($"[{columnAttribute.ColumnName}]");
            }

            if ((sorting != null) && (sorting != default) && (sorting != default(IDictionary<string, SortOrderEnum?>)) && (sorting.Count > 0))
            {
                foreach (string colName in sorting.Keys)
                {
                    sortClause.Add($"[{colName}] {sorting[colName]}");
                }
            }

            return string.Join(
                    ' ',
                        "SELECT",
                        (((rowCount == null) || (rowCount == default) || (rowCount == default(int)) || (rowCount < 1)) ? "" : $"TOP {rowCount}"),
                        string.Join(',', columnNames),
                        "FROM",
                        $"[{metadata.TableName}]",
                        "WITH (NOLOCK)",
                        ((!string.IsNullOrWhiteSpace(whereClause)) ? string.Join(' ', "WHERE", whereClause) : ""),
                        ((sortClause.Count > 0) ? string.Join(' ', "ORDER BY", string.Join(',', sortClause)) : "")
                );
        }


        /// <summary>
        /// Generate SELECT statement - Can generate only simple (without JOINs etc) SELECT statements. If no parameters are provided, selects all rows as per 
        /// the database-driven sorting order.
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="parameters">The parameters for the WHERE clause. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="sorting">Sorting for columns. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="rowCount">Number of rows (TOP ??) to select. Zero or NULL for all rows.</param>
        /// <returns>SqlCommand with the parameterized statement</returns>
        public static SqlCommand GetSqlCommandForSelect<TObject>(IDictionary<string, object?>? parameters = null, IDictionary<string, SortOrderEnum>? sorting = null, int? rowCount = null)
            where TObject : class
        {
            SqlCommand cmd = new(GetSelectStatement<TObject>(parameters, sorting, rowCount));
            if ((parameters != null) && (parameters != default) && (parameters != default(IDictionary<string, object?>)) && (parameters.Count > 0))
            {
                foreach (string colName in parameters.Keys)
                {
                    cmd.Parameters.AddWithValue($"@param{colName}", parameters[colName]);
                }
            }

            return cmd;
        }

        /// <summary>
        /// Generate SELECT statement - Can generate only simple (without JOINs etc) SELECT statements. If no parameters are provided, selects all rows as per 
        /// the database-driven sorting order.
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="conditionBuilder">Collection of conditions to use for the WHERE clause</param>
        /// <param name="sorting">Sorting for columns. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="rowCount">Number of rows (TOP ??) to select. Zero or NULL for all rows.</param>
        /// <returns>SqlCommand with the parameterized statement</returns>
        public static SqlCommand GetSqlCommandForSelect<TObject>(SqlConditionBuilder? conditionBuilder = null, IDictionary<string, SortOrderEnum>? sorting = null, int? rowCount = null)
            where TObject : class
            => new(GetSelectStatement<TObject>(conditionBuilder?.ToString(), sorting, rowCount));



        /// <summary>
        /// Generate INSERT statement
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="instance">Instance of object</param>
        /// <returns>SQL INSERT string</returns>
        public static string GetInsertStatement<TObject>(TObject instance)
            where TObject : class
        {
            List<string> columnNames = new(), values = new();

            TypeMetadata metadata = TypeMetadata.Discover<TObject>();
            foreach(MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
                if ((columnAttribute == null) || (columnAttribute.InsertUpdateColumnBehaviour == InsertUpdateColumnBehaviourEnum.NeitherInsertNorUpdate))
                {
                    continue;
                }

                string sqlValue = ReflectionUtils.GetSQLStringValue(
                        ReflectionUtils.GetValue<TObject>(instance, member), 
                        columnAttribute.EnumSerializationBehaviour,
                        columnAttribute.JsonSerialize
                    );

                columnNames.Add($"[{columnAttribute.ColumnName}]");
                values.Add(sqlValue);
            }

            return string.Join(
                    ' ',
                        "INSERT INTO",
                        $"[{metadata.SchemaName}].[{metadata.TableName}]",
                        "(",
                        string.Join(',', columnNames),
                        ") VALUES (",
                        string.Join(',', values),
                        ");"
                );
        }

        /// <summary>
        /// Generate UPDATE statement
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="instance">Instance of object</param>
        /// <returns>SQL UPDATE string</returns>
        public static string GetUpdateStatement<TObject>(TObject instance)
            where TObject : class
        {
            TypeMetadata metadata = TypeMetadata.Discover<TObject>();
            List<string> conditions = new(), updateValues = new();

            foreach (MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
                if (columnAttribute == null)
                {
                    continue;
                }

                string sqlValue = ReflectionUtils.GetSQLStringValue(
                        ReflectionUtils.GetValue<TObject>(instance, member),
                        columnAttribute.EnumSerializationBehaviour,
                        columnAttribute.JsonSerialize
                    );

                switch (columnAttribute.InsertUpdateColumnBehaviour)
                {
                    case InsertUpdateColumnBehaviourEnum.OnlyInsert:
                    case InsertUpdateColumnBehaviourEnum.NeitherInsertNorUpdate:
                        if (columnAttribute.KeyBehaviour == KeyBehaviourEnum.PrimaryKey)
                        {
                            conditions.Add($"([{columnAttribute.ColumnName}] = {sqlValue})");
                        }
                        break;

                    case InsertUpdateColumnBehaviourEnum.InsertAndUpdate:
                        updateValues.Add($"[{columnAttribute.ColumnName}] = {sqlValue}");
                        break;
                }                
            }

            return string.Join(
                    ' ',
                    "UPDATE",
                    $"[{metadata.SchemaName}].[{metadata.TableName}]",
                    "SET",
                    string.Join(',', updateValues),
                    ((conditions.Count > 0) ? $"WHERE ({string.Join(" AND ", conditions)})" : ""),
                    ";"
                );
        }

        /// <summary>
        /// Generate a T-SQL MERGE (UPSERT) statement
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="instance">Instance of object</param>
        /// <returns>SQL MERGE string</returns>
        public static string GetMergeStatement<TObject>(TObject instance)
            where TObject : class
        {
            TypeMetadata metadata = TypeMetadata.Discover<TObject>();
            List<string> columnNames = new(), values = new(), joinConditions = new();
            string insertStatement = string.Empty, updateStatement = string.Empty;

            foreach (MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
                if ((columnAttribute == null) || (columnAttribute.InsertUpdateColumnBehaviour == InsertUpdateColumnBehaviourEnum.NeitherInsertNorUpdate))
                {
                    continue;
                }

                string sqlValue = ReflectionUtils.GetSQLStringValue(
                        ReflectionUtils.GetValue<TObject>(instance, member),
                        columnAttribute.EnumSerializationBehaviour,
                        columnAttribute.JsonSerialize
                    );

                columnNames.Add($"[{columnAttribute.ColumnName}]");
                values.Add(sqlValue);
                
                if (columnAttribute.KeyBehaviour == KeyBehaviourEnum.PrimaryKey)
                {
                    joinConditions.Add($"(target.[{columnAttribute.ColumnName}] = source.[{columnAttribute.ColumnName}])");
                }
            }

            insertStatement = string.Join(
                    ' ',
                        "INSERT",
                        "(",
                        string.Join(',', columnNames),
                        ") VALUES (",
                        string.Join(',', columnNames.Select(n => $"source.[{n}]")),
                        ");"
                );

            updateStatement = string.Join(
                    ' ',
                        "UPDATE SET",
                        string.Join(',', columnNames.Select(n => $"[{n}] = source.[{n}]"))
                );

            return string.Join(
                    ' ',
                        "MERGE",
                        $"[{metadata.SchemaName}].[{metadata.TableName}] as target",
                        "USING (VALUES(",
                        string.Join(',', values),
                        ")) AS source (",
                        string.Join(',', columnNames),
                        ") ON (",
                        string.Join(" AND ", joinConditions),
                        ") WHEN MATCHED THEN",
                        updateStatement,
                        "WHEN NOT MATCHED THEN",
                        insertStatement
                );
        }


        /// <summary>
        /// Generate DELETE statement
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="instance">Instance of object</param>
        /// <returns>SQL DELETE string</returns>
        public static string GetDeleteStatement<TObject>(TObject instance)
            where TObject : class
        {
            TypeMetadata metadata = TypeMetadata.Discover<TObject>();
            List<string> conditions = new();

            foreach (MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
                if ((columnAttribute == null) || (columnAttribute.KeyBehaviour != KeyBehaviourEnum.PrimaryKey))
                {
                    continue;
                }

                string sqlValue = ReflectionUtils.GetSQLStringValue(
                        ReflectionUtils.GetValue<TObject>(instance, member),
                        columnAttribute.EnumSerializationBehaviour,
                        columnAttribute.JsonSerialize
                    );

                conditions.Add($"([{columnAttribute.ColumnName}] = {sqlValue})");
            }

            if (metadata.UseSoftDelete)
            {
                return string.Join(
                    ' ',
                    "UPDATE",
                    $"[{metadata.SchemaName}].[{metadata.TableName}]",
                    $"SET [{TypeMetadata.ISDELETED_COLUMN_NAME}] = 1",
                    ((conditions.Count > 0) ? $"WHERE ({string.Join(" AND ", conditions)})" : ""),
                    ";"
                );
            }

            return string.Join(
                    ' ',
                    "DELETE FROM",
                    $"[{metadata.SchemaName}].[{metadata.TableName}]",
                    ((conditions.Count > 0) ? $"WHERE ({string.Join(" AND ", conditions)})" : ""),
                    ";"
                );
        }
    }
}
