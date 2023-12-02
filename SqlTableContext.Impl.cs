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
    /// Implementation functions for SqlTableContext
    /// </summary>
    public partial class SqlTableContext
    {

        /// <summary>
        /// Execute a query yielding an enumeration of object instances
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> ExecuteQuery<T>(string query)
            where T : class
            => ExecuteQuery<T>(new SqlCommand(query));

        /// <summary>
        /// Execute a query yielding an enumeration of object instances
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of object instances</returns>
        public IEnumerable<T> ExecuteQuery<T>(SqlCommand query)
            where T : class
        {
            IEnumerable<DataRow> rows = ExecuteQueryRows(query);
            foreach (DataRow row in rows)
            {
                yield return ReflectionUtils.Populate<T>(row);
            }
        }


        /// <summary>
        /// Execute a query yielding an enumeration of data rows
        /// </summary>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of DataRows</returns>
        public IEnumerable<DataRow> ExecuteQueryRows(string query)
            => ExecuteQueryRows(new SqlCommand(query));

        /// <summary>
        /// Execute a query yielding an enumeration of data rows
        /// </summary>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>Enumeration of DataRows</returns>
        public IEnumerable<DataRow> ExecuteQueryRows(SqlCommand query)
        {
            DataTable table = ExecuteQueryTable(query);
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
            => ExecuteQueryTable(new SqlCommand(query));

        /// <summary>
        /// Execute a query yielding a complete DataTable
        /// </summary>
        /// <param name="query">Query to run on the SQL Server</param>
        /// <returns>DataTable</returns>
        public DataTable ExecuteQueryTable(SqlCommand query)
        {
            DataTable table = new();
            using (SqlConnection cn = new(_connectionString))
            {
                cn.Open();
                query.Connection = cn;

                DumpGeneratedSqlToConsole(query.CommandText);

                using SqlDataAdapter da = new(query);
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

                DumpGeneratedSqlToConsole(cmd.CommandText);

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

                DumpGeneratedSqlToConsole(cmd.CommandText);

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

                DumpGeneratedSqlToConsole(cmd.CommandText);

#pragma warning disable IDE0034 // Use 'default' instead of 'default(T)'
                object? v = await cmd.ExecuteScalarAsync();
                if ((v == null) || (v is DBNull) || (v == DBNull.Value))
                {
                    return default(T);
                }

                return (T?)ReflectionUtils.CoerceFromEdmValue(v, typeof(T));
            }
            catch
            {
            }

            return default(T);
#pragma warning restore IDE0034 // Use 'default' instead of 'default(T)'
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

                DumpGeneratedSqlToConsole(cmd.CommandText);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            return rowsAffected;
        }

        /// <summary>
        /// Execute a non-query operation
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="operationType">Type of operation</param>
        /// <param name="data">Instances of objects to insert/update/delete</param>
        /// <param name="AdditionalData">Additional data to be updated (for INSERT and UPDATE only!)</param>
        /// <param name="AdditionalConditions">Additional conditions to check (for UPDATE and DELETE only!) -- will be merged with 'AND'</param>
        /// <returns>Total number of rows affected in the backing data store</returns>
        /// <remarks>
        ///     This method is **PRIVATE** because there is no advantage to calling this over 
        ///     the wrapped 'InsertAsync[T]()', 'UpdateAsync[T]()', 'DeleteAsync[T]()' functions
        /// </remarks>
        private async Task<int> ExecuteNonQueryAsync<T>(SqlStatementType operationType, IEnumerable<T> data, Dictionary<string, object?>? AdditionalData = null, List<string>? AdditionalConditions = null)
            where T : class
        {
            int rowsAffected = 0;

            if (data.Any()) // bug fix: callers may pass empty structures to insert/update/delete
            {
                using (SqlConnection cn = new(_connectionString))
                {
                    cn.Open();

                    using SqlCommand cmd = cn.CreateCommand();
                    cmd.CommandType = CommandType.Text;

                    foreach (T item in data)
                    {
                        cmd.CommandText = operationType switch
                        {
                            SqlStatementType.Insert => SQLScriptGenerator.GetInsertStatement<T>(item, AdditionalData),
                            SqlStatementType.Update => SQLScriptGenerator.GetUpdateStatement<T>(item, AdditionalData, AdditionalConditions),
                            SqlStatementType.Delete => SQLScriptGenerator.GetDeleteStatement<T>(item, AdditionalConditions),

                            // to complicated to support Addl data & conditions with our current code!
                            SqlStatementType.Upsert => SQLScriptGenerator.GetMergeStatement<T>(item),

                            _ => throw new NotSupportedException($"'{nameof(operationType)}' must be INSERT, UPDATE or DELETE.")
                        };

                        DumpGeneratedSqlToConsole(cmd.CommandText);

                        rowsAffected += await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            return rowsAffected;
        }

    }
}
