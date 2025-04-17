using System;
using System.Linq.Expressions;
using DataBlocks.ExpressionToSql.Expressions;
using System.Collections.Generic;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Base class for all CompositeWhere implementations to centralize common functionality
    /// </summary>
    public abstract class CompositeWhereBase<TRoot> : Query
    {
        protected readonly CompositeJoinBase<TRoot> _baseJoin;

        protected CompositeWhereBase(CompositeJoinBase<TRoot> baseJoin)
            : base(baseJoin.Dialect)
        {
            _baseJoin = baseJoin;
            CopyAliasesFrom(baseJoin);
        }

        // Simplified method to register parameter aliases for lambda expressions
        protected void RegisterParameterAliases<TDelegate>(Expression<TDelegate> expression, Dictionary<Type, string> typeAliases)
        {
            // Skip if no expression or no parameters
            if (expression == null || expression.Parameters.Count == 0)
                return;
                
            // Register all parameters with their corresponding types
            foreach (var param in expression.Parameters)
            {
                var paramType = param.Type;
                
                // Look up the alias for this parameter's type
                if (typeAliases.TryGetValue(paramType, out string alias))
                {
                    // Register in AliasRegistry
                    Aliases.RegisterParameterAlias(param.Name, paramType, alias);
                }
            }
        }

        // Simplified method to resolve aliases for all expected types
        protected Dictionary<Type, string> ResolveTypeAliases(QueryBuilder qb)
        {
            var result = new Dictionary<Type, string>();
            
            // Get all expected types for this query
            foreach (var type in GetExpectedTypes())
            {
                // Get alias from our AliasRegistry, creating one if it doesn't exist
                string alias = Aliases.GetAliasForType(type);
                
                // Apply any alias redirection (e.g., from subqueries)
                string effectiveAlias = Aliases.GetEffectiveAlias(alias);
                
                result[type] = effectiveAlias;
                
                // Make sure the QueryBuilder knows about this mapping
                qb.RegisterTableAliasForType(type, effectiveAlias);
            }
            
            return result;
        }

        protected string GetRequiredAlias(Dictionary<Type, string> typeAliases, Type type, string typeName)
        {
            return typeAliases.TryGetValue(type, out var alias)
                ? alias
                : throw new InvalidOperationException($"No alias mapping found for {typeName} {type.Name}");
        }
        
        // Get the types that should be expected in this query based on the generic parameters
        protected virtual Type[] GetExpectedTypes()
        {
            // Base implementation just returns root type
            return new Type[] { typeof(TRoot) };
        }
    }

    /// <summary>
    /// Represents a WHERE clause in a composite query with the root table only
    /// </summary>
    public class CompositeWhere<TRoot> : CompositeWhereBase<TRoot>
    {
        private readonly Expression<Func<TRoot, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot> baseJoin, Expression<Func<TRoot, bool>> predicate)
            : base(baseJoin)
        {
            _predicate = predicate;
            RegisterExpressionParameter(predicate);
            
            // Register the root type alias immediately - use RegisterType to ensure registration
            var rootAlias = Aliases.RegisterType(typeof(TRoot));
            
            // Register parameters by matching parameter type with entity type
            foreach (var param in predicate.Parameters)
            {
                if (param.Type == typeof(TRoot))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TRoot), rootAlias);
                }
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);

            // Make sure all type-to-alias mappings are registered with the QueryBuilder
            ResolveTypeAliases(qb);
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause with the correct alias
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where);
            
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            // Apply all alias mappings globally to ensure consistent aliasing throughout the query
            qb.ApplyGlobalAliasMappings();
            
            return qb;
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with the root and one joined table
    /// </summary>
    public class CompositeWhere<TRoot, TJoin> : CompositeWhereBase<TRoot>
    {
        private readonly Expression<Func<TRoot, TJoin, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot> baseJoin, Expression<Func<TRoot, TJoin, bool>> predicate)
            : base(baseJoin)
        {
            _predicate = predicate;
            RegisterExpressionParameter(predicate);
            
            // Register all types with their aliases immediately - use RegisterType to ensure registration
            var rootAlias = Aliases.RegisterType(typeof(TRoot));
            var joinAlias = Aliases.RegisterType(typeof(TJoin));
            
            // Register parameters by matching parameter type with entity type
            foreach (var param in predicate.Parameters)
            {
                if (param.Type == typeof(TRoot))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TRoot), rootAlias);
                }
                else if (param.Type == typeof(TJoin))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TJoin), joinAlias);
                }
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);
            
            // Make sure all type-to-alias mappings are registered with the QueryBuilder
            ResolveTypeAliases(qb);
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause using AliasRegistry for correct alias mapping
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where);
            
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            // Apply all alias mappings globally to ensure consistent aliasing throughout the query
            qb.ApplyGlobalAliasMappings();
            
            return qb;
        }
        
        // Override to return all expected types for this query
        protected override Type[] GetExpectedTypes()
        {
            return new Type[] { typeof(TRoot), typeof(TJoin) };
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with the root and two joined tables
    /// </summary>
    public class CompositeWhere<TRoot, TJoin1, TJoin2> : CompositeWhereBase<TRoot>
    {
        private readonly Expression<Func<TRoot, TJoin1, TJoin2, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot> baseJoin, Expression<Func<TRoot, TJoin1, TJoin2, bool>> predicate)
            : base(baseJoin)
        {
            _predicate = predicate;
            RegisterExpressionParameter(predicate);
            
            // Register all types with their aliases immediately - use RegisterType to ensure registration
            var rootAlias = Aliases.RegisterType(typeof(TRoot));
            var join1Alias = Aliases.RegisterType(typeof(TJoin1));
            var join2Alias = Aliases.RegisterType(typeof(TJoin2));
            
            // Register parameters by matching parameter type with entity type
            foreach (var param in predicate.Parameters)
            {
                if (param.Type == typeof(TRoot))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TRoot), rootAlias);
                }
                else if (param.Type == typeof(TJoin1))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TJoin1), join1Alias);
                }
                else if (param.Type == typeof(TJoin2))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TJoin2), join2Alias);
                }
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);
            
            // Make sure all type-to-alias mappings are registered with the QueryBuilder
            ResolveTypeAliases(qb);
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause using AliasRegistry for correct alias mapping
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where);
            
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            // Apply all alias mappings globally to ensure consistent aliasing throughout the query
            qb.ApplyGlobalAliasMappings();
            
            return qb;
        }
        
        // Override to return all expected types for this query
        protected override Type[] GetExpectedTypes()
        {
            return new Type[] { typeof(TRoot), typeof(TJoin1), typeof(TJoin2) };
        }
    }
    
    /// <summary>
    /// Represents a WHERE clause in a composite query with the root and three joined tables
    /// </summary>
    public class CompositeWhere<TRoot, TJoin1, TJoin2, TJoin3> : CompositeWhereBase<TRoot>
    {
        private readonly Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, bool>> _predicate;
        
        internal CompositeWhere(CompositeJoinBase<TRoot> baseJoin, Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, bool>> predicate)
            : base(baseJoin)
        {
            _predicate = predicate;
            RegisterExpressionParameter(predicate);
            
            // Register all types with their aliases immediately - use RegisterType to ensure registration
            var rootAlias = Aliases.RegisterType(typeof(TRoot));
            var join1Alias = Aliases.RegisterType(typeof(TJoin1));
            var join2Alias = Aliases.RegisterType(typeof(TJoin2));
            var join3Alias = Aliases.RegisterType(typeof(TJoin3));
            
            // Register parameters by matching parameter type with entity type
            foreach (var param in predicate.Parameters)
            {
                if (param.Type == typeof(TRoot))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TRoot), rootAlias);
                }
                else if (param.Type == typeof(TJoin1))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TJoin1), join1Alias);
                }
                else if (param.Type == typeof(TJoin2))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TJoin2), join2Alias);
                }
                else if (param.Type == typeof(TJoin3))
                {
                    Aliases.RegisterParameterAlias(param.Name, typeof(TJoin3), join3Alias);
                }
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);
            
            // Make sure all type-to-alias mappings are registered with the QueryBuilder
            ResolveTypeAliases(qb);
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Build WHERE clause using AliasRegistry for correct alias mapping
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where);
            
            whereExpressionBuilder.BuildExpression(_predicate.Body, ExpressionBuilder.Clause.And);
            
            // Apply all alias mappings globally to ensure consistent aliasing throughout the query
            qb.ApplyGlobalAliasMappings();
            
            return qb;
        }

        // Override to return all expected types for this query
        protected override Type[] GetExpectedTypes()
        {
            return new Type[] { typeof(TRoot), typeof(TJoin1), typeof(TJoin2), typeof(TJoin3) };
        }
    }
} 