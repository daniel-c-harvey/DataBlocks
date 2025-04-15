using System;
using System.Linq.Expressions;
using System.Text;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    public abstract class CompositeSelectBase : Query
    {
        protected CompositeSelectBase(ISqlDialect dialect) : base(dialect) { }
    }
    
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain
    /// </summary>
    public class CompositeSelect<TRoot, TResult> : CompositeSelectBase
    {
        private readonly Expression<Func<TRoot, TResult>> _selector;
        private readonly Query _baseQuery;
        
        internal CompositeSelect(Query baseQuery, Expression<Func<TRoot, TResult>> selector)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _selector = selector;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
            
            // Register the selector parameters for tracking
            RegisterExpressionParameter(selector);
            
            // Explicitly register all types used in this query
            EnsureTypeRegistrations();
        }
        
        private void EnsureTypeRegistrations()
        {
            // Make sure we have a registration for the root type
            RegisterIfMissing(typeof(TRoot));
        }
        
        private void RegisterIfMissing(Type type)
        {
            // Check if the type is already registered with an alias
            bool found = false;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    found = true;
                    break;
                }
            }
            
            // If not found, register with a default alias
            if (!found)
            {
                // Use the type name as the basis for an alias
                string alias = type.Name.ToLowerInvariant()[0].ToString();
                
                // Make sure this alias doesn't clash with an existing one
                int suffix = 1;
                string candidateAlias = alias;
                while (EntityTypes.ContainsKey(candidateAlias))
                {
                    candidateAlias = $"{alias}{suffix++}";
                }
                
                // Register the type with this alias
                RegisterEntityType(candidateAlias, type);
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First register all expected type-to-alias mappings
            RegisterExpectedTypes(qb);
            
            // Prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            CopyParametersFromType(_baseQuery);
            
            // Ensure all entity types from the base query are also registered in our QueryBuilder
            foreach (var kvp in _baseQuery.EntityTypes)
            {
                RegisterEntityType(kvp.Key, kvp.Value);
                qb.RegisterTableAliasForType(kvp.Value, kvp.Key);
            }
            
            // Add the base query to the main builder
            qb.Append(tempBuilder.ToString());
            
            // Now prepend the SELECT statement to the final query builder
            CompositeExpressionUtils.PrependSelectExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb);
            
            return qb;
        }
        
        private void RegisterExpectedTypes(QueryBuilder qb)
        {
            // Register all expected types with aliases
            RegisterTypeWithAlias(qb, typeof(TRoot));
        }
        
        private void RegisterTypeWithAlias(QueryBuilder qb, Type type)
        {
            // First check if we already have an alias for this type in our EntityTypes
            string alias = null;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    alias = pair.Key;
                    break;
                }
            }
            
            // If we found an alias, register it with the QueryBuilder
            if (!string.IsNullOrEmpty(alias))
            {
                qb.RegisterTableAliasForType(type, alias);
            }
            else
            {
                // Check if QueryBuilder already has an alias for this type
                alias = qb.GetAliasForType(type);
                
                if (!string.IsNullOrEmpty(alias))
                {
                    // Register it in our EntityTypes
                    RegisterEntityType(alias, type);
                }
                else
                {
                    // Create a new alias
                    alias = qb.GetOrCreateAliasForType(type);
                    RegisterEntityType(alias, type);
                }
            }
        }
    }
    
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain with one joined table
    /// </summary>
    public class CompositeSelect<TRoot, TJoin, TResult> : CompositeSelectBase
    {
        private readonly Expression<Func<TRoot, TJoin, TResult>> _selector;
        private readonly Query _baseQuery;
        
        internal CompositeSelect(Query baseQuery, Expression<Func<TRoot, TJoin, TResult>> selector)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _selector = selector;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
            
            // Register the selector parameters for tracking
            RegisterExpressionParameter(selector);
            
            // Explicitly register all types used in this query
            EnsureTypeRegistrations();
        }
        
        private void EnsureTypeRegistrations()
        {
            // Make sure we have a registration for each generic type parameter
            RegisterIfMissing(typeof(TRoot));
            RegisterIfMissing(typeof(TJoin));
        }
        
        private void RegisterIfMissing(Type type)
        {
            // Check if the type is already registered with an alias
            bool found = false;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    found = true;
                    break;
                }
            }
            
            // If not found, register with a default alias
            if (!found)
            {
                // Use the type name as the basis for an alias
                string alias = type.Name.ToLowerInvariant()[0].ToString();
                
                // Make sure this alias doesn't clash with an existing one
                int suffix = 1;
                string candidateAlias = alias;
                while (EntityTypes.ContainsKey(candidateAlias))
                {
                    candidateAlias = $"{alias}{suffix++}";
                }
                
                // Register the type with this alias
                RegisterEntityType(candidateAlias, type);
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First register all expected type-to-alias mappings
            RegisterExpectedTypes(qb);
            
            // Prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            CopyParametersFromType(_baseQuery);
            
            // Ensure all entity types from the base query are also registered in our QueryBuilder
            foreach (var kvp in _baseQuery.EntityTypes)
            {
                RegisterEntityType(kvp.Key, kvp.Value);
                qb.RegisterTableAliasForType(kvp.Value, kvp.Key);
            }
            
            // Add the base query to the main builder
            qb.Append(tempBuilder.ToString());
            
            // Now prepend the SELECT statement to the final query builder
            CompositeExpressionUtils.PrependSelectExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb,
                typeof(TJoin)); // Pass the joined table type
            
            return qb;
        }
        
        private void RegisterExpectedTypes(QueryBuilder qb)
        {
            // Register all expected types with aliases
            RegisterTypeWithAlias(qb, typeof(TRoot));
            RegisterTypeWithAlias(qb, typeof(TJoin));
        }
        
        private void RegisterTypeWithAlias(QueryBuilder qb, Type type)
        {
            // First check if we already have an alias for this type in our EntityTypes
            string alias = null;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    alias = pair.Key;
                    break;
                }
            }
            
            // If we found an alias, register it with the QueryBuilder
            if (!string.IsNullOrEmpty(alias))
            {
                qb.RegisterTableAliasForType(type, alias);
            }
            else
            {
                // Check if QueryBuilder already has an alias for this type
                alias = qb.GetAliasForType(type);
                
                if (!string.IsNullOrEmpty(alias))
                {
                    // Register it in our EntityTypes
                    RegisterEntityType(alias, type);
                }
                else
                {
                    // Create a new alias
                    alias = qb.GetOrCreateAliasForType(type);
                    RegisterEntityType(alias, type);
                }
            }
        }
    }
    
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain with two joined tables
    /// </summary>
    public class CompositeSelect<TRoot, TJoin1, TJoin2, TResult> : CompositeSelectBase
    {
        private readonly Expression<Func<TRoot, TJoin1, TJoin2, TResult>> _selector;
        private readonly Query _baseQuery;
        
        internal CompositeSelect(Query baseQuery, Expression<Func<TRoot, TJoin1, TJoin2, TResult>> selector)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _selector = selector;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
            
            // Register the selector parameters for tracking
            RegisterExpressionParameter(selector);
            
            // Explicitly register all types used in this query to ensure alias consistency
            EnsureTypeRegistrations();
        }
        
        private void EnsureTypeRegistrations()
        {
            // Make sure we have a registration for each generic type parameter
            RegisterIfMissing(typeof(TRoot));
            RegisterIfMissing(typeof(TJoin1));
            RegisterIfMissing(typeof(TJoin2));
        }
        
        private void RegisterIfMissing(Type type)
        {
            // Check if the type is already registered with an alias
            bool found = false;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    found = true;
                    break;
                }
            }
            
            // If not found, register with a default alias
            if (!found)
            {
                // Use the type name as the basis for an alias
                string alias = type.Name.ToLowerInvariant()[0].ToString();
                
                // Make sure this alias doesn't clash with an existing one
                int suffix = 1;
                string candidateAlias = alias;
                while (EntityTypes.ContainsKey(candidateAlias))
                {
                    candidateAlias = $"{alias}{suffix++}";
                }
                
                // Register the type with this alias
                RegisterEntityType(candidateAlias, type);
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First register all expected type-to-alias mappings before doing anything else
            RegisterExpectedTypes(qb);
            
            // Now prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            CopyParametersFromType(_baseQuery);
            
            // Ensure all entity types from the base query are also registered in our QueryBuilder
            foreach (var kvp in _baseQuery.EntityTypes)
            {
                RegisterEntityType(kvp.Key, kvp.Value);
                qb.RegisterTableAliasForType(kvp.Value, kvp.Key);
            }
            
            // Add the base query to the main builder
            qb.Append(tempBuilder.ToString());
            
            // Now prepend the SELECT statement to the final query builder
            CompositeExpressionUtils.PrependSelectExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb,
                typeof(TJoin1), typeof(TJoin2)); // Pass both joined table types
            
            return qb;
        }
        
        private void RegisterExpectedTypes(QueryBuilder qb)
        {
            // Register all expected types with aliases
            RegisterTypeWithAlias(qb, typeof(TRoot));
            RegisterTypeWithAlias(qb, typeof(TJoin1));
            RegisterTypeWithAlias(qb, typeof(TJoin2));
        }
        
        private void RegisterTypeWithAlias(QueryBuilder qb, Type type)
        {
            // First check if we already have an alias for this type in our EntityTypes
            string alias = null;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    alias = pair.Key;
                    break;
                }
            }
            
            // If we found an alias, register it with the QueryBuilder
            if (!string.IsNullOrEmpty(alias))
            {
                qb.RegisterTableAliasForType(type, alias);
            }
            else
            {
                // Check if QueryBuilder already has an alias for this type
                alias = qb.GetAliasForType(type);
                
                if (!string.IsNullOrEmpty(alias))
                {
                    // Register it in our EntityTypes
                    RegisterEntityType(alias, type);
                }
                else
                {
                    // Create a new alias
                    alias = qb.GetOrCreateAliasForType(type);
                    RegisterEntityType(alias, type);
                }
            }
        }
    }

    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain with three joined tables
    /// </summary>
    public class CompositeSelect<TRoot, TJoin1, TJoin2, TJoin3, TResult> : CompositeSelectBase
    {
        private readonly Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, TResult>> _selector;
        private readonly Query _baseQuery;
        
        internal CompositeSelect(Query baseQuery, Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, TResult>> selector)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _selector = selector;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
            
            // Register the selector parameters for tracking
            RegisterExpressionParameter(selector);
            
            // Explicitly register all types used in this query to ensure alias consistency
            EnsureTypeRegistrations();
        }
        
        private void EnsureTypeRegistrations()
        {
            // Make sure we have a registration for each generic type parameter
            RegisterIfMissing(typeof(TRoot));
            RegisterIfMissing(typeof(TJoin1));
            RegisterIfMissing(typeof(TJoin2));
            RegisterIfMissing(typeof(TJoin3));
        }
        
        private void RegisterIfMissing(Type type)
        {
            // Check if the type is already registered with an alias
            bool found = false;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    found = true;
                    break;
                }
            }
            
            // If not found, register with a default alias
            if (!found)
            {
                // Use the type name as the basis for an alias
                string alias = type.Name.ToLowerInvariant()[0].ToString();
                
                // Make sure this alias doesn't clash with an existing one
                int suffix = 1;
                string candidateAlias = alias;
                while (EntityTypes.ContainsKey(candidateAlias))
                {
                    candidateAlias = $"{alias}{suffix++}";
                }
                
                // Register the type with this alias
                RegisterEntityType(candidateAlias, type);
            }
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First register all expected type-to-alias mappings before doing anything else
            RegisterExpectedTypes(qb);
            
            // Now prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            CopyParametersFromType(_baseQuery);
            
            // Ensure all entity types from the base query are also registered in our QueryBuilder
            foreach (var kvp in _baseQuery.EntityTypes)
            {
                RegisterEntityType(kvp.Key, kvp.Value);
                qb.RegisterTableAliasForType(kvp.Value, kvp.Key);
            }
            
            // Add the base query to the main builder
            qb.Append(tempBuilder.ToString());
            
            // Now prepend the SELECT statement to the final query builder
            CompositeExpressionUtils.PrependSelectExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb,
                typeof(TJoin1), typeof(TJoin2), typeof(TJoin3)); // Pass all joined table types
            
            return qb;
        }
        
        private void RegisterExpectedTypes(QueryBuilder qb)
        {
            // Register all expected types with aliases
            RegisterTypeWithAlias(qb, typeof(TRoot));
            RegisterTypeWithAlias(qb, typeof(TJoin1));
            RegisterTypeWithAlias(qb, typeof(TJoin2));
            RegisterTypeWithAlias(qb, typeof(TJoin3));
        }
        
        private void RegisterTypeWithAlias(QueryBuilder qb, Type type)
        {
            // First check if we already have an alias for this type in our EntityTypes
            string alias = null;
            foreach (var pair in EntityTypes)
            {
                if (pair.Value == type)
                {
                    alias = pair.Key;
                    break;
                }
            }
            
            // If we found an alias, register it with the QueryBuilder
            if (!string.IsNullOrEmpty(alias))
            {
                qb.RegisterTableAliasForType(type, alias);
            }
            else
            {
                // Check if QueryBuilder already has an alias for this type
                alias = qb.GetAliasForType(type);
                
                if (!string.IsNullOrEmpty(alias))
                {
                    // Register it in our EntityTypes
                    RegisterEntityType(alias, type);
                }
                else
                {
                    // Create a new alias
                    alias = qb.GetOrCreateAliasForType(type);
                    RegisterEntityType(alias, type);
                }
            }
        }
    }
    
    /// <summary>
    /// Extension methods for adding SELECT to the end of a query chain
    /// </summary>
    public static class CompositeSelectExtensions
    {
        /// <summary>
        /// Adds a SELECT clause to a CompositeFrom query
        /// </summary>
        public static CompositeSelect<TRoot, TResult> Select<TRoot, TResult>(
            this CompositeFrom<TRoot> from,
            Expression<Func<TRoot, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TResult>(from, selector);
        }
        
        /// <summary>
        /// Adds a SELECT clause to a CompositeJoin query with one joined table
        /// </summary>
        public static CompositeSelect<TRoot, TJoin, TResult> Select<TRoot, TJoin, TResult>(
            this CompositeJoin<TRoot, TJoin> join,
            Expression<Func<TRoot, TJoin, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TJoin, TResult>(join, selector);
        }
        
        /// <summary>
        /// Adds a SELECT clause to a CompositeJoin query with two joined tables
        /// </summary>
        public static CompositeSelect<TRoot, TJoin1, TJoin2, TResult> Select<TRoot, TJoin1, TJoin2, TResult>(
            this CompositeJoin<TRoot, TJoin1, TJoin2> join,
            Expression<Func<TRoot, TJoin1, TJoin2, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TJoin1, TJoin2, TResult>(join, selector);
        }

        /// <summary>
        /// Adds a SELECT clause to a CompositeJoin query with three joined tables
        /// </summary>
        public static CompositeSelect<TRoot, TJoin1, TJoin2, TJoin3, TResult> Select<TRoot, TJoin1, TJoin2, TJoin3, TResult>(
            this CompositeJoin<TRoot, TJoin1, TJoin2, TJoin3> join,
            Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TJoin1, TJoin2, TJoin3, TResult>(join, selector);
        }
        
        /// <summary>
        /// Adds a SELECT clause to a CompositeWhere query with root table only
        /// </summary>
        public static CompositeSelect<TRoot, TResult> Select<TRoot, TResult>(
            this CompositeWhere<TRoot> where,
            Expression<Func<TRoot, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TResult>(where, selector);
        }
        
        /// <summary>
        /// Adds a SELECT clause to a CompositeWhere query with one joined table
        /// </summary>
        public static CompositeSelect<TRoot, TJoin, TResult> Select<TRoot, TJoin, TResult>(
            this CompositeWhere<TRoot, TJoin> where,
            Expression<Func<TRoot, TJoin, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TJoin, TResult>(where, selector);
        }
        
        /// <summary>
        /// Adds a SELECT clause to a CompositeWhere query with two joined tables
        /// </summary>
        public static CompositeSelect<TRoot, TJoin1, TJoin2, TResult> Select<TRoot, TJoin1, TJoin2, TResult>(
            this CompositeWhere<TRoot, TJoin1, TJoin2> where,
            Expression<Func<TRoot, TJoin1, TJoin2, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TJoin1, TJoin2, TResult>(where, selector);
        }

        /// <summary>
        /// Adds a SELECT clause to a CompositeWhere query with three joined tables
        /// </summary>
        public static CompositeSelect<TRoot, TJoin1, TJoin2, TJoin3, TResult> Select<TRoot, TJoin1, TJoin2, TJoin3, TResult>(
            this CompositeWhere<TRoot, TJoin1, TJoin2, TJoin3> where,
            Expression<Func<TRoot, TJoin1, TJoin2, TJoin3, TResult>> selector)
        {
            return new CompositeSelect<TRoot, TJoin1, TJoin2, TJoin3, TResult>(where, selector);
        }
    }
}