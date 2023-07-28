using System;

namespace SujaySarma.Data.SqlServer
{
    /// <summary>
    /// Debug time only functionality
    /// </summary>
    public partial class SqlTableContext
    {
#if DEBUG
        
        /// <summary>
        /// Dump the sql to console if enabled
        /// </summary>
        /// <param name="sql">SQL to dump</param>
        private static void DumpGeneratedSqlToConsole(string sql)
        {
            if (Environment.GetEnvironmentVariable(DUMP_SQL_FLAG) != null)
            {
                Console.WriteLine(sql);
            }
        }

        /// <summary>
        /// Name of the SQL-debug environment variable. Value can be set to anything.
        /// </summary>
        public const string DUMP_SQL_FLAG = "SQLTABLECONTEXT_DUMPSQL";

#else

        /// <summary>
        /// In release mode, this is a NO-OP function
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        private static void DumpGeneratedSqlToConsole(string sql) { }
#pragma warning restore IDE0060 // Remove unused parameter

#endif
    }
}
