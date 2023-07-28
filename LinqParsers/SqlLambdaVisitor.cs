using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using SujaySarma.Data.SqlServer.Attributes;
using SujaySarma.Data.SqlServer.Fluid.Tools;
using SujaySarma.Data.SqlServer.Reflection;

namespace SujaySarma.Data.SqlServer.LinqParsers
{
    /// <summary>
    /// A visitor that examines parts of an expression and returns the relevant results
    /// </summary>
    internal class SqlLambdaVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="typeTableMap">Mapping of types with tables</param>
        public SqlLambdaVisitor(TypeTableAliasMapCollection typeTableMap) : base()
        {
            _typeTableAliasMap = typeTableMap;
            _values = new();
        }

        /// <summary>
        /// Parse the Linq expression to a SQL expression
        /// </summary>
        /// <param name="expression">Linq expression to parse</param>
        /// /// <param name="treatAssignmentsAsAlias">[Optional] When set, tells the parser to treat any assignments in the expression as aliases. For eg: 'a = s.Id' will turn into 's.Id as [a]'</param>
        /// <returns>SQL expression</returns>
        public string ParseToSql(Expression expression, bool treatAssignmentsAsAlias = false)
        {
            _treatAssignmentsAsAlias = treatAssignmentsAsAlias;

            Visit(expression);

            StringBuilder sql = new();
            while (_values.Count > 0)
            {
                sql.Append(_values.Pop());
                sql.Append(' ');
            }

            return sql.ToString().Trim();
        }

        /// <summary>
        /// Resolve a conditional expression ((a > b) ? c : d) to a SQL expression (CASE WHEN ELSE)
        /// </summary>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Visit(node.Test);
            string caseWhenConditionSql = _values.Pop();

            Visit(node.IfTrue);
            string caseWhenTrueSql = _values.Pop();

            Visit(node.IfFalse);
            string caseElseSql = _values.Pop();

            _values.Push($"CASE WHEN ({caseWhenConditionSql}) THEN {caseWhenTrueSql} ELSE {caseElseSql} END");

            return node;
        }

        /// <summary>
        /// Resolve a binary expression (eg: A + B, X == Y, etc) into its SQL expression
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            string @operator = GetSqlOperatorForExpressionType(node);

            Visit(node.Left);
            string leftOperandSql = _values.Pop();

            Visit(node.Right);
            string rightOperandSql = _values.Pop();

            _values.Push($"({leftOperandSql} {@operator} {rightOperandSql})");

            return node;
        }

        /// <summary>
        /// Resolves a unary expression (NOT x, -ABC, etc) into its SQL expression.
        /// </summary>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            // resolve the operand first
            Visit(node.Operand);
            string nodeSql = _values.Pop();

            switch (node.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    nodeSql = $"(-{nodeSql})";
                    break;

                case ExpressionType.Not:
                    nodeSql = $"NOT {nodeSql}";
                    break;

                default:
                    break;
            }
            _values.Push(nodeSql);

            return node;
        }

        /// <summary>
        /// Resolves a member access (A.B) to its SQL table.column expression. If member is static or 
        /// non-table mapped entity, its value is taken instead.
        /// </summary>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == null)
            {
                return node;
            }

            if ((node.Member is FieldInfo fi) && fi.IsStatic)
            {
                _values.Push(
                    ReflectionUtils.GetSQLStringValue(
                        fi.GetValue(null), 
                        ((node.Member.DeclaringType.IsEnum && (_currentEnum != null)) ? _currentEnumSerializationBehaviour : EnumSerializationBehaviourEnum.AsInt)
                    ));

                return node;
            }

            // normal property access
            string? tid = _typeTableAliasMap.GetAliasOrName(node.Member.DeclaringType);
            Type propertyOrFieldType = ReflectionUtils.GetFieldOrPropertyType(node.Member);
            bool isEnumProperty = propertyOrFieldType.IsEnum;

            if (string.IsNullOrWhiteSpace(tid))
            {
                // not a mapped type, so we need the value of what is being referenced
                object? resolvedValueOfExpression = ResolveExpressionAsValue(node);
                _values.Push(ReflectionUtils.GetSQLStringValue(
                    resolvedValueOfExpression, 
                    ((isEnumProperty && (_currentEnum != null)) ? _currentEnumSerializationBehaviour : EnumSerializationBehaviourEnum.AsInt)
                ));
                return node;
            }

            // property of an object mapped to a table we can use
            TableColumnAttribute? columnAttribute = node.Member.GetCustomAttribute<TableColumnAttribute>(true);
            if ((columnAttribute != null) && (!string.IsNullOrWhiteSpace(columnAttribute.ColumnName)))
            {
                // we have the table.column accessor!
                _values.Push($"{tid}.[{columnAttribute.ColumnName}]");
                if (isEnumProperty)
                {
                    _currentEnum = propertyOrFieldType;
                    _currentEnumSerializationBehaviour = columnAttribute.EnumSerializationBehaviour;
                }
            }

            return node;
        }

        /// <summary>
        /// Get the value of a constant
        /// </summary>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            _values.Push(SerializeToString(node.Value));
            return node;
        }

        /// <summary>
        /// Get value of a new object init within an expression, usually of anonymous types. 
        /// Eg: x => new { x.Id, x.Name } --> "t.[Id], t.[Name]..."
        /// </summary>
        protected override Expression VisitNew(NewExpression node)
        {
            IEnumerable<KeyValuePair<MemberInfo, Expression>>? args = node.Members?.Zip(node.Arguments, (m, a) => new KeyValuePair<MemberInfo, Expression>(m, a));
            if (args != null)
            {
                List<string> list = new();
                foreach (KeyValuePair<MemberInfo, Expression> item in args)
                {
                    Visit(item.Value);

                    string s = _values.Pop();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        if (_treatAssignmentsAsAlias)
                        {
                            list.Add($"{s} AS [{item.Key.Name}]");
                        }
                        else
                        {
                            list.Add(s);
                        }
                    }
                }

                if (list.Count > 0)
                {
                    _values.Push(string.Join(',', list));
                }
            }
            return node;
        }

        /// <summary>
        /// Get the SQL operator for the type of node
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>SQL operator string</returns>
        private static string GetSqlOperatorForExpressionType(Expression node)
            => node.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",

                ExpressionType.OrElse => "OR",
                ExpressionType.AndAlso => "AND",
                ExpressionType.Not => "NOT",

                _ => throw new NotSupportedException($"Operator {node.NodeType} is not supported yet.")
            };

        /// <summary>
        /// When we need to find the VALUE pointed to by an A.B.C member access expression, 
        /// this function recursively walks through to the final element and then walks back 
        /// to resolve the value of A.B.C.
        /// </summary>
        /// <param name="expression">Expression to traverse</param>
        /// <returns>The raw value of what we found</returns>
        private static object? ResolveExpressionAsValue(MemberExpression expression)
        {
            object? result = null;
            Stack<MemberInfo> _recursiveResolverStack = new();

            MemberExpression memberExpression = expression;
            do
            {
                _recursiveResolverStack.Push(memberExpression.Member);
                if ((memberExpression.Expression == null) || (memberExpression.Expression is not MemberExpression mex))
                {
                    break;
                }
                memberExpression = mex;

            } while (true);

            if (memberExpression.Expression != null)
            {
                if (memberExpression.Expression is ConstantExpression cex)
                {
                    MemberInfo parentPropertyOrField = _recursiveResolverStack.Pop();
                    object? referenceObject = cex.Value;
                    result = GetValueFromPropertyOrField(parentPropertyOrField, referenceObject);
                }
                // else: what else can it be!
            }

            while (_recursiveResolverStack.Count > 0)
            {
                MemberInfo parentPropertyOrField = _recursiveResolverStack.Pop();
                result = GetValueFromPropertyOrField(parentPropertyOrField, result);
            }
            return result;
        }


        private static object? GetValueFromPropertyOrField(MemberInfo propertyOrFieldInfo, object? parentInstance)
            => propertyOrFieldInfo.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)propertyOrFieldInfo).GetValue(parentInstance),
                MemberTypes.Field => ((FieldInfo)propertyOrFieldInfo).GetValue(parentInstance),

                _ => throw new InvalidOperationException($"Unsupported operation: Cannot get value from member of type '{propertyOrFieldInfo.MemberType}'")
            };


        private string SerializeToString(object? value)
        {
            string result;
            if (_currentEnum == null)
            {
                result = ReflectionUtils.GetSQLStringValue(value);
            }
            else
            {
                result = ReflectionUtils.GetSQLStringValue(
                        ((value == null) ? null : Enum.Parse(_currentEnum, value.ToString()!)),
                        _currentEnumSerializationBehaviour
                    );

                _currentEnum = null;
            }

            return result;
        }


        private EnumSerializationBehaviourEnum _currentEnumSerializationBehaviour = EnumSerializationBehaviourEnum.AsInt;
        private Type? _currentEnum = null;

        private readonly Stack<string> _values;
        private readonly TypeTableAliasMapCollection _typeTableAliasMap;
        private bool _treatAssignmentsAsAlias = false;


    }
}
