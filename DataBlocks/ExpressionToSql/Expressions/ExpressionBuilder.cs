using System;
using System.Linq;
using System.Linq.Expressions;
using ExpressionToSql;
using ScheMigrator.Migrations;
using ExpressionToSql.Utils;

namespace DataBlocks.ExpressionToSql.Expressions
{
    public enum ClauseType
    {
        Where,
        On,
        Having
    }

    public class ExpressionBuilder
    {
        private readonly ISqlDialect _dialect;
        private readonly QueryBuilder _queryBuilder;
        private readonly Query _query;
        private bool _firstCondition = true;
        private ClauseType _currentClauseType = ClauseType.Where;

        public ExpressionBuilder(Query query, QueryBuilder queryBuilder)
        {
            _dialect = query.Dialect;
            _queryBuilder = queryBuilder;
            _query = query;
        }

        public ExpressionBuilder WithClauseType(ClauseType clauseType)
        {
            _currentClauseType = clauseType;
            return this;
        }

        public void BuildExpression(Expression expression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    BuildNotExpression((UnaryExpression)expression, clause);
                    break;
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    BuildBinaryExpression((BinaryExpression)expression, clause);
                    break;
                case ExpressionType.Call:
                    BuildMethodCallExpression(expression, clause);
                    break;
                default:
                    throw new NotImplementedException($"Expression type {expression.NodeType} not supported");
            }
        }

        public virtual void BuildMethodCallExpression(Expression expression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            // Handle method calls like IsIn
            var methodCall = (MethodCallExpression)expression;
            if (methodCall.Method.DeclaringType == typeof(QUtil) && methodCall.Method.Name == nameof(QUtil.IsIn))
            {
                // Extract selector and parameter name
                if (methodCall.Arguments[0] is MemberExpression selector && 
                    methodCall.Arguments.Count > 1 && 
                    methodCall.Arguments[1] is ConstantExpression constExpr)
                {
                    string paramName = constExpr.Value as string;
                    if (paramName != null)
                    {
                        // Get alias for the parameter's type
                        string tableAlias = GetTableAliasForMember(selector);
                        string attributeName = GetAttributeName(selector);
                        _queryBuilder.AppendInClause($"{tableAlias}.{attributeName}", paramName);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"Method call to {methodCall.Method.DeclaringType?.Name}.{methodCall.Method.Name} is not supported");
            }
        }

        protected void BuildNotExpression(UnaryExpression unaryExpression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            if (unaryExpression.NodeType != ExpressionType.Not)
                throw new NotImplementedException($"Unary expression type {unaryExpression.NodeType} not supported");

            if (unaryExpression.Operand is BinaryExpression binaryOperand)
            {
                switch (binaryOperand.NodeType)
                {
                    case ExpressionType.Equal:
                        clause(_queryBuilder, binaryOperand, "<>").Append();
                        break;
                    case ExpressionType.NotEqual:
                        clause(_queryBuilder, binaryOperand, "=").Append();
                        break;
                    case ExpressionType.GreaterThan:
                        clause(_queryBuilder, binaryOperand, "<=").Append();
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        clause(_queryBuilder, binaryOperand, "<").Append();
                        break;
                    case ExpressionType.LessThan:
                        clause(_queryBuilder, binaryOperand, ">=").Append();
                        break;
                    case ExpressionType.LessThanOrEqual:
                        clause(_queryBuilder, binaryOperand, ">").Append();
                        break;
                    default:
                        _queryBuilder.AppendNot();
                        _queryBuilder.AppendOpenParenthesis();
                        BuildExpression(binaryOperand, clause);
                        _queryBuilder.AppendCloseParenthesis();
                        break;
                }
            }
            else if (unaryExpression.Operand is MemberExpression memberExpression)
            {
                var attributeName = GetAttributeName(memberExpression);
                clause(_queryBuilder, null, "=").AppendWithAttribute(attributeName, false);
            }
            else
            {
                throw new NotImplementedException($"Unary operand type {unaryExpression.Operand.GetType()} not supported");
            }
        }

        protected void BuildBinaryExpression(BinaryExpression binaryExpression, Func<QueryBuilder, BinaryExpression, string, Clause> clause)
        {
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    clause(_queryBuilder, binaryExpression, "=").Append();
                    break;
                case ExpressionType.NotEqual:
                    clause(_queryBuilder, binaryExpression, "<>").Append();
                    break;
                case ExpressionType.GreaterThan:
                    clause(_queryBuilder, binaryExpression, ">").Append();
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    clause(_queryBuilder, binaryExpression, ">=").Append();
                    break;
                case ExpressionType.LessThan:
                    clause(_queryBuilder, binaryExpression, "<").Append();
                    break;
                case ExpressionType.LessThanOrEqual:
                    clause(_queryBuilder, binaryExpression, "<=").Append();
                    break;
                case ExpressionType.AndAlso:
                    BuildExpression(binaryExpression.Left, clause);
                    BuildExpression(binaryExpression.Right, Clause.And);
                    break;
                case ExpressionType.OrElse:
                    BuildExpression(binaryExpression.Left, clause);
                    BuildExpression(binaryExpression.Right, Clause.Or);
                    break;
                default:
                    throw new NotImplementedException($"Binary expression type {binaryExpression.NodeType} not supported");
            }
        }

        // Get attribute name handling type information
        protected string GetAttributeName(MemberExpression expression)
        {
            string tableAlias = GetTableAliasForMember(expression);
            Type entityType = _query.GetEntityType(tableAlias);
            return SqlTypeUtils.ResolveFieldName(expression.Member, entityType);
        }

        // Helper method to get the table alias for a member expression
        private string GetTableAliasForMember(MemberExpression expression)
        {
            if (expression.Expression is ParameterExpression paramExpr)
            {
                return _queryBuilder.GetAliasForType(paramExpr.Type);
            }
            return QueryBuilder.TableAliasName; // Default alias
        }

        public class Clause
        {
            private readonly BinaryExpression _binaryExpression;
            private readonly string _op;
            private readonly Func<string, string, object, string, QueryBuilder> _appendValue;
            private readonly Func<string, string, string, object, string, QueryBuilder> _appendParameter;
            private readonly QueryBuilder _queryBuilder;
            private readonly ExpressionBuilder _expressionBuilder;
            private readonly string _condition;

            private Clause(BinaryExpression binaryExpression, string op,
                Func<string, string, object, string, QueryBuilder> appendValue,
                Func<string, string, string, object, string, QueryBuilder> appendParameter,
                QueryBuilder queryBuilder, ExpressionBuilder expressionBuilder, string condition)
            {
                _binaryExpression = binaryExpression;
                _op = op;
                _appendValue = appendValue;
                _appendParameter = appendParameter;
                _queryBuilder = queryBuilder;
                _expressionBuilder = expressionBuilder;
                _condition = condition;
            }

            public static Clause And(QueryBuilder qb, BinaryExpression binaryExpression, string op)
            {
                var expressionBuilder = new ExpressionBuilder(qb._query, qb);
                return new Clause(binaryExpression, op, qb.AddCondition, qb.AddCondition, qb, expressionBuilder, "AND");
            }

            public static Clause Or(QueryBuilder qb, BinaryExpression binaryExpression, string op)
            {
                var expressionBuilder = new ExpressionBuilder(qb._query, qb);
                return new Clause(binaryExpression, op, qb.OrCondition, qb.OrCondition, qb, expressionBuilder, "OR");
            }

            public void Append()
            {
                if (_binaryExpression == null)
                    return;
                
                var left = _binaryExpression.Left;
                var right = _binaryExpression.Right;
                
                // Handle reversed expressions (constant on left)
                if (left.NodeType == ExpressionType.Constant && right.NodeType == ExpressionType.MemberAccess)
                {
                    (left, right) = (right, left);
                }

                if (left is MemberExpression leftMember)
                {
                    string tableAlias = _expressionBuilder.GetTableAliasForMember(leftMember);
                    string attributeName = _expressionBuilder.GetAttributeName(leftMember);

                    switch (right.NodeType)
                    {
                        case ExpressionType.Constant:
                            var c = (ConstantExpression)right;
                            var paramName = $"p_{attributeName}";
                            
                            // For attribute condition, we need to manually check if it's the first condition
                            if (_queryBuilder.IsFirstCondition())
                            {
                                // The first condition needs special handling based on current clause type
                                if (_expressionBuilder._currentClauseType == ClauseType.Where)
                                {
                                    _queryBuilder.AppendCondition("WHERE");
                                }
                                else if (_expressionBuilder._currentClauseType == ClauseType.On)
                                {
                                    _queryBuilder.Append(" ON");

                                }
                                else if (_expressionBuilder._currentClauseType == ClauseType.Having)
                                {
                                    _queryBuilder.AppendCondition("HAVING");
                                }
                                
                                _queryBuilder.AddAttribute(attributeName, "", tableAlias);
                                _queryBuilder.Append(" ").Append(_op);
                                _queryBuilder.AddParameterWithValue(paramName, c.Value);
                            }
                            else
                            {
                                // Use the normal condition handling for subsequent conditions
                                _appendParameter(_op, attributeName, paramName, c.Value, tableAlias);
                            }
                            break;
                        case ExpressionType.MemberAccess:
                            var rightMember = (MemberExpression)right;
                            var value = Expression.Lambda(rightMember).Compile().DynamicInvoke();
                            
                            // Same logic for member expression
                            if (_queryBuilder.IsFirstCondition())
                            {
                                // Handle first condition based on current clause type
                                if (_expressionBuilder._currentClauseType == ClauseType.Where)
                                {
                                    _queryBuilder.AppendCondition("WHERE");
                                }
                                else if (_expressionBuilder._currentClauseType == ClauseType.On)
                                {
                                    _queryBuilder.Append(" ON");
                                }
                                else if (_expressionBuilder._currentClauseType == ClauseType.Having)
                                {
                                    _queryBuilder.AppendCondition("HAVING");
                                }
                                
                                _queryBuilder.AddAttribute(attributeName, "", tableAlias);
                                _queryBuilder.Append(" ").Append(_op);
                                _queryBuilder.AddParameterWithValue(rightMember.Member.Name, value);
                            }
                            else
                            {
                                _appendParameter(_op, attributeName, rightMember.Member.Name, value, tableAlias);
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Right operand type {right.NodeType} not supported");
                    }
                }
                else
                {
                    throw new NotImplementedException($"Left operand type {left.NodeType} not supported");
                }
            }
            
            public void AppendWithAttribute(string attributeName, object value)
            {
                // Check if this is the first condition
                if (_queryBuilder.IsFirstCondition())
                {
                    // Handle first condition based on current clause type
                    if (_expressionBuilder._currentClauseType == ClauseType.Where)
                    {
                        _queryBuilder.AppendCondition("WHERE");
                    }
                    else if (_expressionBuilder._currentClauseType == ClauseType.On)
                    {
                        // Apply join condition with ON keyword
                        _queryBuilder.Append(" ON");

                    }
                    else if (_expressionBuilder._currentClauseType == ClauseType.Having)
                    {
                        _queryBuilder.AppendCondition("HAVING");
                    }
                    
                    _queryBuilder.AddAttribute(attributeName, "", QueryBuilder.TableAliasName);
                    _queryBuilder.Append(" ").Append(_op);
                    
                    var paramName = $"p_{attributeName}_direct";
                    _queryBuilder.AddParameterWithValue(paramName, value);
                }
                else
                {
                    // Use parameterized value
                    var paramName = $"p_{attributeName}_direct";
                    _appendParameter(_op, attributeName, paramName, value, QueryBuilder.TableAliasName);
                }
            }
        }
    }
}