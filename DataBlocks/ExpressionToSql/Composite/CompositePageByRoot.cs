using System;
using System.Linq.Expressions;
using System.Text;
using DataBlocks.DataAccess;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Base class for PageByRoot implementations that apply paging to the root table as a subquery
    /// </summary>
    public abstract class CompositePageByRootBase<TRoot> : Query
    {
        private readonly int _pageSize;
        private readonly int _pageIndex;
        private readonly int _offset;
        private readonly CompositeFrom<TRoot> _baseQuery;
        private readonly string _subqueryAlias;
        
        /// <summary>
        /// Creates a paged query that paginates the root table first before applying joins
        /// </summary>
        internal CompositePageByRootBase(CompositeFrom<TRoot> baseQuery, int pageIndex, int pageSize)
            : base(baseQuery.Dialect)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));
            
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize));
                
            _pageSize = pageSize;
            _pageIndex = pageIndex;
            _offset = pageIndex * pageSize;
            _baseQuery = baseQuery;
            _subqueryAlias = "subq"; // Default subquery alias
            
            // Copy entity types from the base query to maintain context
            CopyEntityTypesFrom(baseQuery);
        }
        
        /// <summary>
        /// Adds a JOIN clause to the query with a custom table
        /// </summary>
        public CompositeJoin<TRoot, TJoin> Join<TJoin>(
            Table joinTable, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            // Create a new CompositeJoin with this as the base
            var baseJoin = new CompositeJoin<TRoot>(this);
            return baseJoin.Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a JOIN clause to the query with a schema-based table
        /// </summary>
        public CompositeJoin<TRoot, TJoin> Join<TJoin>(
            DataSchema schema, 
            Expression<Func<TRoot, TJoin, bool>> joinCondition, 
            JoinType joinType = JoinType.Inner)
        {
            var joinTable = new Table<TJoin> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return Join(joinTable, joinCondition, joinType);
        }
        
        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        public CompositeWhere<TRoot> Where(Expression<Func<TRoot, bool>> predicate)
        {
            var baseJoin = new CompositeJoin<TRoot>(this);
            return baseJoin.Where(predicate);
        }
        
        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            if (_baseQuery != null)
            {
                // Create a new StringBuilder and QueryBuilder for the subquery
                var subquerySb = new StringBuilder();
                var subqueryQb = new QueryBuilder(subquerySb, Dialect, _baseQuery);
                
                // Build the base query into the subquery builder
                _baseQuery.ToSql(subqueryQb);
                
                // Apply the LIMIT and OFFSET to the subquery
                subqueryQb.LimitOffset(_pageSize, _offset);
                
                // Get the subquery SQL text
                string subquerySql = subquerySb.ToString();
                
                // Apply the subquery to the main query builder
                qb.AddSubquery(subquerySql, _subqueryAlias);
                
                // Update the alias mapping for the root type 
                // (the root table is now referenced via the subquery alias)
                qb.RegisterTableAliasForType(typeof(TRoot), _subqueryAlias);
                RegisterEntityType(_subqueryAlias, typeof(TRoot));
                
                // Copy parameters from the base query
                CopyParametersFromType(_baseQuery);
                
                return qb;
            }
            
            throw new InvalidOperationException("Query is in an invalid state");
        }
    }
    
    /// <summary>
    /// Represents a SQL query that applies paging to the root table before performing joins
    /// </summary>
    public class CompositePageByRoot<TRoot> : CompositePageByRootBase<TRoot>
    {
        internal CompositePageByRoot(CompositeFrom<TRoot> baseQuery, int pageIndex, int pageSize) 
            : base(baseQuery, pageIndex, pageSize) { }
    }
    
    /// <summary>
    /// Extension methods for applying PageByRoot to composite queries
    /// </summary>
    public static class CompositePageByRootExtensions
    {
        /// <summary>
        /// Applies paging to the root table by creating a subquery
        /// </summary>
        public static CompositePageByRoot<TRoot> PageByRoot<TRoot>(this CompositeFrom<TRoot> query, int pageIndex, int pageSize)
        {
            return new CompositePageByRoot<TRoot>(query, pageIndex, pageSize);
        }
    }
} 