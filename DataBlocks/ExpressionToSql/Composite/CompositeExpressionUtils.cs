using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;
using ExpressionToSql.Utils;
using ScheMigrator.Migrations;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Utility methods for working with composite expression trees
    /// </summary>
    internal static class CompositeExpressionUtils
    {
        /// <summary>
        /// Gets the expressions from a given body
        /// </summary>
        public static IEnumerable<Expression> GetExpressions(Type type, Expression body)
        {
            switch (body.NodeType)
            {
                case ExpressionType.New:
                    var n = (NewExpression)body;
                    return n.Arguments;
                    
                case ExpressionType.MemberInit:
                    var init = (MemberInitExpression)body;
                    return GetMemberInitExpressions(init);
                    
                case ExpressionType.Parameter:
                    return GetParameterExpressions(type, body);
                    
                default:
                    return new[] { body };
            }
        }

        /// <summary>
        /// Extracts expressions from a member initialization expression
        /// </summary>
        private static IEnumerable<Expression> GetMemberInitExpressions(MemberInitExpression init)
        {
            var memberInitExpressions = new List<Expression>();
            foreach (var binding in init.Bindings)
            {
                if (binding is MemberAssignment assignment)
                {
                    // For direct parameter references (e.g. CompositeModel = root, TargetModel = target),
                    // we want to include all their properties
                    if (assignment.Expression.NodeType == ExpressionType.Parameter)
                    {
                        var param = (ParameterExpression)assignment.Expression;
                        var properties = param.Type.GetProperties()
                            .Where(p => p.GetCustomAttributes(typeof(ScheDataAttribute), true).Any());
                            
                        foreach (var prop in properties)
                        {
                            memberInitExpressions.Add(Expression.Property(param, prop));
                        }
                        continue;
                    }
                    
                    // If the assigned value is a model object, expand it
                    if (IsModelType(assignment.Expression.Type))
                    {
                        // For model properties, we need to expand the child properties
                        var modelProps = assignment.Expression.Type.GetProperties()
                            .Where(p => p.GetCustomAttributes(typeof(ScheDataAttribute), true).Any());
                        
                        foreach (var prop in modelProps)
                        {
                            memberInitExpressions.Add(Expression.Property(assignment.Expression, prop));
                        }
                    }
                    else
                    {
                        // For regular properties, just add the expression
                        memberInitExpressions.Add(assignment.Expression);
                    }
                }
            }
            return memberInitExpressions;
        }

        /// <summary>
        /// Extracts expressions from a parameter expression
        /// </summary>
        private static IEnumerable<Expression> GetParameterExpressions(Type type, Expression paramExpr)
        {
            var propertyInfos = type.GetProperties().Where(p => 
                p.GetCustomAttributes(typeof(ScheDataAttribute), true).Any());
            return propertyInfos.Select(pi => Expression.Property(paramExpr, pi));
        }

        /// <summary>
        /// Checks if a type implements IModel interface or has ScheModel attribute
        /// </summary>
        private static bool IsModelType(Type type)
        {
            return typeof(IModel).IsAssignableFrom(type) ||
                   type.GetCustomAttributes(typeof(ScheModelAttribute), true).Any();
        }

        /// <summary>
        /// Adds expressions to the query builder (appending only)
        /// </summary>
        public static QueryBuilder AddExpressions(IEnumerable<Expression> expressions, Type rootType, QueryBuilder qb, params Type[] joinTypes)
        {
            return ProcessExpressions(expressions, rootType, qb, false, joinTypes);
        }

        /// <summary>
        /// Adds expressions to the query builder with option to prepend
        /// </summary>
        public static QueryBuilder AddExpressions(IEnumerable<Expression> expressions, Type rootType, QueryBuilder qb, bool prepend, params Type[] joinTypes)
        {
            return ProcessExpressions(expressions, rootType, qb, prepend, joinTypes);
        }

        /// <summary>
        /// Prepends SELECT expressions to the query builder
        /// </summary>
        public static QueryBuilder PrependSelectExpressions(IEnumerable<Expression> expressions, Type rootType, QueryBuilder qb, params Type[] joinTypes)
        {
            ProcessExpressions(expressions, rootType, qb, true, joinTypes);
            return qb.PrependSelect();
        }

        /// <summary>
        /// Core expression processing method that handles both append and prepend
        /// </summary>
        private static QueryBuilder ProcessExpressions(IEnumerable<Expression> expressions, Type rootType, QueryBuilder qb, bool prepend, Type[] joinTypes)
        {
            // Convert to list to avoid multiple enumeration
            var expressionList = expressions.ToList();
            if (expressionList.Count == 0) return qb;
            
            // Process expressions in the appropriate order
            var processOrder = prepend ? expressionList.AsEnumerable().Reverse() : expressionList;
            bool isFirst = true;
            
            foreach (var expression in processOrder)
            {
                if (!isFirst)
                {
                    // Add separator between expressions
                    if (prepend) qb.PrependSeparator(); else qb.AppendSeparator();
                }
                
                ProcessExpression(expression, rootType, qb, joinTypes, prepend);
                isFirst = false;
            }
            
            return qb;
        }
        
        /// <summary>
        /// Recursive method to process any expression type
        /// </summary>
        private static void ProcessExpression(Expression expression, Type rootType, QueryBuilder qb, Type[] joinTypes, bool prepend)
        {
            if (expression == null) return;
            
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    var c = (ConstantExpression)expression;
                    qb.AddValue(c.Value, prepend);
                    break;
                
                case ExpressionType.MemberAccess:
                    var m = (MemberExpression)expression;
                    ProcessMemberAccess(m, rootType, qb, joinTypes, prepend);
                    break;
                
                case ExpressionType.Parameter:
                    ProcessParameterProperties(expression, rootType, qb, joinTypes, prepend);
                    break;
                
                case ExpressionType.New:
                    var newExpr = (NewExpression)expression;
                    ProcessExpressionList(newExpr.Arguments, rootType, qb, joinTypes, prepend);
                    break;
                
                case ExpressionType.MemberInit:
                    var initExpr = (MemberInitExpression)expression;
                    ProcessMemberInit(initExpr, rootType, qb, joinTypes, prepend);
                    break;
                
                default:
                    throw new NotImplementedException($"Expression type {expression.NodeType} not supported: {expression}");
            }
        }
        
        /// <summary>
        /// Process a list of expressions with proper separator handling
        /// </summary>
        private static void ProcessExpressionList(IEnumerable<Expression> expressions, Type rootType, QueryBuilder qb, Type[] joinTypes, bool prepend)
        {
            var exprList = expressions.ToList();
            if (exprList.Count == 0) return;
            
            // Process in the appropriate order
            var processOrder = prepend ? exprList.AsEnumerable().Reverse() : exprList;
            bool isFirst = true;
            
            foreach (var expr in processOrder)
            {
                if (!isFirst)
                {
                    // Add separator between expressions
                    if (prepend) qb.PrependSeparator(); else qb.AppendSeparator();
                }
                
                ProcessExpression(expr, rootType, qb, joinTypes, prepend);
                isFirst = false;
            }
        }
        
        /// <summary>
        /// Process a MemberInit expression by handling each binding
        /// </summary>
        private static void ProcessMemberInit(MemberInitExpression initExpr, Type rootType, QueryBuilder qb, Type[] joinTypes, bool prepend)
        {
            var bindings = initExpr.Bindings.OfType<MemberAssignment>().ToList();
            if (bindings.Count == 0) return;
            
            // Process in the appropriate order
            var processOrder = prepend ? bindings.AsEnumerable().Reverse() : bindings;
            bool isFirst = true;
            
            foreach (var binding in processOrder)
            {
                // Special case for parameter expressions to expand all their properties
                if (binding.Expression.NodeType == ExpressionType.Parameter)
                {
                    var param = (ParameterExpression)binding.Expression;
                    Type? paramType = DetermineParameterType(param, rootType, joinTypes);
                    
                    if (paramType != null)
                    {
                        if (!isFirst)
                        {
                            // Add separator between bindings
                            if (prepend) qb.PrependSeparator(); else qb.AppendSeparator();
                        }
                        
                        ProcessParameterProperties(param, rootType, qb, joinTypes, prepend);
                        isFirst = false;
                    }
                }
                else
                {
                    if (!isFirst)
                    {
                        // Add separator between bindings
                        if (prepend) qb.PrependSeparator(); else qb.AppendSeparator();
                    }
                    
                    ProcessExpression(binding.Expression, rootType, qb, joinTypes, prepend);
                    isFirst = false;
                }
            }
        }
        
        /// <summary>
        /// Process a member access expression with table aliasing
        /// </summary>
        private static void ProcessMemberAccess(MemberExpression m, Type rootType, QueryBuilder qb, Type[] joinTypes, bool prepend)
        {
            // Check if this is from the root type or a joined type
            string? tableAlias = DetermineTableAlias(m, rootType, joinTypes);
            
            // If no alias was determined but this is a root type member,
            // get the alias from the QueryBuilder (handles subquery aliases)
            if (tableAlias == null && m.Member.DeclaringType == rootType)
            {
                tableAlias = qb.GetAliasForType(rootType) ?? QueryBuilder.TableAliasName;
            }
            
            if (tableAlias != null)
            {
                try
                {
                    // Use the centralized utility to get the field name
                    var declaringType = m.Member.DeclaringType;
                    string fieldName = SqlTypeUtils.ResolveFieldName(m.Member, declaringType);
                    string columnAlias = fieldName != m.Member.Name ? m.Member.Name : "";
                    qb.AddAttribute(fieldName, columnAlias, tableAlias, prepend);
                }
                catch (Exception)
                {
                    // If field name resolution fails, fall back to member name
                    qb.AddAttribute(m.Member.Name, "", tableAlias, prepend);
                }
            }
            else
            {
                // Not from a table we know about - use parameter
                qb.AddParameter(m.Member.Name, prepend);
            }
        }
        
        /// <summary>
        /// Process all properties of a parameter expression
        /// </summary>
        private static void ProcessParameterProperties(Expression paramExpr, Type rootType, QueryBuilder qb, Type[] joinTypes, bool prepend)
        {
            if (paramExpr.NodeType != ExpressionType.Parameter) return;
            
            var param = (ParameterExpression)paramExpr;
            Type? paramType = DetermineParameterType(param, rootType, joinTypes);
            if (paramType == null) return;
            
            string? tableAlias = DetermineTableAliasForType(paramType, rootType, joinTypes);
            if (tableAlias == null) return;
            
            // Get schema-annotated properties
            var properties = paramType.GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(ScheDataAttribute), true).Any())
                .ToList();
            
            if (properties.Count == 0) return;
            
            // Process properties in the appropriate order
            var processOrder = prepend ? properties.AsEnumerable().Reverse() : properties;
            bool isFirst = true;
            
            foreach (var prop in processOrder)
            {
                if (!isFirst)
                {
                    // Add separator between properties
                    if (prepend) qb.PrependSeparator(); else qb.AppendSeparator();
                }
                
                try
                {
                    // Resolve field name and add attribute
                    string fieldName = SqlTypeUtils.ResolveFieldName(prop, paramType);
                    string columnAlias = fieldName != prop.Name ? prop.Name : "";
                    qb.AddAttribute(fieldName, columnAlias, tableAlias, prepend);
                }
                catch (Exception)
                {
                    // If resolution fails, fall back to property name
                    qb.AddAttribute(prop.Name, "", tableAlias, prepend);
                }
                
                isFirst = false;
            }
        }
        
        /// <summary>
        /// Determines the appropriate table alias for a member expression
        /// </summary>
        private static string? DetermineTableAlias(MemberExpression m, Type rootType, Type[] joinTypes)
        {
            if (m.Expression?.NodeType == ExpressionType.Parameter)
            {
                var paramExpr = (ParameterExpression)m.Expression;
                return DetermineTableAliasForType(paramExpr.Type, rootType, joinTypes);
            }
            
            // Handle nested properties by using the base property's declared type
            if (m.Expression?.NodeType == ExpressionType.MemberAccess)
            {
                var parentMember = (MemberExpression)m.Expression;
                if (parentMember.Member is PropertyInfo prop)
                {
                    return DetermineTableAliasForType(prop.PropertyType, rootType, joinTypes);
                }
            }
            
            // Default to root type alias if we can't determine
            return QueryBuilder.TableAliasName;
        }
        
        /// <summary>
        /// Determines the appropriate table alias for a type based on position in join chain
        /// </summary>
        private static string? DetermineTableAliasForType(Type type, Type rootType, Type[] joinTypes)
        {
            if (type == rootType)
            {
                // For root type, let the QueryBuilder determine the alias
                // This ensures we use the subquery alias if one is registered
                return null;
            }
            
            // For join types, determine the appropriate letter
            for (int i = 0; i < joinTypes.Length; i++)
            {
                if (type == joinTypes[i])
                {
                    char c = (char)('b' + i);
                    return c.ToString();
                }
            }
            
            return null; // No table alias could be determined
        }
        
        /// <summary>
        /// Determine the actual type of a parameter by comparing with root and join types
        /// </summary>
        private static Type? DetermineParameterType(ParameterExpression param, Type rootType, Type[] joinTypes)
        {
            if (param.Type == rootType)
            {
                return rootType;
            }
            
            if (joinTypes != null)
            {
                return joinTypes.FirstOrDefault(t => t == param.Type);
            }
            
            return null;
        }

        /// <summary>
        /// Helper method to create a PageByRoot query that paginates at the root table level
        /// </summary>
        /// <typeparam name="TRoot">The root table entity type</typeparam>
        /// <param name="rootTable">The root table</param>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <param name="dialect">The SQL dialect to use</param>
        /// <returns>A PageByRoot query ready for further composition</returns>
        public static CompositePageByRoot<TRoot> CreatePageByRootQuery<TRoot>(
            Table rootTable, 
            int pageIndex, 
            int pageSize, 
            ISqlDialect dialect)
        {
            // Create the base FROM query
            var fromQuery = new CompositeFrom<TRoot>(rootTable, dialect);
            
            // Apply PageByRoot to create a subquery-based pagination
            return fromQuery.PageByRoot(pageIndex, pageSize);
        }
        
        /// <summary>
        /// Helper method to create a PageByRoot query with a schema-based table
        /// </summary>
        /// <typeparam name="TRoot">The root table entity type</typeparam>
        /// <param name="schema">The schema definition</param>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <param name="dialect">The SQL dialect to use</param>
        /// <returns>A PageByRoot query ready for further composition</returns>
        public static CompositePageByRoot<TRoot> CreatePageByRootQuery<TRoot>(
            DataSchema schema, 
            int pageIndex, 
            int pageSize, 
            ISqlDialect dialect)
        {
            // Create a table from the schema
            var rootTable = new Table<TRoot> { Name = schema.CollectionName, Schema = schema.SchemaName };
            
            // Delegate to the main implementation
            return CreatePageByRootQuery<TRoot>(rootTable, pageIndex, pageSize, dialect);
        }
    }
} 