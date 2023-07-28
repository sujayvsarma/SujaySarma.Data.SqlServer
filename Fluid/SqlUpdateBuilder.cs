using System;
using System.Collections.Generic;
using System.Reflection;

using SujaySarma.Data.SqlServer.Attributes;
using SujaySarma.Data.SqlServer.Fluid.Tools;
using SujaySarma.Data.SqlServer.Reflection;

namespace SujaySarma.Data.SqlServer.Fluid
{
    /// <summary>
    /// Fluid builder for UPDATE statement and operations. Allows updating columns not a part of <typeparamref name="TTable"/>. 
    /// Handles simple UPDATEs, but we do not deal with complex statements like UPDATEs with JOINs and so on.
    /// </summary>
    /// <typeparam name="TTable">Type of business object mapped to the table being updated</typeparam>
    public class SqlUpdateBuilder<TTable> : SqlFluidStatementBuilder
        where TTable : class
    {

        /// <inheritdoc />
        public override string Build()
        {
            List<string> query = new(), additionalInfo = new(), whereClause = new();
            TypeTableAliasMap map = base.TypeTableMap.GetPrimaryTable();

            if ((_additionalColumnsWithValues != null) && (_additionalColumnsWithValues.Count > 0))
            {
                foreach (string addlColName in _additionalColumnsWithValues.Keys)
                {
                    additionalInfo.Add($"[{addlColName}] = {ReflectionUtils.GetSQLStringValue(_additionalColumnsWithValues[addlColName])}");
                }
            }

            if (Where.HasConditions)
            {
                whereClause.Add(Where.ToString());
            }

            foreach (TTable item in _updateList)
            {
                query.Add($"UPDATE [{map.GetQualifiedTableName()}] SET");
                foreach (MemberInfo member in map.Discovery.Members)
                {
                    TableColumnAttribute columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true)!;
                    if ((columnAttribute.InsertUpdateColumnBehaviour == InsertUpdateColumnBehaviourEnum.NeitherInsertNorUpdate) && (columnAttribute.KeyBehaviour != KeyBehaviourEnum.PrimaryKey))
                    {
                        continue;
                    }

                    string value = ReflectionUtils.GetSQLStringValue(ReflectionUtils.GetValue<TTable>(item, member), columnAttribute.EnumSerializationBehaviour, columnAttribute.JsonSerialize);
                    if (columnAttribute.KeyBehaviour == KeyBehaviourEnum.PrimaryKey)
                    {
                        whereClause.Add($"[{columnAttribute.ColumnName}] = {value}");
                    }
                    else
                    {
                        query.Add($"[{columnAttribute.ColumnName}] = {value}");
                    }
                }

                if (additionalInfo.Count > 0)
                {
                    query.Add(string.Join(',', additionalInfo));
                }

                if (whereClause.Count > 0)
                {
                    query.Add(
                            string.Join(
                                ' ',
                                "WHERE",
                                string.Join(" AND ", whereClause)
                            )
                        );
                }
            }

            return string.Join(";" + Environment.NewLine, query);
        }

        /// <summary>
        /// Define additional columns to update (columns in table not defined in the <typeparamref name="TTable"/> object). These columns are added to the END of the list.
        /// </summary>
        /// <param name="additionalColumnsWithValues">Additional columns with values. Items with columns already defined are replaced. The same values are updated for every row!</param>
        /// <returns>Self-instance</returns>
        public SqlUpdateBuilder<TTable> WithAdditionalColumns(Dictionary<string, object?> additionalColumnsWithValues)
        {
            _additionalColumnsWithValues ??= new();
            foreach (string colName in additionalColumnsWithValues.Keys)
            {
                _additionalColumnsWithValues[colName] = additionalColumnsWithValues[colName];
            }

            return this;
        }

        /// <summary>
        /// Add the object instances that contain the data to be updated
        /// </summary>
        /// <param name="items">Object instances with data</param>
        /// <returns>Self-instance</returns>
        public SqlUpdateBuilder<TTable> AddItems(params TTable[] items)
        {
            _updateList.AddRange(items);
            return this;
        }

        /// <summary>
        /// Defines the primary CLR object (and hence it's backing SQL Server table) that should be used 
        /// to populate the query. Any unqualified column references are implicitly assumed to be homed 
        /// in this object/table.
        /// </summary>
        /// <returns>Created instance of SqlQueryBuilder</returns>
        public static SqlUpdateBuilder<TTable> Begin()
            => new();

        /// <summary>
        /// Collection of WHERE conditions
        /// </summary>
        public SqlTableWhereConditionsCollection Where
        {
            get;
            init;
        }

        /// <inheritdoc />
        private SqlUpdateBuilder() : base()
        {
            base.TypeTableMap.TryAdd<TTable>(isPrimaryTable: true);

            _updateList = new();
            _additionalColumnsWithValues = null;

            Where = new(base.TypeTableMap);
        }

        private readonly List<TTable> _updateList;
        private Dictionary<string, object?>? _additionalColumnsWithValues;
    }
}
