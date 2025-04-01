namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;
    using System.Linq;
    using ScheMigrator.Migrations;

    public class Where<T, R> : Query
    {
        private readonly Select<T, R> _select;
        private readonly Expression<Func<T, bool>> _where;

        internal Where(Select<T, R> select, Expression<Func<T, bool>> where)
            : base(select.Dialect)
        {
            _select = select;
            _where = where;
        }

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            _select.ToSql(qb);
            BuildWhereExpression(qb, _where.Body, Clause.And);
            return qb;
        }

        private static void BuildWhereExpression(QueryBuilder qb, Expression expression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    BuildWhereNot(qb, (UnaryExpression)expression, clause);
                    break;
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    BuildWhere(qb, (BinaryExpression)expression, clause);
                    break;
                case ExpressionType.Call:
                    BuildWhereIn(qb, expression, clause);
                    break;
                default:
                    throw new NotImplementedException($"Expression type {expression.NodeType} not supported");
            }
        }

        private static void BuildWhereIn(QueryBuilder qb, Expression expression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            var methodCall = (MethodCallExpression)expression;
            if (methodCall.Method.DeclaringType != typeof(QUtil) || methodCall.Method.Name != nameof(QUtil.IsIn))
            {
                throw new NotImplementedException($"Method call to {methodCall.Method.DeclaringType?.Name}.{methodCall.Method.Name} is not supported");
            }

            var selector = methodCall.Arguments[0] as MemberExpression;
            
            // Extract the value from the selector's Member
            object? selectorValue = null;
            if (selector != null)
            {
                selectorValue = Expression.Lambda(selector).Compile().DynamicInvoke();
            }
            
            // Extract the member from the inner expression
            System.Reflection.MemberInfo? innerMember = null;
            if (selectorValue is Expression innerExpression)
            {
                if (innerExpression is MemberExpression memberExpression)
                {
                    innerMember = memberExpression.Member;
                }
                else if (innerExpression is LambdaExpression lambdaExpression && lambdaExpression.Body is MemberExpression lambdaMemberExpression)
                {
                    innerMember = lambdaMemberExpression.Member;
                }
            }
            
            // Extract the parameter name from the second argument of the IsIn method call
            string? paramName = null;
            if (methodCall.Arguments.Count > 1)
            {
                switch (methodCall.Arguments[1])
                {
                    // The second argument should be a constant expression containing the parameter name
                    case ConstantExpression constantExpression:
                        paramName = constantExpression.Value as string;
                        break;
                    case MemberExpression memberExpression:
                    {
                        paramName = Expression.Lambda(memberExpression).Compile().DynamicInvoke() as string;
                        break;
                    }
                }
            }

            if (selector == null || innerMember == null || paramName == null)
            {
                throw new InvalidOperationException("Invalid IsIn call");
            }
            
            var attributeName = GetAttributeName(innerMember);
            
            qb.AppendInClause(attributeName, paramName);

        }

        private static void BuildWhereNot(QueryBuilder qb, UnaryExpression unaryExpression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            if (unaryExpression.NodeType != ExpressionType.Not)
                throw new NotImplementedException($"Unary expression type {unaryExpression.NodeType} not supported");

            // Handle the operand based on its type
            if (unaryExpression.Operand is BinaryExpression binaryOperand)
            {
                // Handle negating a binary expression
                switch (binaryOperand.NodeType)
                {
                    case ExpressionType.Equal:
                        clause(qb, binaryOperand, "<>").Append();
                        break;
                    case ExpressionType.NotEqual:
                        clause(qb, binaryOperand, "=").Append();
                        break;
                    case ExpressionType.GreaterThan:
                        clause(qb, binaryOperand, "<=").Append();
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        clause(qb, binaryOperand, "<").Append();
                        break;
                    case ExpressionType.LessThan:
                        clause(qb, binaryOperand, ">=").Append();
                        break;
                    case ExpressionType.LessThanOrEqual:
                        clause(qb, binaryOperand, ">").Append();
                        break;
                    default:
                        // For complex expressions, wrap them in NOT (...)
                        qb.AppendNot();
                        qb.AppendOpenParenthesis();
                        BuildWhereExpression(qb, binaryOperand, clause);
                        qb.AppendCloseParenthesis();
                        break;
                }
            }
            else if (unaryExpression.Operand is MemberExpression memberExpression)
            {
                // Handle negating a boolean property - add "= FALSE" condition
                var attributeName = GetAttributeName(memberExpression.Member);
                clause(qb, null, "=").AppendWithAttribute(attributeName, false);
            }
            else
            {
                throw new NotImplementedException($"Unary operand type {unaryExpression.Operand.GetType()} not supported");
            }
        }

        private static void BuildWhere(QueryBuilder qb, BinaryExpression binaryExpression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    clause(qb, binaryExpression, "=").Append();
                    break;
                case ExpressionType.NotEqual:
                    clause(qb, binaryExpression, "<>").Append();
                    break;
                case ExpressionType.GreaterThan:
                    clause(qb, binaryExpression, ">").Append();
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    clause(qb, binaryExpression, ">=").Append();
                    break;
                case ExpressionType.LessThan:
                    clause(qb, binaryExpression, "<").Append();
                    break;
                case ExpressionType.LessThanOrEqual:
                    clause(qb, binaryExpression, "<=").Append();
                    break;
                case ExpressionType.AndAlso:
                    BuildWhereExpression(qb, binaryExpression.Left, clause);
                    BuildWhereExpression(qb, binaryExpression.Right, Clause.And);
                    break;
                case ExpressionType.OrElse:
                    BuildWhereExpression(qb, binaryExpression.Left, clause);
                    BuildWhereExpression(qb, binaryExpression.Right, Clause.Or);
                    break;
                default:
                    throw new NotImplementedException($"Binary expression type {binaryExpression.NodeType} not supported");
            }
        }

        private static string GetAttributeName(System.Reflection.MemberInfo memberInfo)
        {
            var memberName = memberInfo.Name;
            var declaringType = memberInfo.DeclaringType;
            
            // Get the actual type being used in the query (T from Where<T,R>)
            var actualType = typeof(T);
            
            // If we have an expression, try to get the property from the actual type
            
            var property = actualType.GetProperty(memberName);
            if (property != null)
            {
                var actualAttributes = property
                    .GetCustomAttributes(true)
                    .Where(a => a is ScheDataAttribute)
                    .Select(a => a as ScheDataAttribute)
                    .FirstOrDefault();
                    
                if (actualAttributes != null && !string.IsNullOrEmpty(actualAttributes.FieldName))
                {
                    return actualAttributes.FieldName;
                }
            }
            
            throw new Exception($"ScheData attribute not found for member {memberName} on type {actualType.FullName}");
        }

        private class Clause
        {
            private readonly BinaryExpression _binaryExpression;
            private readonly string _op;
            private readonly Func<string, string, object, string, QueryBuilder> _appendValue;
            private readonly Func<string, string, string, object, string, QueryBuilder> _appendParameter;

            private Clause(BinaryExpression binaryExpression, string op,
                Func<string, string, object, string, QueryBuilder> appendValue,
                Func<string, string, string, object, string, QueryBuilder> appendParameter)
            {
                _binaryExpression = binaryExpression;
                _op = op;
                _appendValue = appendValue;
                _appendParameter = appendParameter;
            }

            public static Clause And(QueryBuilder qb, BinaryExpression binaryExpression, string op)
            {
                return new Clause(binaryExpression, op, qb.AddCondition, qb.AddCondition);
            }

            public static Clause Or(QueryBuilder qb, BinaryExpression binaryExpression, string op)
            {
                return new Clause(binaryExpression, op, qb.OrCondition, qb.OrCondition);
            }

            public void Append()
            {
                // Skip processing if binaryExpression is null (used in unary expressions)
                if (_binaryExpression == null)
                    return;
                
                var left = _binaryExpression.Left;
                var right = _binaryExpression.Right;
                if (left.NodeType == ExpressionType.Constant && right.NodeType == ExpressionType.MemberAccess)
                {
                    left = right;
                    right = left;
                }

                var memberExpression = (MemberExpression)left;
                var attributeName = GetAttributeName(memberExpression.Member);

                switch (right.NodeType)
                {
                    case ExpressionType.Constant:
                        var c = (ConstantExpression)right;
                        
                        // Use parameterized value for constants
                        var paramName = $"p_{attributeName}";
                        _appendParameter(_op, attributeName, paramName, c.Value, QueryBuilder.TableAliasName);
                        break;
                    case ExpressionType.MemberAccess:
                        var m2 = (MemberExpression)right;
                        var value = Expression.Lambda(m2).Compile().DynamicInvoke();
                        _appendParameter(_op, attributeName, m2.Member.Name, value, QueryBuilder.TableAliasName);
                        break;
                    default:
                        throw new NotImplementedException($"Right operand type {right.NodeType} not supported");
                }
            }
            
            // Add a method to handle direct attribute comparison with a value
            public void AppendWithAttribute(string attributeName, object value)
            {
                // Use parameterized value
                var paramName = $"p_{attributeName}_direct";
                _appendParameter(_op, attributeName, paramName, value, QueryBuilder.TableAliasName);
            }
        }

        /// <summary>
        /// Adds an OFFSET clause to the query for paging support
        /// </summary>
        /// <param name="offset">The number of rows to skip</param>
        /// <returns>An Offset query object</returns>
        public Offset<T, R> Offset(int offset)
        {
            return new Offset<T, R>(this, offset);
        }

        /// <summary>
        /// Creates a paged query with the specified page size and page number
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>A Page query object with limit and offset applied</returns>
        public Page<T, R> Page(int pageNumber, int pageSize)
        {
            return new Page<T, R>(this, pageNumber, pageSize);
        }

        /// <summary>
        /// Adds a LIMIT clause to the query
        /// </summary>
        /// <param name="limit">The maximum number of rows to return</param>
        /// <returns>A Limit query object</returns>
        public Limit<T, R> Limit(int limit)
        {
            return new Limit<T, R>(this, limit);
        }
    }
}