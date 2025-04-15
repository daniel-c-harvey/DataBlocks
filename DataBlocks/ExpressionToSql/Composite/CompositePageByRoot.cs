using System;
using System.Linq.Expressions;
using System.Text;
using DataBlocks.DataAccess;
using ExpressionToSql.Utils;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Base class for PageByRoot implementations that apply paging to the root table as a subquery
    /// </summary>
    public abstract class CompositePageByRootBase<TRoot> : QueryRoot<TRoot>
    {
        private readonly int _pageSize;
        private readonly int _pageIndex;
        private readonly int _offset;
        private readonly QueryRoot<TRoot> _baseQuery;
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
            
            // Register the root type with our subquery alias
            RegisterEntityType(_subqueryAlias, typeof(TRoot));
        }
        
        /// <summary>
        /// Gets the alias used for the subquery
        /// </summary>
        public string SubqueryAlias => _subqueryAlias;
        
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
                
                // Get the current SQL text
                string currentSql = subquerySb.ToString().TrimStart();
                
                // If there's no SELECT, add one with all columns from the root table
                if (!currentSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    // Get all columns from the root table
                    var rootType = typeof(TRoot);
                    var columns = SqlTypeUtils.GetColumnNames(rootType);
                    
                    // Build a clean SELECT clause with proper syntax
                    var selectBuilder = new StringBuilder("SELECT ");
                    bool first = true;
                    foreach (var column in columns)
                    {
                        if (!first) selectBuilder.Append(", ");
                        selectBuilder.Append(QueryBuilder.TableAliasName).Append(".\"").Append(column).Append("\"");
                        first = false;
                    }
                    selectBuilder.Append(" ");
                    
                    // Replace or prepend the SELECT clause
                    if (currentSql.StartsWith("FROM", StringComparison.OrdinalIgnoreCase))
                    {
                        // Insert SELECT before FROM
                        subquerySb.Insert(0, selectBuilder.ToString());
                    }
                    else
                    {
                        // Replace the entire string with our new SELECT
                        subquerySb.Clear();
                        subquerySb.Append(selectBuilder);
                        _baseQuery.ToSql(subqueryQb); // Re-add everything after SELECT
                    }
                }
                
                // Apply the LIMIT and OFFSET to the subquery
                subqueryQb.LimitOffset(_pageSize, _offset);
                
                // Get the subquery SQL text
                string subquerySql = subquerySb.ToString();
                
                // Apply the subquery to the main query builder
                qb.AddSubquery(subquerySql, _subqueryAlias);
                
                // Update the alias mapping for the root type 
                // (the root table is now referenced via the subquery alias)
                qb.RegisterTableAliasForType(typeof(TRoot), _subqueryAlias);
                
                // Store the alias mapping from the old default alias to the subquery alias
                // This is critical for WHERE clauses to work properly with subqueries
                qb.StoreAliasMapping(QueryBuilder.TableAliasName, _subqueryAlias);
                
                // Also store any other alias that might be used for the root type
                foreach (var alias in EntityTypes.Where(et => et.Value == typeof(TRoot)).Select(et => et.Key))
                {
                    if (alias != _subqueryAlias && alias != QueryBuilder.TableAliasName)
                    {
                        qb.StoreAliasMapping(alias, _subqueryAlias);
                    }
                }
                
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