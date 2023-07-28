using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

using SujaySarma.Data.SqlServer.Attributes;


namespace SujaySarma.Data.SqlServer.Reflection
{
    /// <summary>
    /// Reflection utilities
    /// </summary>
    internal static class ReflectionUtils
    {

        /// <summary>
        /// Get the value of a property or field
        /// </summary>
        /// <typeparam name="TObject">Type of object</typeparam>
        /// <param name="instance">Instance of object</param>
        /// <param name="member">Member property or field</param>
        /// <param name="doNotAutoGen">If set, the AutoGenerateValue is ignored</param>
        /// <returns>Value of property or field or NULL</returns>
        public static object? GetValue<TObject>(TObject? instance, MemberInfo member, bool doNotAutoGen = false)
            where TObject : class
        {
            TableColumnAttribute? columnAttribute = member.GetCustomAttribute<TableColumnAttribute>(true);
            object? value = null;

            if (member is FieldInfo field)
            {
                value = field.GetValue((field.IsStatic ? null : instance));
                if ((!doNotAutoGen) && (columnAttribute != null) && columnAttribute.AutoGenerateValue)
                {
                    if (field.FieldType == typeof(Guid))
                    {
                        Guid? g1 = (Guid?)value;
                        if ((g1 == null) || (g1 == default(Guid)) || (g1 == Guid.Empty))
                        {
                            value = Guid.NewGuid();
                        }
                    }
                    else if (field.FieldType == typeof(DateTime))
                    {
                        DateTime? d1 = (DateTime?)value;
                        if ((d1 == null) || (d1 == default(DateTime)) || (d1 == DateTime.MinValue))
                        {
                            value = DateTime.UtcNow;
                        }
                    }
                }
            }
            else if (member is PropertyInfo property)
            {
                // properties are never 'static'
                value = property.GetValue(instance);
                if ((!doNotAutoGen) && (columnAttribute != null) && columnAttribute.AutoGenerateValue)
                {
                    if (property.PropertyType == typeof(Guid))
                    {
                        Guid? g2 = (Guid?)value;
                        if ((g2 == null) || (g2 == default(Guid)) || (g2 == Guid.Empty))
                        {
                            value = Guid.NewGuid();
                        }
                    }
                    else if (property.PropertyType == typeof(DateTime))
                    {
                        DateTime? d2 = (DateTime?)value;
                        if ((d2 == null) || (d2 == default(DateTime)) || (d2 == DateTime.MinValue))
                        {
                            value = DateTime.UtcNow;
                        }
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Set the value of a property or field
        /// </summary>
        /// <param name="instance">Instance of object</param>
        /// <param name="member">Member property or field</param>
        /// <param name="value">Value to set</param>
        public static void SetValue(object instance, MemberInfo member, object? value)
        {
            if (member is FieldInfo field)
            {
                value = CoerceFromEdmValue(value, field.FieldType);
                field.SetValue(instance, value);
            }
            else if (member is PropertyInfo property)
            {
                value = CoerceFromEdmValue(value, property.PropertyType);
                property.SetValue(instance, value);
            }
        }

        /// <summary>
        /// Cooerce a value from EDM to a CLR type 
        /// </summary>
        /// <param name="value">Value from Edm</param>
        /// <param name="targetClrType">The target CLR type to convert to</param>
        /// <returns>Cooerced value</returns>
        public static object? CoerceFromEdmValue(object? value, Type targetClrType)
        {
            if (value == default)
            {
                return default;
            }

            Type sourceEdmType = value.GetType();
            Type? actualClrType = Nullable.GetUnderlyingType(targetClrType);
            Type destinationClrType = actualClrType ?? targetClrType;

            if (!sourceEdmType.FullName!.Equals(destinationClrType.FullName, StringComparison.Ordinal))
            {
                // we need to convert
                return ConvertTo(destinationClrType, value);
            }

            return value;
        }

        /// <summary>
        /// Get the value correctly formatted and appropriately quoted (and escaped) for use in a SQL statement.
        /// </summary>
        /// <param name="clrValue">Value from the CLR object</param>
        /// <param name="enumSerializationBehaviour">If value is an enum, then how is it serialized</param>
        /// <param name="serializeToJson">If set, serializes complex types to Json</param>
        /// <param name="quotedStrings">When true, returns strings in quoted form</param>
        /// <returns>Correctly quoted and formatted value to be used in a SQL statement</returns>
        public static string GetSQLStringValue(object? clrValue, EnumSerializationBehaviourEnum enumSerializationBehaviour = EnumSerializationBehaviourEnum.AsInt, bool serializeToJson = false, bool quotedStrings = true)
        {
            if ((clrValue == null) || (clrValue == default))
            {
                return "NULL";
            }

            Type t = clrValue.GetType();
            if (t.IsEnum)
            {
                return ((enumSerializationBehaviour == EnumSerializationBehaviourEnum.AsInt) ? $"{(int)clrValue}" : $"'{clrValue}'");
            }

            if (clrValue is byte[] v5)
            {
                return $"0x{Convert.ToHexString(v5)}";
            }

            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Boolean:
                    return $"{((bool)clrValue ? 1 : 0)}";

                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.SByte:
                case TypeCode.Single:
                    return $"{clrValue}";

                case TypeCode.Char:
                    return (quotedStrings ? $"'{clrValue}'" : $"{clrValue}");

                case TypeCode.String:
                    string s = (string)clrValue;
                    s = s.Replace("'", "''");
                    return (quotedStrings ? $"'{s}'" : $"{s}");

                case TypeCode.DateTime:
                    return (quotedStrings ? $"'{((DateTime)clrValue).ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z'" : $"{((DateTime)clrValue).ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z");
            }

            if (clrValue is DateOnly v1)
            {
                return (quotedStrings ? $"'{v1:yyyy-MM-dd}T00:00:00Z'" : $"{v1:yyyy-MM-dd}T00:00:00Z");
            }

            if (clrValue is TimeOnly v2)
            {
                return (quotedStrings ? $"'01-01-{DateTime.UtcNow.Year}T{v2:HH:mm:ss}Z'" : $"01-01-{DateTime.UtcNow.Year}T{v2:HH:mm:ss}Z");
            }

            if (clrValue is DateTimeOffset v3)
            {
                return (quotedStrings ? $"'{v3.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z'" : $"{v3.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z");
            }

            if (clrValue is Guid v4)
            {
                return (quotedStrings ? $"'{v4:d}'" : $"{v4:d}");
            }

            if (serializeToJson)
            {
                return (quotedStrings ? $"'{JsonSerializer.Serialize(clrValue).Replace("'", "''")}'" : $"{JsonSerializer.Serialize(clrValue)}");
            }

            throw new ArgumentException($"Cannot serialize clrValue of type '{t.Name}'.");
        }

        /// <summary>
        /// Convert between types
        /// </summary>
        /// <param name="destinationType">CLR Type of destination</param>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value</returns>
        public static object? ConvertTo(Type destinationType, object value)
        {
            //NOTE: value is not null -- already been checked by caller before calling here
            if (destinationType.IsEnum)
            {
                if (value is string strVal)
                {
                    // Input is a string, destination is an Enum, Enum.Parse() it to convert!
                    // We are using Parse() and not TryParse() with good reason. Bad values will throw exceptions to the top-level caller 
                    // and we WANT that to happen! -- not only that, TryParse requires an extra typed storage that we do not want to provide here!

                    return Enum.Parse(destinationType, strVal);
                }

                if (value is int intVal)
                {
                    return intVal;
                }
            }

            // Adding support for new .NET types DateOnly and TimeOnly
            if (value is DateTimeOffset dto)
            {
                if (destinationType == typeof(DateTime))
                {
                    return dto.UtcDateTime;
                }

                if (destinationType == typeof(DateOnly))
                {
                    return new DateOnly(dto.Year, dto.Month, dto.Day);
                }

                if (destinationType == typeof(TimeOnly))
                {
                    return new TimeOnly(dto.Hour, dto.Minute, dto.Second);
                }

                return dto;
            }
            else if (value is DateTime dt)
            {
                if (destinationType == typeof(DateTimeOffset))
                {
                    return new DateTimeOffset(dt);
                }

                if (destinationType == typeof(DateOnly))
                {
                    return new DateOnly(dt.Year, dt.Month, dt.Day);
                }

                if (destinationType == typeof(TimeOnly))
                {
                    return new TimeOnly(dt.Hour, dt.Minute, dt.Second);
                }

                return dt;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
            if ((converter != null) && converter.CanConvertTo(destinationType))
            {
                return converter.ConvertTo(value, destinationType);
            }

            // see if type has a Parse static method
            MethodInfo[] methods = destinationType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if ((methods != null) && (methods.Length > 0))
            {
                Type sourceType = ((value == null) ? typeof(object) : value.GetType());
                foreach (MethodInfo m in methods)
                {
                    if (m.Name.Equals("Parse"))
                    {
                        ParameterInfo? p = m.GetParameters()?[0];
                        if ((p != null) && (p.ParameterType == sourceType))
                        {
                            return m.Invoke(null, new object?[] { value });
                        }
                    }
                    else if (m.Name.Equals("TryParse"))
                    {
                        ParameterInfo? p = m.GetParameters()?[0];
                        if ((p != null) && (p.ParameterType == sourceType))
                        {
                            object?[]? parameters = new object?[] { value, null };
                            bool? tpResult = (bool?)m.Invoke(null, parameters);
                            return ((tpResult.HasValue && tpResult.Value) ? parameters[1] : default);
                        }
                    }
                }
            }

            throw new TypeLoadException($"Could not find type converters for '{destinationType.Name}' type.");
        }

        /// <summary>
        /// Populate an object
        /// </summary>
        /// <typeparam name="TObject">CLR type of object</typeparam>
        /// <param name="row"><see cref="DataRow"/> containing data for this instance, MUST be connected to a valid <see cref="DataTable"/> that has a valid collection of columns.</param>
        /// <returns>Instance of <typeparamref name="TObject"/></returns>
        public static TObject Populate<TObject>(DataRow row)
            where TObject : class
        {
            if ((row.Table == default) || (row.Table.Columns.Count == 0))
            {
                throw new TypeLoadException($"The DataRow passed is not attached to a table, or the table has no schema. Object: '{typeof(TObject).Name}'");
            }

            TypeMetadata metadata = TypeMetadata.Discover<TObject>();
            TObject instance = Activator.CreateInstance<TObject>();

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

                    SetValue(instance, member, value);
                }
            }

            return instance;
        }


        /// <summary>
        /// Populate an object
        /// </summary>
        /// <param name="TObject">CLR type of object</param>
        /// <param name="row"><see cref="DataRow"/> containing data for this instance, MUST be connected to a valid <see cref="DataTable"/> that has a valid collection of columns.</param>
        /// <returns>Instance of <paramref name="TObject"/></returns>
        public static object Populate(Type TObject, DataRow row)
        {
            if ((row.Table == default) || (row.Table.Columns.Count == 0))
            {
                throw new TypeLoadException($"The DataRow passed is not attached to a table, or the table has no schema. Object: '{TObject.Name}'");
            }

            TypeMetadata metadata = TypeMetadata.Discover(TObject);
            object instance = Activator.CreateInstance(TObject) ?? throw new TypeLoadException($"Unable to instantiate object of type '{TObject.Name}'.");

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

                    SetValue(instance, member, value);
                }
            }

            return instance;
        }


        /// <summary>
        /// Get the underlying CLR data type of the property or field
        /// </summary>
        /// <param name="member">Property or field</param>
        /// <returns>Type information</returns>
        public static Type GetFieldOrPropertyType(this MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.FieldType;
            }

            if (member is PropertyInfo property)
            {
                return property.PropertyType;
            }

            throw new InvalidOperationException($"Cannot determine data type for member '{member.Name}' of type '{member.MemberType}'.");
        }

        /// <summary>
        /// Returns the SQL data type best matching the provided CLR type
        /// </summary>
        /// <param name="clrType">CLR type</param>
        /// <returns>SQL data type as a string</returns>
        public static string GetSqlTypeForClrType(Type clrType)
        {
            if (SqlClrTypeMapping.TryGetValue(clrType, out string? sqlType))
            {
                return sqlType;
            }

            return "nvarchar";  // when all else fails
        }

        /// <summary>
        /// Try and get the table and column name of the property/field
        /// </summary>
        /// <param name="memberInfo">Property or Field information</param>
        /// <param name="tableName">[Out] Name of the table this column belongs in</param>
        /// <param name="columnName">[Out] Name of the column corresponding to this <paramref name="memberInfo"/></param>
        /// <returns>True if we retrieved the information.</returns>
        public static bool TryGetTableAndColumnName(MemberInfo memberInfo, [NotNullWhen(true)] out string? tableName, [NotNullWhen(true)] out string? columnName)
        {
            bool result = false;
            tableName = null;
            columnName = null;

            TableAttribute? tableAttribute = memberInfo.DeclaringType!.GetCustomAttribute<TableAttribute>(true);
            if (tableAttribute != null)
            {
                tableName = tableAttribute.ToString();
                TableColumnAttribute? columnAttribute = memberInfo.GetCustomAttribute<TableColumnAttribute>(true);
                if (columnAttribute != null)
                {
                    columnName = columnAttribute.ColumnName;
                }

                result = true;
            }

            return result;
        }


        /// <summary>
        /// Type mapping dictionary used by <see cref="GetSqlTypeForClrType(Type)"/>
        /// </summary>
        private static readonly Dictionary<Type, string> SqlClrTypeMapping = new()
        {
            { typeof(bool), "bit" },
            { typeof(byte[]), "varbinary" },
            { typeof(byte), "tinyint" },
            { typeof(sbyte), "tinyint" },
            { typeof(char), "nchar" },
            { typeof(decimal), "float" },
            { typeof(double), "decimal" },
            { typeof(float), "float" },
            { typeof(Guid), "uniqueidentifier" },
            { typeof(int), "int" },
            { typeof(uint), "int" },
            { typeof(long), "bigint" },
            { typeof(ulong), "bigint" },
            { typeof(short), "smallint" },
            { typeof(ushort), "smallint" },
            { typeof(string), "nvarchar" },
            { typeof(DateTime), "datetime" },
            { typeof(DateTimeOffset), "datetimeoffset" },
            { typeof(DateOnly), "smalldatetime" },
            { typeof(TimeOnly), "smalldatetime" },
            { typeof(TimeSpan), "datetimeoffset"}
        };
    }

}
