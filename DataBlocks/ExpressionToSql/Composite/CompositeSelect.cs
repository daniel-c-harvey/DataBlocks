using System;
using System.Linq.Expressions;
using System.Text;
using DataBlocks.DataAccess;
using DataBlocks.ExpressionToSql.Expressions;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain
    /// </summary>
    public class CompositeSelect<TRoot, TResult> : Query
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Add the base query to the main builder
            qb.Append(tempBuilder.ToString());
            
            // Now prepend the SELECT statement to the final query builder
            CompositeExpressionUtils.PrependSelectExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb);
            
            return qb;
        }
    }
    
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain with one joined table
    /// </summary>
    public class CompositeSelect<TRoot, TJoin, TResult> : Query
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            
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
    }
    
    /// <summary>
    /// A composite Select query that works with multiple table joins
    /// and comes at the end of a query chain with two joined tables
    /// </summary>
    public class CompositeSelect<TRoot, TJoin1, TJoin2, TResult> : Query
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
        }
        

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            CopyParametersFromType(_baseQuery);
            
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
    }

    /// <summary>
    /// A composite Select query that works with three joined tables
    /// </summary>
    public class CompositeSelect<TRoot, TJoin1, TJoin2, TJoin3, TResult> : Query
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
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First prepare a temporary buffer for the base query
            var tempBuilder = new StringBuilder();
            var tempQb = new QueryBuilder(tempBuilder, Dialect, _baseQuery);
            
            // Build the base query into the temporary buffer
            _baseQuery.ToSql(tempQb);
            
            // Apply entity types to ensure aliases are registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Add the base query to the main builder
            qb.Append(tempBuilder.ToString());
            
            // Now prepend the SELECT statement to the final query builder
            CompositeExpressionUtils.PrependSelectExpressions(
                CompositeExpressionUtils.GetExpressions(typeof(TResult), _selector.Body),
                typeof(TRoot),
                qb,
                typeof(TJoin1), typeof(TJoin2), typeof(TJoin3)); // Pass all three joined table types
            
            return qb;
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