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

        public void BuildExpression(Expression expression, Func<QueryBuilder, BinaryExpression, ExpressionBuilder, string, Clause> clause)
        {
            // If this is a parameter expression, register it with the query
            if (expression is LambdaExpression lambda && lambda.Parameters.Count > 0)
            {
                foreach (var param in lambda.Parameters)
                {
                    // Register parameter directly with the query
                    _query.RegisterParameter(param.Name, param.Type);
                }
                
                // Process the lambda body
                expression = lambda.Body;
            }
            else if (expression is ParameterExpression paramExpr)
            {
                // Direct parameter expression
                _query.RegisterParameter(paramExpr.Name, paramExpr.Type);
            }
            
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

        public virtual void BuildMethodCallExpression(Expression expression, Func<QueryBuilder, BinaryExpression, ExpressionBuilder, string, Clause> clause)
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

        protected void BuildNotExpression(UnaryExpression unaryExpression, Func<QueryBuilder, BinaryExpression, ExpressionBuilder, string, Clause> clause)
        {
            if (unaryExpression.NodeType != ExpressionType.Not)
                throw new NotImplementedException($"Unary expression type {unaryExpression.NodeType} not supported");

            if (unaryExpression.Operand is BinaryExpression binaryOperand)
            {
                switch (binaryOperand.NodeType)
                {
                    case ExpressionType.Equal:
                        clause(_queryBuilder, binaryOperand, this, "<>").Append2();
                        break;
                    case ExpressionType.NotEqual:
                        clause(_queryBuilder, binaryOperand, this, "=").Append2();
                        break;
                    case ExpressionType.GreaterThan:
                        clause(_queryBuilder, binaryOperand, this, "<=").Append2();
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        clause(_queryBuilder, binaryOperand, this, "<").Append2();
                        break;
                    case ExpressionType.LessThan:
                        clause(_queryBuilder, binaryOperand, this, ">=").Append2();
                        break;
                    case ExpressionType.LessThanOrEqual:
                        clause(_queryBuilder, binaryOperand, this, ">").Append2();
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
                clause(_queryBuilder, null, this, "=").AppendWithAttribute(attributeName, false);
            }
            else
            {
                throw new NotImplementedException($"Unary operand type {unaryExpression.Operand.GetType()} not supported");
            }
        }

        protected void BuildBinaryExpression(BinaryExpression binaryExpression, Func<QueryBuilder, BinaryExpression, ExpressionBuilder, string, Clause> clause)
        {
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    clause(_queryBuilder, binaryExpression, this, "=").Append2();
                    break;
                case ExpressionType.NotEqual:
                    clause(_queryBuilder, binaryExpression, this, "<>").Append2();
                    break;
                case ExpressionType.GreaterThan:
                    clause(_queryBuilder, binaryExpression, this, ">").Append2();
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    clause(_queryBuilder, binaryExpression, this, ">=").Append2();
                    break;
                case ExpressionType.LessThan:
                    clause(_queryBuilder, binaryExpression, this, "<").Append2();
                    break;
                case ExpressionType.LessThanOrEqual:
                    clause(_queryBuilder, binaryExpression, this, "<=").Append2();
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
                var alias = _queryBuilder.GetAliasForType(paramExpr.Type);
                // If we got a null alias, use the default
                if (string.IsNullOrEmpty(alias))
                {
                    // This is likely an error - we got a type that hasn't been properly registered
                    throw new InvalidOperationException(
                        $"Type {paramExpr.Type.Name} was not found in the query's registered entity types. " +
                        "Check that all joined types are properly registered.");
                }
                return alias;
            }
            
            // For non-parameter expressions, use the default alias
            // Note: This might be a nested property access (e.g. person.Address.Street)
            return QueryBuilder.TableAliasName;
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

            public static Clause And(QueryBuilder qb, BinaryExpression binaryExpression, ExpressionBuilder eb, string op)
            {
                return new Clause(binaryExpression, op, qb.AddCondition, qb.AddCondition, qb, eb, "AND");
            }

            public static Clause Or(QueryBuilder qb, BinaryExpression binaryExpression, ExpressionBuilder eb, string op)
            {
                return new Clause(binaryExpression, op, qb.OrCondition, qb.OrCondition, qb, eb, "OR");
            }

            public void Append2()
            {
                if (_binaryExpression == null)
                    return;
                
                var left = _binaryExpression.Left;
                var right = _binaryExpression.Right;
                
                // Step 1: Determine operand order
                (left, right, bool isSwapped) = DetermineOperandOrder(left, right);

                // Step 2: Process the binary expression with determined order
                ProcessOrderedExpression(left, right, isSwapped);
            }

            private void ProcessOrderedExpression(Expression left, Expression right, bool isSwapped)
            {
                // Left expression must be a member access for query columns
                if (left is MemberExpression leftMember)
                {
                    // 1. Handle SQL clause prefix (WHERE/ON/HAVING) if needed
                    HandleClausePrefix();
                    
                    // 2. Process the left side (always a query property)
                    ProcessLeftSide(leftMember, isSwapped);
                    
                    // 3. Process the right side based on its type
                    ProcessRightSide(right, _expressionBuilder.GetAttributeName(leftMember));
                }
                else
                {
                    throw new NotImplementedException($"Left operand type {left.NodeType} not supported");
                }
            }

            private void HandleClausePrefix()
            {
                if (_queryBuilder.IsFirstCondition())
                {
                    // Add the appropriate clause keyword
                    switch (_expressionBuilder._currentClauseType)
                    {
                        case ClauseType.Where:
                            _queryBuilder.AppendCondition("WHERE");
                            break;
                        case ClauseType.On:
                            _queryBuilder.Append(" ON");
                            break;
                        case ClauseType.Having:
                            _queryBuilder.AppendCondition("HAVING");
                            break;
                    }
                }
                else
                {
                    // Not the first condition, add the proper conjunction
                    _queryBuilder.Append(" ").Append(_condition);
                }
            }

            private void ProcessLeftSide(MemberExpression leftMember, bool isSwapped)
            {
                string tableAlias = _expressionBuilder.GetTableAliasForMember(leftMember);
                string attributeName = _expressionBuilder.GetAttributeName(leftMember);
                
                // Add the column reference
                _queryBuilder.AddAttribute(attributeName, "", tableAlias);
                
                // Add the operator (reversed if needed)
                _queryBuilder.Append(" ").Append(isSwapped ? ReverseOperator(_op) : _op);
            }

            private void ProcessRightSide(Expression right, string leftAttributeName)
            {
                switch (right.NodeType)
                {
                    case ExpressionType.Constant:
                        // Simple constant value
                        AddParameterFromConstant((ConstantExpression)right, leftAttributeName);
                        break;
                        
                    case ExpressionType.MemberAccess:
                        // Member could be: another query property or a closure variable
                        ProcessRightMember((MemberExpression)right, leftAttributeName);
                        break;
                        
                    default:
                        throw new NotImplementedException($"Right operand type {right.NodeType} not supported");
                }
            }

            private void AddParameterFromConstant(ConstantExpression constExpr, string attributeName)
            {
                var paramName = $"p_{attributeName}";
                _queryBuilder.AddParameterWithValue(paramName, constExpr.Value);
            }

            private void ProcessRightMember(MemberExpression memberExpr, string leftAttributeName)
            {
                string rightTableAlias = _expressionBuilder.GetTableAliasForMember(memberExpr);
                
                if (IsPropertyInQuery(memberExpr, rightTableAlias))
                {
                    // This is another column in the query
                    string rightAttributeName = _expressionBuilder.GetAttributeName(memberExpr);
                    _queryBuilder.AddAttribute(rightAttributeName, "", rightTableAlias);
                }
                else
                {
                    // This is a closure variable - add as parameter
                    object value = Expression.Lambda(memberExpr).Compile().DynamicInvoke();
                    string paramName = $"p_{leftAttributeName}";
                    _queryBuilder.AddParameterWithValue(paramName, value);
                }
            }

            private string ReverseOperator(string op)
            {
                // Return the reversed comparison operator if needed
                return op switch
                {
                    ">" => "<",
                    "<" => ">",
                    ">=" => "<=",
                    "<=" => ">=",
                    _ => op  // = and <> operators remain the same when reversed
                };
            }

            private (Expression left, Expression right, bool swapped) DetermineOperandOrder(Expression left, Expression right)
            {
                bool swapped = false;
                
                // Case 1: Simple constant on left
                if (left.NodeType == ExpressionType.Constant && right.NodeType == ExpressionType.MemberAccess)
                {
                    return (right, left, true);
                }
                
                // Case 2: Both are member expressions - need to check if right is part of the query and left is not
                if (left is MemberExpression leftMember && right is MemberExpression rightMember)
                {
                    // Determine if each member belongs to the query or an external context
                    bool leftIsQueryProperty = IsExpressionInQueryContext(leftMember);
                    bool rightIsQueryProperty = IsExpressionInQueryContext(rightMember);
                    
                    // If right is a query property and left is not (e.g., closure variable)
                    // then we should swap them
                    if (rightIsQueryProperty && !leftIsQueryProperty)
                    {
                        return (right, left, true);
                    }
                }
                
                // Default - keep original order
                return (left, right, swapped);
            }
            
            // Helper method to determine if an expression is in the query context
            private bool IsExpressionInQueryContext(Expression expr)
            {
                // For parameter expressions, check if they're registered in the query
                if (expr is ParameterExpression paramExpr)
                {
                    return _expressionBuilder._query.HasExpressionParameter(paramExpr.Name, paramExpr.Type);
                }
                
                // For member expressions, check the containing expression
                if (expr is MemberExpression memberExpr)
                {
                    // Get the table alias to check if this is a known entity in the query
                    string tableAlias = _expressionBuilder.GetTableAliasForMember(memberExpr);
                    
                    // Check if this member is from a closure (constant) or parameter
                    return IsPropertyInQuery(memberExpr, tableAlias);
                }
                
                // Default to false for unknown expressions
                return false;
            }
            
            private bool IsPropertyInQuery(MemberExpression member, string tableAlias)
            {
                // If this is a closure variable (captured from outer scope)
                if (member.Expression?.NodeType == ExpressionType.Constant)
                {
                    return false; // Constant expressions are always from closures
                }
                
                // If this is a parameter expression, we need to check if it's part of our query
                if (member.Expression is ParameterExpression paramExpr)
                {
                    // Check if this parameter is registered in the query context
                    return _expressionBuilder._query.HasExpressionParameter(paramExpr.Name, paramExpr.Type);
                }
                
                // Handle nested property expressions
                if (member.Expression is MemberExpression nestedMember)
                {
                    // Recursively check if the parent member is in the query
                    return IsPropertyInQuery(nestedMember, tableAlias);
                }
                
                // Check if the type has a registered alias (indicates it's part of the query)
                if (member.Expression?.Type != null && _queryBuilder.HasAliasForType(member.Expression.Type))
                {
                    return true;
                }
                
                // Check if the table alias is registered for any entity in the query
                if (!string.IsNullOrEmpty(tableAlias))
                {
                    // If we have a non-default alias, it likely means this property is from an entity in our query
                    if (tableAlias != QueryBuilder.TableAliasName)
                    {
                        return true;
                    }
                    
                    // If we have the default alias, check if it's a known entity type
                    if (_expressionBuilder._query.GetEntityType(tableAlias) != null)
                    {
                        return true;
                    }
                }
                
                // No strong evidence this property is part of our query
                return false;
            }

            public void AppendWithAttribute(string attributeName, object value)
            {
                // Handle clause prefix
                HandleClausePrefix();
                
                // Add attribute
                _queryBuilder.AddAttribute(attributeName, "", QueryBuilder.TableAliasName);
                _queryBuilder.Append(" ").Append(_op);
                
                // Add parameter
                var paramName = $"p_{attributeName}_direct";
                _queryBuilder.AddParameterWithValue(paramName, value);
            }
        }
    }

    // // Extension for QueryBuilder
    // public static class QueryBuilderExtensions
    // {
    //     public static bool HasAliasForType(this QueryBuilder queryBuilder, Type type)
    //     {
    //         if (type == null)
    //             return false;
    //
    //         // Get alias for the type - this should reuse the same logic
    //         // that GetAliasForType is using
    //         var alias = queryBuilder.GetAliasForType(type);
    //         
    //         // If we got something other than the default, it exists
    //         return !string.IsNullOrEmpty(alias);
    //     }
    // }
}