using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

using SujaySarma.Data.SqlServer.Reflection;

namespace SujaySarma.Data.SqlServer
{
    /// <summary>
    /// Enables connection-less SQL Server table interaction
    /// </summary>
    public partial class SqlTableContext
    {
        #region Wrappers

        /// <summary>
        /// Executes a SELECT
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="parameters">The parameters for the WHERE clause. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="sorting">Sorting for columns. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="rowCount">Number of rows (TOP ??) to select. Zero or NULL for all rows.</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> Select<T>(IDictionary<string, object?> parameters, IDictionary<string, SortOrderEnum>? sorting = null, int? rowCount = null)
            where T : class
            => Select<T>(SQLScriptGenerator.GetSqlCommandForSelect<T>(parameters, sorting, rowCount));

        /// <summary>
        /// Executes a SELECT and returns a single result or NULL (if none found)
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="parameters">The parameters for the WHERE clause. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <param name="sorting">Sorting for columns. Key must be the TABLE COLUMN name and NOT the property name!</param>
        /// <returns>Enumeration of object instances</returns>
        public T? SelectOnlyResultOrNull<T>(IDictionary<string, object?> parameters, IDictionary<string, SortOrderEnum>? sorting = null)
            where T : class
        {
            IEnumerable<T> items = Select<T>(SQLScriptGenerator.GetSqlCommandForSelect<T>(parameters, sorting, 1));
            if (items.Any())
            {
                return items.First();
            }

            return null;
        }

        /// <summary>
        /// Executes a SELECT and returns a single result or NULL (if none found)
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">The query to run</param>
        /// <returns>Enumeration of object instances</returns>
        public T? SelectOnlyResultOrNull<T>(string query)
            where T : class
        {
            IEnumerable<T> items = Select<T>(query);
            if (items.Any())
            {
                return items.First();
            }

            return null;
        }


        /// <summary>
        /// Executes a SELECT
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> Select<T>(string query) where T : class
            => ExecuteQuery<T>(query);

        /// <summary>
        /// Executes a SELECT
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> Select<T>(SqlCommand query) where T : class
            => ExecuteQuery<T>(query);


        /// <summary>
        /// Execute an INSERT
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to insert</param>
        /// <returns>Total number of rows inserted</returns>
        public async Task<int> InsertAsync<T>(params T[] data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Insert, data);

        /// <summary>
        /// Execute an INSERT
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to insert</param>
        /// <returns>Total number of rows inserted</returns>
        public async Task<int> InsertAsync<T>(IEnumerable<T> data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Insert, data);

        /// <summary>
        /// Execute an UPDATE
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to update</param>
        /// <returns>Total number of rows updated</returns>
        public async Task<int> UpdateAsync<T>(params T[] data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Update, data);

        /// <summary>
        /// Execute an UPDATE
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to update</param>
        /// <returns>Total number of rows updated</returns>
        public async Task<int> UpdateAsync<T>(IEnumerable<T> data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Update, data);

        /// <summary>
        /// Execute a MERGE (Upsert)
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to insert or update</param>
        /// <returns>Total number of rows inserted/updated</returns>
        public async Task<int> UpsertAsync<T>(params T[] data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Upsert, data);

        /// <summary>
        /// Execute a MERGE (Upsert)
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to insert or update</param>
        /// <returns>Total number of rows inserted/updated</returns>
        public async Task<int> UpsertAsync<T>(IEnumerable<T> data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Upsert, data);


        /// <summary>
        /// Execute a DELETE
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to delete</param>
        /// <returns>Total number of rows deleted</returns>
        public async Task<int> DeleteAsync<T>(params T[] data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Delete, data);

        /// <summary>
        /// Execute a DELETE
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="data">Collection of object instances to delete</param>
        /// <returns>Total number of rows deleted</returns>
        public async Task<int> DeleteAsync<T>(IEnumerable<T> data) where T : class
            => await ExecuteNonQueryAsync<T>(SqlStatementType.Delete, data);


        /// <summary>
        /// Execute a stored procedure and fetch the results
        /// </summary>
        /// <param name="procedureName">Stored procedure to run</param>
        /// <param name="inParameters">[OPTIONAL] Dictionary of IN parameters (keys are parameter names, values are param values)</param>
        /// <param name="outParameters">[OPTIONAL] Dictionary of OUT parameters. If this dictionary is NULL, then no out parameters are retrieved</param>
        /// <param name="commandTimeout">[OPTIONAL] Timeout of command. Default of 30 seconds.</param>
        public StoredProcedureExecutionResult ExecuteStoredProcedure(string procedureName, Dictionary<string, object?>? inParameters = null, Dictionary<string, object?>? outParameters = null, int commandTimeout = 30)
        {
            StoredProcedureExecutionResult result = new()
            {
                IsError = false,
                Messages = null,
                Exception = null,
                Results = null,
                ProcedureName = procedureName,
                ReturnParameters = new(),
                ReturnValue = 0
            };

            try
            {
                DataSet ds = new();
                using SqlConnection cn = new(_connectionString);
                cn.Open();

                using SqlCommand cmd = new(procedureName, cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = commandTimeout;

                if ((inParameters != null) && (inParameters.Count > 0))
                {
                    foreach (string key in inParameters.Keys)
                    {
                        cmd.Parameters.Add(CreateParameter(key, inParameters[key], ParameterDirection.Input));
                    }
                }

                if ((outParameters != null) && (outParameters.Count > 0))
                {
                    foreach (string key in outParameters.Keys)
                    {
                        if (outParameters[key] == null)
                        {
                            throw new ArgumentException(string.Format("Value of [out] parameter [{0}] cannot be passed as [NULL] since the underlying layer cannot guess the datatype.", key));
                        }

                        cmd.Parameters.Add(CreateParameter(key, outParameters[key], ParameterDirection.Output));
                    }
                }

                cmd.Parameters.Add(CreateParameter("@returnValue", null, ParameterDirection.ReturnValue));

                using SqlDataAdapter da = new(cmd);
                da.Fill(ds);

                if (cmd.Parameters["@returnvalue"].Value != null)
                {
                    if (int.TryParse(cmd.Parameters["@returnvalue"].Value.ToString(), out int ret))
                    {
                        result.ReturnValue = ret;

                        if (ret < 0)
                        {
                            result.IsError = true;
                            result.Messages = $"Stored procedure returned {ret} (error condition) instead of throwing an exception.";
                        }
                    }
                }

                if (outParameters != null)
                {
                    result.ReturnParameters = new();
                    foreach (string key in outParameters.Keys)
                    {
                        if (cmd.Parameters[key] != null)
                        {
                            result.ReturnParameters.Add(key, cmd.Parameters[key].Value);
                        }
                    }
                }

                if (!result.IsError)
                {
                    NameTables(ds);
                    result.Results = ds;
                }
            }
            catch (Exception mainEx)
            {
                result.IsError = true;
                result.Messages = mainEx.Message;
                result.Exception = mainEx;
            }

            // result return
            return result;

            // create sql parameter (local helper)
            static SqlParameter CreateParameter(string name, object? value, ParameterDirection direction)
            {
                SqlParameter p = new((name.StartsWith("@") ? name : ("@" + name)), value)
                {
                    Direction = direction
                };
                return p;
            }

            // name the tables in the dataset (local helper)
            static DataSet NameTables(DataSet? dataSet)
            {
                if ((dataSet == null) || (dataSet == default) || (dataSet == default(DataSet)))
                {
                    return new();
                }

                if (dataSet.Tables.Count < 1)
                {
                    return dataSet;
                }

                for (int tableIndex = 0; tableIndex < dataSet.Tables.Count; tableIndex++)
                {
                    if (string.IsNullOrEmpty(dataSet.Tables[tableIndex].TableName))
                    {
                        dataSet.Tables[tableIndex].TableName = $"Table{tableIndex + 1}";
                    }
                }

                return dataSet;
            }
        }

        #endregion

        #region Implementation functions

        /// <summary>
        /// Execute a query yielding an enumeration of object instances
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> ExecuteQuery<T>(string query)
            where T : class
        {
            DataTable table = new();
            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                using SqlDataAdapter da = new(cmd);
                da.Fill(table);
            }

            if ((table.Columns.Count > 0) && (table.Rows.Count > 0))
            {
                foreach (DataRow row in table.Rows)
                {
                    yield return ReflectionUtils.Populate<T>(row);
                }
            }
        }

        /// <summary>
        /// Execute a query yielding an enumeration of object instances
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> ExecuteQuery<T>(SqlCommand query)
            where T : class
        {
            DataTable table = new();
            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();
                query.Connection = cn;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(query.CommandText);
                }
#endif

                using SqlDataAdapter da = new(query);
                da.Fill(table);
            }

            if ((table.Columns.Count > 0) && (table.Rows.Count > 0))
            {
                foreach (DataRow row in table.Rows)
                {
                    yield return ReflectionUtils.Populate<T>(row);
                }
            }
        }


        /// <summary>
        /// Execute a query yielding an enumeration of data rows
        /// </summary>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of DataRows</returns>
        public IEnumerable<DataRow> ExecuteQueryRows(string query)
        {
            DataTable table = new();
            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                using SqlDataAdapter da = new(cmd);
                da.Fill(table);
            }

            if ((table.Columns.Count > 0) && (table.Rows.Count > 0))
            {
                foreach (DataRow row in table.Rows)
                {
                    yield return row;
                }
            }
        }

        /// <summary>
        /// Execute a query yielding a complete DataTable
        /// </summary>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>DataTable</returns>
        public DataTable ExecuteQueryTable(string query)
        {
            DataTable table = new();
            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                using SqlDataAdapter da = new(cmd);
                da.Fill(table);
            }

            if ((table.Columns.Count > 0) && (table.Rows.Count > 0))
            {
                return table;
            }

            return new();
        }

        /// <summary>
        /// Execute a query yielding a DataSet
        /// </summary>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>DataSet</returns>
        public DataSet ExecuteQueryTables(string query)
        {
            DataSet ds = new();
            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                using SqlDataAdapter da = new(cmd);
                da.Fill(ds);
            }

            if (ds.Tables.Count > 0)
            {
                return ds;
            }

            return new();
        }

        /// <summary>
        /// Get binary data out of a SQL table
        /// </summary>
        /// <param name="query">SELECT query to run</param>
        /// <param name="expectedLength">Expected length of data</param>
        /// <param name="commandTimeout">[OPTIONAL] Timeout of command. Default of 30 seconds</param>
        /// <returns>Byte array containing the binary content requested or empty byte array. If there was a problem will return an empty array instead of throwing an exception.</returns>
        public byte[] ExecuteSelectBinaryContent(string query, int expectedLength, int commandTimeout = 30)
        {
            byte[] data = new byte[expectedLength];
            try
            {
                using SqlConnection cn = new(_connectionString);
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;
                cmd.CommandTimeout = commandTimeout;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                using SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
                if (dr.Read())
                {
                    dr.GetBytes(0, 0, data, 0, expectedLength);
                }
            }
            catch
            {
                data = Array.Empty<byte>();
            }

            return data;
        }

        /// <summary>
        /// Get a single value output (instead of a statement). This is similar to the ExecuteScalar statement.
        /// </summary>
        /// <param name="query">SELECT query to run</param>
        /// <param name="commandTimeout">[OPTIONAL] Timeout of command. Default of 30 seconds.</param>
        /// <returns>The single scalar value -- will return 'default(T)' and not NULL!</returns>
        public async Task<T?> ExecuteScalarAsync<T>(string query, int commandTimeout = 30)
        {
            try
            {
                using SqlConnection cn = new(_connectionString);
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;
                cmd.CommandTimeout = commandTimeout;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                object? v = await cmd.ExecuteScalarAsync();
                return (T?)(((v is DBNull) || (v == DBNull.Value)) ? default(T) : v);     // return CLR null instead of DBNull
            }
            catch
            {
            }

#pragma warning disable IDE0034
            return default(T);
#pragma warning restore IDE0034
        }

        /// <summary>
        /// Execute a non-query operation
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="operationType">Type of operation</param>
        /// <param name="data">Instances of objects to insert/update/delete</param>
        /// <returns>Total number of rows affected in the backing data store</returns>
        public async Task<int> ExecuteNonQueryAsync<T>(SqlStatementType operationType, params T[] data)
            where T : class
            => await ExecuteNonQueryAsync<T>(operationType, data.AsEnumerable<T>());

        /// <summary>
        /// Execute a non-query operation
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="operationType">Type of operation</param>
        /// <param name="data">Instances of objects to insert/update/delete</param>
        /// <returns>Total number of rows affected in the backing data store</returns>
        public async Task<int> ExecuteNonQueryAsync<T>(SqlStatementType operationType, IEnumerable<T> data)
            where T : class
        {
            int rowsAffected = 0;

            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.Text;

                foreach (T item in data)
                {
                    cmd.CommandText = operationType switch
                    {
                        SqlStatementType.Insert => SQLScriptGenerator.GetInsertStatement<T>(item),
                        SqlStatementType.Update => SQLScriptGenerator.GetUpdateStatement<T>(item),
                        SqlStatementType.Delete => SQLScriptGenerator.GetDeleteStatement<T>(item),
                        SqlStatementType.Upsert => SQLScriptGenerator.GetMergeStatement<T>(item),

                        _ => throw new NotSupportedException($"'{nameof(operationType)}' must be INSERT, UPDATE or DELETE.")
                    };

#if DEBUG
                    if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                    {
                        Console.WriteLine(cmd.CommandText);
                    }
#endif

                    rowsAffected += await cmd.ExecuteNonQueryAsync();
                }
            }

            return rowsAffected;
        }

        /// <summary>
        /// Execute a non-query SQL statement
        /// </summary>
        /// <param name="sql">SQL statement to execute</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteNonQueryAsync(string sql)
        {
            int rowsAffected = 0;

            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();

                using SqlCommand cmd = cn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

#if DEBUG
                if (Environment.GetEnvironmentVariable("SQLTABLECONTEXT_DUMPSQL") != null)
                {
                    Console.WriteLine(cmd.CommandText);
                }
#endif

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            return rowsAffected;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="connectionString">Connection string to use</param>
        public SqlTableContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Initialize - with local SQL Server, connected to TempDB
        /// </summary>
        public SqlTableContext()
            : this(LOCAL_SQL_SERVER)
        {
        }

        /// <summary>
        /// Return a context pointed to the local SQL Server and specified database
        /// </summary>
        /// <param name="databaseName">Name of database to connect to</param>
        /// <returns>Context pointed to <paramref name="databaseName"/> database</returns>
        public static SqlTableContext WithLocalDatabase(string databaseName)
            => new(LOCAL_SQL_SERVER.Replace("tempdb", databaseName));

        /// <summary>
        /// Return a context pointed to the server and database specified by the <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">Connection string to a SQL Server and database</param>
        /// <returns>Context pointed to <paramref name="connectionString"/> server and database</returns>
        public static SqlTableContext WithConnectionString(string connectionString)
            => new(connectionString);


        #endregion

        private readonly string _connectionString;
        private const string LOCAL_SQL_SERVER = "Data Source=(local);Initial Catalog=tempdb;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=True;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True";
    }
}
