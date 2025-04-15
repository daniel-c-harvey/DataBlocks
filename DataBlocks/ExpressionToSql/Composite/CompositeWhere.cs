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
            CopyEntityTypesFrom(baseJoin);
        }

        // Centralized method to resolve type-to-alias mappings
        protected Dictionary<Type, string> ResolveTypeAliases(QueryBuilder qb)
        {
            var typeAliases = new Dictionary<Type, string>();

            // First gather all entity type mappings from the EntityTypes dictionary
            foreach (var kvp in EntityTypes)
            {
                // Only record the first alias for each type if there are multiple
                if (!typeAliases.ContainsKey(kvp.Value))
                {
                    typeAliases.Add(kvp.Value, kvp.Key);
                }
            }

            // Check if any aliases are mapped to another alias (e.g., subquery alias)
            foreach (var type in typeAliases.Keys.ToList())
            {
                string alias = typeAliases[type];
                string effectiveAlias = qb.GetEffectiveAlias(alias);
                
                // Update with effective alias if different
                if (effectiveAlias != alias)
                {
                    typeAliases[type] = effectiveAlias;
                }
            }

            // IMPROVEMENT: Check for missing types that might be needed for this query
            // If we have generic type parameters that aren't registered, try to find them in the QueryBuilder
            // This handles cases where types like PersonnelContact aren't being properly tracked
            Type[] expectedTypes = GetExpectedTypes();
            foreach (var type in expectedTypes)
            {
                if (!typeAliases.ContainsKey(type))
                {
                    // First try getting alias from the QueryBuilder
                    string alias = qb.GetAliasForType(type);
                    if (!string.IsNullOrEmpty(alias))
                    {
                        typeAliases[type] = alias;
                        
                        // Also register it in our EntityTypes dictionary for future use
                        RegisterEntityType(alias, type);
                        continue;
                    }
                    
                    // If not in QueryBuilder, look for the type in the base query's EntityTypes
                    if (_baseJoin != null)
                    {
                        foreach (var kvp in _baseJoin.EntityTypes)
                        {
                            if (kvp.Value == type)
                            {
                                alias = kvp.Key;
                                typeAliases[type] = alias;
                                RegisterEntityType(alias, type);
                                break;
                            }
                        }
                    }
                    
                    // If still not found, create a new alias as a last resort
                    if (!typeAliases.ContainsKey(type))
                    {
                        alias = qb.GetOrCreateAliasForType(type);
                        typeAliases[type] = alias;
                        RegisterEntityType(alias, type);
                    }
                }
            }

            return typeAliases;
        }

        // Get the types that should be expected in this query based on the generic parameters
        protected virtual Type[] GetExpectedTypes()
        {
            // Base implementation just returns root type
            return new Type[] { typeof(TRoot) };
        }

        protected void RegisterResolvedAliases(QueryBuilder qb, Dictionary<Type, string> typeAliases)
        {
            // Register all resolved aliases back to the query builder
            foreach (var kvp in typeAliases)
            {
                qb.RegisterTableAliasForType(kvp.Key, kvp.Value);
            }
        }

        protected string GetRequiredAlias(Dictionary<Type, string> typeAliases, Type type, string typeName)
        {
            return typeAliases.TryGetValue(type, out var alias)
                ? alias
                : throw new InvalidOperationException($"No alias mapping found for {typeName} {type.Name}");
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);

            // Resolve all type-to-alias mappings
            var typeAliases = ResolveTypeAliases(qb);
            
            // Get the alias for the root type
            string rootAlias = GetRequiredAlias(typeAliases, typeof(TRoot), "root type");
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Register the alias for root type
            RegisterResolvedAliases(qb, typeAliases);
            
            // Build WHERE clause with the correct alias
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .WithRootAlias(rootAlias);
            
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);
            
            // Resolve all type-to-alias mappings
            var typeAliases = ResolveTypeAliases(qb);
            
            // Get aliases for all needed types
            string rootAlias = GetRequiredAlias(typeAliases, typeof(TRoot), "root type");
            string joinAlias = GetRequiredAlias(typeAliases, typeof(TJoin), "join type");
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Register the resolved aliases
            RegisterResolvedAliases(qb, typeAliases);
            
            // Build WHERE clause with the correct alias
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .WithRootAlias(rootAlias);
            
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);
            
            // Resolve all type-to-alias mappings
            var typeAliases = ResolveTypeAliases(qb);
            
            // Get aliases for all needed types
            string rootAlias = GetRequiredAlias(typeAliases, typeof(TRoot), "root type");
            string join1Alias = GetRequiredAlias(typeAliases, typeof(TJoin1), "join type");
            string join2Alias = GetRequiredAlias(typeAliases, typeof(TJoin2), "join type");
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Register the resolved aliases
            RegisterResolvedAliases(qb, typeAliases);
            
            // Build WHERE clause with the correct alias
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .WithRootAlias(rootAlias);
            
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // Build the base query first
            _baseJoin.ToSql(qb);
            
            // Resolve all type-to-alias mappings
            var typeAliases = ResolveTypeAliases(qb);
            
            // Get aliases for all needed types
            string rootAlias = GetRequiredAlias(typeAliases, typeof(TRoot), "root type");
            string join1Alias = GetRequiredAlias(typeAliases, typeof(TJoin1), "join type");
            string join2Alias = GetRequiredAlias(typeAliases, typeof(TJoin2), "join type");
            string join3Alias = GetRequiredAlias(typeAliases, typeof(TJoin3), "join type");
            
            // Reset condition state for WHERE clause
            qb.ResetConditionState();
            
            // Register the resolved aliases
            RegisterResolvedAliases(qb, typeAliases);
            
            // Build WHERE clause with the correct alias
            var whereExpressionBuilder = new ExpressionBuilder(this, qb)
                .WithClauseType(ClauseType.Where)
                .WithRootAlias(rootAlias);
            
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