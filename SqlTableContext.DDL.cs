using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using SujaySarma.Data.SqlServer.Attributes;
using SujaySarma.Data.SqlServer.Reflection;

namespace SujaySarma.Data.SqlServer
{
    /// <summary>
    /// DDL operations through SqlTableContext
    /// </summary>
    public partial class SqlTableContext
    {

        /// <summary>
        /// Attempt to create a table for the provided CLR object
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        public async Task CreateTableAsync<T>()
            where T : class
        {
            TypeMetadata metadata = TypeMetadata.Discover<T>();
            List<string> statements = new()
            {
                "CREATE TABLE",
                $"[{metadata.SchemaName}].[{metadata.TableName}]",
                "("
            }, 
            primaryKeys = new(),
            columns = new();

            foreach(MemberInfo member in metadata.Members)
            {
                TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>();
                if (columnAttribute == null)
                {
                    continue;
                }

                List<string> columnDefinition = new()
                {
                    $"[{columnAttribute.ColumnName}]"
                };

                Type clrType = ReflectionUtils.GetFieldOrPropertyType(member);
                string sqlType = ReflectionUtils.GetSqlTypeForClrType(clrType);

                if (sqlType == "varbinary")
                {
                    sqlType = "varbinary(max)";
                }

                // do we have any validation attributes?
                List<ValidationAttribute> validationAttributes = member.GetCustomAttributes<ValidationAttribute>(true).ToList();
                if (validationAttributes.Count > 0)
                {
                    if ((sqlType == "nchar") || (sqlType == "nvarchar"))
                    {
                        StringLengthAttribute? length = (StringLengthAttribute?)validationAttributes.FirstOrDefault(a => (a is StringLengthAttribute));
                        sqlType = ((length != null) ? $"{sqlType}({length.MaximumLength})" : $"{sqlType}(max)");
                        columnDefinition.Add(sqlType);
                    }
                    else
                    {
                        columnDefinition.Add(sqlType);
                    }

                    columnDefinition.Add(
                            (validationAttributes.Any(a => (a is RequiredAttribute)))
                            ? "NOT NULL"
                            : "NULL"
                        );
                }
                else
                {
                    Type? srcActualType = Nullable.GetUnderlyingType(clrType);
                    columnDefinition.Add((srcActualType == null) ? $"{sqlType} NOT NULL" : $"{sqlType} NULL");
                }

                // push to table columns
                columns.Add(string.Join(' ', columnDefinition));

                if (columnAttribute.KeyBehaviour == KeyBehaviourEnum.PrimaryKey)
                {
                    primaryKeys.Add($"[{columnAttribute.ColumnName}]");
                }
            }

            string sql = string.Join(
                    ' ',
                    string.Join(' ', statements),
                    string.Join(',', columns),
                    ',',
                    $"CONSTRAINT PK_{metadata.TableName.Replace(' ', '_')} PRIMARY KEY ({string.Join(',', primaryKeys)})",
                    ");"
                );


            await ExecuteNonQueryAsync(sql);
        }

        /// <summary>
        /// Attempt to drop the table for the provided CLR object
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        public async Task DropTableAsync<T>()
            where T : class
        {
            Type t = typeof(T);
            TableAttribute? tableAttribute = t.GetCustomAttribute<TableAttribute>() 
                ?? throw new InvalidOperationException($"Cannot create table for '{t.Name}' as it is not decorated with 'Table' attribute");

            string sql = $"DROP TABLE [{tableAttribute.SchemaName}].[{tableAttribute.TableName}];";
            await ExecuteNonQueryAsync(sql);
        }




    }
}
