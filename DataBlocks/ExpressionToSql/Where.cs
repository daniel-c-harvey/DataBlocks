namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;
    using System.Text;
    using DataBlocks.ExpressionToSql.Expressions;

    public class Where<T, R> : Query
    {
        private readonly Query _baseQuery;
        private readonly Expression<Func<T, bool>> _where;
        // private readonly ExpressionBuilder _expressionBuilder;
        // private readonly QueryBuilder _queryBuilder;

        internal Where(Query baseQuery, Expression<Func<T, bool>> where)
            : base(baseQuery.Dialect)
        {
            _baseQuery = baseQuery;
            _where = where;
            
            // Copy entity types from the base query
            CopyEntityTypesFrom(baseQuery);
            
            // Register the where expression parameter
            RegisterExpressionParameter(where);
            
            // _queryBuilder = new QueryBuilder(new StringBuilder(), baseQuery.Dialect, baseQuery);
            // _expressionBuilder = new ExpressionBuilder(this, _queryBuilder);
        }

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            // First build the base query
            _baseQuery.ToSql(qb);
            
            // Apply entity types to ensure aliases are properly registered
            ApplyEntityTypesToQueryBuilder(qb);
            
            // Then apply WHERE conditions - let ExpressionBuilder handle the WHERE keyword
            new ExpressionBuilder(this, qb).WithClauseType(ClauseType.Where).BuildExpression(_where.Body, ExpressionBuilder.Clause.And);
            
            return qb;
        }

        /// <summary>
        /// Adds an OFFSET clause to the query for paging support
        /// </summary>
        /// <param name="offset">The number of rows to skip</param>
        /// <returns>An Offset query object</returns>
        public Offset<T, R> Offset(int offset)
        {
            return new Offset<T, R>(this, offset);
        }

        /// <summary>
        /// Creates a paged query with the specified page size and page number
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>A Page query object with limit and offset applied</returns>
        public Page<T, R> Page(int pageNumber, int pageSize)
        {
            return new Page<T, R>(this, pageNumber, pageSize);
        }

        /// <summary>
        /// Adds a LIMIT clause to the query
        /// </summary>
        /// <param name="limit">The maximum number of rows to return</param>
        /// <returns>A Limit query object</returns>
        public Limit<T, R> Limit(int limit)
        {
            return new Limit<T, R>(this, limit);
        }
    }
}