using System;
using System.Linq;
using System.Linq.Expressions;
using ExpressionToSql;
using ScheMigrator.Migrations;
using ExpressionToSql.Utils;
using System.Collections.Generic;

namespace DataBlocks.ExpressionToSql.Expressions
{
    public enum ClauseType
    {
        Where,
        On,
        Having
    }

    /// <summary>
    /// Resolves entity member fields to SQL column names
    /// </summary>
    public class MemberNameResolver
    {
        private readonly Query _query;
        private readonly QueryBuilder _queryBuilder;
        private readonly string _rootAlias;

        public MemberNameResolver(Query query, QueryBuilder queryBuilder, string rootAlias = null)
        {
            _query = query;
            _queryBuilder = queryBuilder;
            _rootAlias = rootAlias ?? QueryBuilder.TableAliasName;
        }

        /// <summary>
        /// Gets the SQL column name for a member expression
        /// </summary>
        public string ResolveMemberName(MemberExpression expression)
        {
            try
            {
                // Get the appropriate table alias
                string tableAlias = ResolveAliasForMember(expression);
                
                // Get the entity type for this alias from the AliasRegistry
                Type entityType = _query.Aliases.GetTypeForAlias(tableAlias);
                
                // Resolve the field name
                return SqlTypeUtils.ResolveFieldName(expression.Member, entityType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve member name for {expression.Member.Name}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Gets the correct alias for a member expression using the centralized AliasRegistry
        /// </summary>
        public string ResolveAliasForMember(MemberExpression member)
        {
            try 
            {
                // Fully delegate to AliasRegistry for consistent alias resolution
                return _query.Aliases.GetAliasForMemberExpression(member);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve alias for member {member.Member.Name}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Determines if an expression belongs to the query context using AliasRegistry
        /// </summary>
        public bool IsExpressionInQueryContext(Expression expr)
        {
            // Delegate to the centralized AliasRegistry
            return _query.Aliases.IsExpressionInQueryContext(expr);
        }
    }

    public class ExpressionBuilder
    {
        private readonly ISqlDialect _dialect;
        private readonly QueryBuilder _queryBuilder;
        private readonly Query _query;
        private bool _firstCondition = true;
        private ClauseType _currentClauseType = ClauseType.Where;
        private string _rootAlias;
        
        // Member name resolution helper
        private readonly MemberNameResolver _memberNameResolver;

        public ExpressionBuilder(
            Query query, 
            QueryBuilder queryBuilder, 
            string rootAlias = null,
            ClauseType clauseType = ClauseType.Where)
        {
            _dialect = query.Dialect;
            _queryBuilder = queryBuilder;
            _query = query;
            _rootAlias = rootAlias ?? QueryBuilder.TableAliasName;
            
            // Initialize member resolver
            _memberNameResolver = new MemberNameResolver(query, queryBuilder, _rootAlias);
            _currentClauseType = clauseType;
        }

        public ExpressionBuilder WithClauseType(ClauseType clauseType)
        {
            _currentClauseType = clauseType;
            return this;
        }

        public ExpressionBuilder WithRootAlias(string rootAlias)
        {
            if (string.IsNullOrEmpty(rootAlias))
                throw new ArgumentException("Root alias cannot be null or empty", nameof(rootAlias));
                
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
                    if (expression is MemberExpression memberExpr &&
                        memberExpr.Expression is ConstantExpression constExpr)
                    {
                        // Handle member access on a constant (closure variable)
                        object value = Expression.Lambda(memberExpr).Compile().DynamicInvoke();
                        _queryBuilder.AddValue(value);
                    }
                    else if (expression is UnaryExpression unaryExpr && 
                             unaryExpr.NodeType == ExpressionType.Convert)
                    {
                        // Handle type conversion expressions by evaluating the operand
                        BuildExpression(unaryExpr.Operand, clause);
                    }
                    else
                    {
                        throw new NotImplementedException($"Expression type {expression.NodeType} not supported");
                    }
                    break;
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
                var tableAlias = GetTableAliasForMember(memberExpression);
                clause(_queryBuilder, null, this, "=").AppendWithAttribute(attributeName, tableAlias, false);
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
        public string GetAttributeName(MemberExpression expression)
        {
            return _memberNameResolver.ResolveMemberName(expression);
        }

        // Helper method to get the table alias for a member expression
        public string GetTableAliasForMember(MemberExpression member)
        {
            return _memberNameResolver.ResolveAliasForMember(member);
        }
        
        // Helper to determine if an expression is part of the query context
        public bool IsExpressionInQueryContext(Expression expr)
        {
            return _memberNameResolver.IsExpressionInQueryContext(expr);
        }

        /// <summary>
        /// Registers a parameter with its type and specific alias to ensure proper alias resolution
        /// </summary>
        public ExpressionBuilder RegisterParameterType(string paramName, Type paramType, string alias)
        {
            if (!string.IsNullOrEmpty(paramName) && paramType != null && !string.IsNullOrEmpty(alias))
            {
                // Use the centralized AliasRegistry
                _query.Aliases.RegisterParameterAlias(paramName, paramType, alias);
                
                // Ensure QueryBuilder knows about this mapping too
                _queryBuilder.RegisterTableAliasForType(paramType, alias);
            }
            return this;
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
                
                // Fixed: Use the proper alias instead of hardcoding the default
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
                        string tableAlias = GetDominantAlias();
                        string condition = $"{tableAlias}.{attributeName} = @{checkParamName}";
                        
                        // If this exact condition already exists, skip adding it again
                        if (sql.Contains(condition, StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }
                
                var paramName = $"p_{attributeName}";
                _queryBuilder.AddParameterWithValue(paramName, constExpr.Value);
            }

            // Helper to get the dominant table alias for any expression context
            private string GetDominantAlias()
            {
                // If this is a unary expression (with null binary expression), throw exception
                if (_binaryExpression == null)
                {
                    throw new InvalidOperationException("Cannot determine dominant alias: binary expression is null");
                }
                
                // For binary expressions, use proper alias resolution
                return _expressionBuilder._query.Aliases.GetDominantAliasForBinaryExpression(
                    _binaryExpression);
            }

            private void ProcessRightMember(MemberExpression memberExpr, string leftAttributeName)
            {
                if (IsPropertyInQuery(memberExpr))
                {
                    // This is another column in the query
                    string rightAttributeName = _expressionBuilder.GetAttributeName(memberExpr);
                    string rightTableAlias = _expressionBuilder.GetTableAliasForMember(memberExpr);
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
                    // Use AliasRegistry to determine query context
                    bool leftIsQueryProperty = _expressionBuilder._query.Aliases.IsExpressionInQueryContext(leftMember);
                    bool rightIsQueryProperty = _expressionBuilder._query.Aliases.IsExpressionInQueryContext(rightMember);
                    
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
            
            private bool IsPropertyInQuery(MemberExpression member)
            {
                // Use AliasRegistry for consistent query context determination
                return _expressionBuilder._query.Aliases.IsExpressionInQueryContext(member);
            }

            public void AppendWithAttribute(string attributeName, string tableAlias, object value)
            {
                if (string.IsNullOrEmpty(attributeName))
                    throw new ArgumentException("Attribute name cannot be null or empty", nameof(attributeName));
                    
                // Handle clause prefix
                HandleClausePrefix();
                
                // Add attribute with the proper alias
                _queryBuilder.AddAttribute(attributeName, "", tableAlias);
                _queryBuilder.Append(" ").Append(_op);
                
                // Add parameter
                var paramName = $"p_{attributeName}_direct";
                _queryBuilder.AddParameterWithValue(paramName, value);
            }
        }

        /// <summary>
        /// Builds a member expression into SQL
        /// </summary>
        private string BuildMemberExpression(MemberExpression memberExpression)
        {
            var memberName = _memberNameResolver.ResolveMemberName(memberExpression);
            var alias = _memberNameResolver.ResolveAliasForMember(memberExpression);
            
            return !string.IsNullOrEmpty(alias)
                ? $"{alias}.{memberName}"
                : memberName;
        }
    }
}