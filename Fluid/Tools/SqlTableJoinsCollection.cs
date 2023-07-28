﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using SujaySarma.Data.SqlServer.Fluid.Constants;
using SujaySarma.Data.SqlServer.LinqParsers;

namespace SujaySarma.Data.SqlServer.Fluid.Tools
{
    /// <summary>
    /// A collection of table JOINs. Enumeration will yield a fully parsed JOIN clause as a STRING that can be 
    /// plugged into a SQL query/statement.
    /// </summary>
    public class SqlTableJoinsCollection : IEnumerable<string>
    {

        /// <summary>
        /// Register a JOIN between two objects/tables.
        /// </summary>
        /// <param name="tableName">Actual name of the table</param>
        /// <param name="condition">Fully formed condition (ON clause) to join the tables. This is neither checked nor parsed</param>
        /// <param name="joinType">The type of join to perform.</param>
        /// <returns>Self-instance</returns>
        public void Add(string tableName, string condition, TypesOfJoinsEnum joinType = TypesOfJoinsEnum.Inner)
        {
            string joinTypeString = joinType switch
            {
                TypesOfJoinsEnum.Left => "LEFT JOIN",
                TypesOfJoinsEnum.Right => "RIGHT JOIN",
                TypesOfJoinsEnum.Full => "FULL JOIN",
                _ => "INNER JOIN"
            };

            string tableAlias = _aliasMapCollection.GetAliasIfDefined(tableName) ?? $"j{_joinStatements.Count}";
            _joinStatements.Add($"{joinTypeString} [{tableName}] {tableAlias} WITH (NOLOCK) ON {condition}");
        }

        /// <summary>
        /// Register a JOIN between two objects/tables.
        /// </summary>
        /// <typeparam name="TLeft">Type of CLR object for the LEFT table in the join</typeparam>
        /// <typeparam name="TRight">Type of CLR object for the RIGHT table in the join</typeparam>
        /// <param name="onCondition">Condition to join the tables</param>
        /// <param name="joinType">The type of join to perform. Default: INNER JOIN</param>
        /// <returns>Self-instance</returns>
        public void Add<TLeft, TRight>(Expression<Func<TLeft, TRight, bool>> onCondition, TypesOfJoinsEnum joinType = TypesOfJoinsEnum.Inner)
            where TLeft : class
            where TRight : class
        {
            if (typeof(TLeft) == typeof(TRight))
            {
                throw new ArgumentException("The types of the two tables/CLR objects must be the same.");
            }

            _aliasMapCollection.TryAdd<TLeft>();
            _aliasMapCollection.TryAdd<TRight>();

            TypeTableAliasMap rightTable = _aliasMapCollection.GetMap<TRight>()!;
            SqlLambdaVisitor parser = new(_aliasMapCollection);
            string onConditionSql = parser.ParseToSql(onCondition, false);
            string joinTypeString = joinType switch
            {
                TypesOfJoinsEnum.Left => "LEFT JOIN",
                TypesOfJoinsEnum.Right => "RIGHT JOIN",
                TypesOfJoinsEnum.Full => "FULL JOIN",
                _ => "INNER JOIN"
            };

            _joinStatements.Add($"{joinTypeString} [{rightTable.Discovery.TableName}] {rightTable.Alias} WITH (NOLOCK) ON {onConditionSql}");
        }

        /// <summary>
        /// Returns if collection contains any items
        /// </summary>
        public bool HasItems => (_joinStatements.Count > 0);

        #region IEnumerable

        /// <summary>
        /// Get the enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<string> GetEnumerator()
            => _joinStatements.GetEnumerator();

        /// <summary>
        /// Get the enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
            => _joinStatements.GetEnumerator();

        #endregion

        /// <summary>
        /// Create the collection. Only accessible to our internal query builders
        /// </summary>
        internal SqlTableJoinsCollection(TypeTableAliasMapCollection aliasMapCollection)
        {
            _joinStatements = new();
            _aliasMapCollection = aliasMapCollection;
        }

        private TypeTableAliasMapCollection _aliasMapCollection;
        private List<string> _joinStatements;
    }
}
