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
        private string _rootAlias = QueryBuilder.TableAliasName;

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

        public ExpressionBuilder WithRootAlias(string rootAlias)
        {
            _rootAlias = rootAlias;
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
                    // Create a new builder for each side to ensure correct alias context
                    var leftBuilder = new ExpressionBuilder(_query, _queryBuilder)
                        .WithClauseType(_currentClauseType)
                        .WithRootAlias(_rootAlias);
                        
                    leftBuilder.BuildExpression(binaryExpression.Left, clause);
                    
                    // For the right side, ensure we maintain the same root alias context
                    var rightBuilder = new ExpressionBuilder(_query, _queryBuilder)
                        .WithClauseType(_currentClauseType)
                        .WithRootAlias(_rootAlias);
                        
                    rightBuilder.BuildExpression(binaryExpression.Right, Clause.And);
                    break;
                case ExpressionType.OrElse:
                    // Same pattern for OR expressions
                    var leftOrBuilder = new ExpressionBuilder(_query, _queryBuilder)
                        .WithClauseType(_currentClauseType)
                        .WithRootAlias(_rootAlias);
                        
                    leftOrBuilder.BuildExpression(binaryExpression.Left, clause);
                    
                    var rightOrBuilder = new ExpressionBuilder(_query, _queryBuilder)
                        .WithClauseType(_currentClauseType)
                        .WithRootAlias(_rootAlias);
                        
                    rightOrBuilder.BuildExpression(binaryExpression.Right, Clause.Or);
                    break;
                default:
                    throw new NotImplementedException($"Binary expression type {binaryExpression.NodeType} not supported");
            }
        }

        // Get attribute name handling type information
        protected string GetAttributeName(MemberExpression expression)
        {
            // Get the appropriate table alias
            string tableAlias = GetTableAliasForMember(expression);
            
            // Get the entity type for this alias
            Type entityType = _query.GetEntityType(tableAlias);
            
            // If we don't have an entity type for this alias, try to get it from the expression parameter
            if (entityType == null && expression.Expression is ParameterExpression paramExpr)
            {
                entityType = paramExpr.Type;
            }
            
            // If still null, try with the root type as a fallback
            if (entityType == null && !string.IsNullOrEmpty(_rootAlias))
            {
                entityType = _query.GetEntityType(_rootAlias);
            }
            
            // Last resort, try the default table alias
            if (entityType == null)
            {
                entityType = _query.GetEntityType(QueryBuilder.TableAliasName);
            }
            
            // If we still don't have an entity type, this is an error
            if (entityType == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve entity type for member {expression.Member.Name}. " +
                    $"Check that the entity type is properly registered in the query context.");
            }
            
            // Resolve the field name
            return SqlTypeUtils.ResolveFieldName(expression.Member, entityType);
        }

        // Helper method to get the table alias for a member expression
        private string GetTableAliasForMember(MemberExpression expression)
        {
            if (expression.Expression is ParameterExpression paramExpr)
            {
                // First try getting the alias directly from the QueryBuilder
                string alias = _queryBuilder.GetAliasForType(paramExpr.Type);
                
                // If we have an alias for this type, use it (with any mappings applied)
                if (!string.IsNullOrEmpty(alias))
                {
                    return _queryBuilder.GetEffectiveAlias(alias);
                }
                
                // If we don't have an alias in the QueryBuilder, check the Query's EntityTypes
                // to see if we have a mapping for this type
                foreach (var pair in _query.EntityTypes)
                {
                    if (pair.Value == paramExpr.Type)
                    {
                        alias = pair.Key;
                        
                        // Register this alias with the QueryBuilder to ensure consistent usage
                        _queryBuilder.RegisterTableAliasForType(paramExpr.Type, alias);
                        
                        return _queryBuilder.GetEffectiveAlias(alias);
                    }
                }
                
                // If no specific alias found but this is the root type and we have a root alias, use that
                if (!string.IsNullOrEmpty(_rootAlias))
                {
                    Type rootType = _query.GetEntityType(_rootAlias);
                    if (rootType == paramExpr.Type)
                    {
                        return _rootAlias;
                    }
                }
                
                // As a last resort, create a new alias for this type
                alias = _queryBuilder.GetOrCreateAliasForType(paramExpr.Type);
                
                // Register it with our query for future reference
                _query.RegisterEntityType(alias, paramExpr.Type);
                
                return alias;
            }
            
            // For non-parameter expressions, use the root alias if specified
            if (!string.IsNullOrEmpty(_rootAlias) && _rootAlias != QueryBuilder.TableAliasName)
            {
                return _rootAlias;
            }
            
            // For other cases, use the default alias
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
                // Get the table alias for this member expression
                // This should properly map to different tables based on the parameter type
                string tableAlias = _expressionBuilder.GetTableAliasForMember(leftMember);
                
                // Get the attribute name
                string attributeName = _expressionBuilder.GetAttributeName(leftMember);
                
                // Add the column reference with the determined alias
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
                // For WHERE clauses, check if we might be adding a duplicate condition
                if (_expressionBuilder._currentClauseType == ClauseType.Where && !_queryBuilder.IsFirstCondition())
                {
                    // Check if the current SQL already contains a similar condition
                    string sql = _queryBuilder.ToString();
                    if (sql.Contains("WHERE ", StringComparison.OrdinalIgnoreCase))
                    {
                        string checkParamName = $"p_{attributeName}_direct";
                        string tableAlias = GetDominantTableAlias();
                        string condition = $"{tableAlias}.{attributeName} = @{checkParamName}";
                        
                        // If this exact condition already exists, skip adding it again
                        if (sql.Contains(condition, StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }
                
                var paramName = $"p_{attributeName}";
                _queryBuilder.AddParameterWithValue(paramName, constExpr.Value);
            }

            // Helper to get the dominant table alias (either the specified one or root alias)
            private string GetDominantTableAlias()
            {
                // In a subquery context, prefer the subquery alias over the default
                if (_expressionBuilder._rootAlias != QueryBuilder.TableAliasName)
                    return _expressionBuilder._rootAlias;
                
                return _binaryExpression?.Left is MemberExpression leftMember ?
                    _expressionBuilder.GetTableAliasForMember(leftMember) : QueryBuilder.TableAliasName;
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
                    // If the root alias is specified (e.g., in a subquery context)
                    // and is different from the default alias, then we should treat
                    // root expressions as part of the query
                    if (!string.IsNullOrEmpty(_expressionBuilder._rootAlias) && 
                        _expressionBuilder._rootAlias != QueryBuilder.TableAliasName)
                    {
                        // If expression uses a parameter, check if it matches the root type
                        if (memberExpr.Expression is ParameterExpression paramMemberExpr &&
                            _expressionBuilder._query.GetEntityType(_expressionBuilder._rootAlias) == paramMemberExpr.Type)
                        {
                            return true;
                        }
                    }
                    
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
                    // Special handling for subquery contexts
                    if (!string.IsNullOrEmpty(_expressionBuilder._rootAlias) && 
                        _expressionBuilder._rootAlias != QueryBuilder.TableAliasName)
                    {
                        // If the root type matches this parameter's type, it's likely part of the query
                        Type rootType = _expressionBuilder._query.GetEntityType(_expressionBuilder._rootAlias);
                        if (rootType == paramExpr.Type)
                        {
                            return true;
                        }
                    }
                    
                    // Regular check if this parameter is registered in the query context
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
                    
                    // If we have a custom root alias, check if the tableAlias is the root alias
                    if (!string.IsNullOrEmpty(_expressionBuilder._rootAlias) && 
                        tableAlias == _expressionBuilder._rootAlias)
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

        private void BuildMemberExpression(MemberExpression expression)
        {
            if (expression.Expression is ParameterExpression parameter)
            {
                string alias = parameter.Type == _query.GetEntityType(QueryBuilder.TableAliasName) 
                    ? _rootAlias 
                    : _queryBuilder.GetAliasForType(parameter.Type) ?? parameter.Name;
                
                _queryBuilder.AddAttribute(expression.Member.Name, "", alias);
            }
            else
            {
                // For nested expressions, we need to handle them without using BuildExpression directly
                if (expression.Expression is MemberExpression memberExpr)
                {
                    // Recursively process the parent member
                    BuildMemberExpression(memberExpr);
                    _queryBuilder.Append(".").Append(expression.Member.Name);
                }
                else if (expression.Expression is ConstantExpression constExpr)
                {
                    // For constant expressions, just append the value and property
                    _queryBuilder.Append(constExpr.Value?.ToString() ?? "NULL");
                    _queryBuilder.Append(".").Append(expression.Member.Name);
                }
                else
                {
                    // For other expression types, evaluate and append
                    var value = Expression.Lambda(expression.Expression).Compile().DynamicInvoke();
                    _queryBuilder.Append(value?.ToString() ?? "NULL");
                    _queryBuilder.Append(".").Append(expression.Member.Name);
                }
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