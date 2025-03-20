namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents a SQL query with paging (LIMIT and OFFSET) clauses
    /// </summary>
    public class Page<T, R> : Query
    {
        private readonly int _pageSize;
        private readonly int _pageNumber;
        private readonly int _offset;
        private readonly Select<T, R> _select;
        private readonly Where<T, R> _where;
        private readonly Offset<T, R> _offset_query;

        /// <summary>
        /// Creates a paged query with the specified page size and page number from a select query
        /// </summary>
        internal Page(Select<T, R> select, int pageNumber, int pageSize)
            : base(select.Dialect)
        {
            if (pageNumber < 1)
                pageNumber = 1;
            
            if (pageSize < 1)
                pageSize = 10;
                
            _pageSize = pageSize;
            _pageNumber = pageNumber;
            _offset = (pageNumber - 1) * pageSize;
            _select = select;
        }

        /// <summary>
        /// Creates a paged query with the specified page size and page number from a where query
        /// </summary>
        internal Page(Where<T, R> where, int pageNumber, int pageSize)
            : base(where.Dialect)
        {
            if (pageNumber < 1)
                pageNumber = 1;
            
            if (pageSize < 1)
                pageSize = 10;
                
            _pageSize = pageSize;
            _pageNumber = pageNumber;
            _offset = (pageNumber - 1) * pageSize;
            _where = where;
        }

        /// <summary>
        /// Creates a paged query with the specified page size and page number from an offset query
        /// </summary>
        internal Page(Offset<T, R> offset, int pageSize)
            : base(offset.Dialect)
        {
            _pageSize = pageSize;
            _offset_query = offset;
        }

        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        /// <param name="predicate">The where predicate</param>
        /// <returns>A Where query object</returns>
        public Where<T, R> Where(Expression<Func<T, bool>> predicate)
        {
            if (_select != null)
            {
                return _select.Where(predicate);
            }
            
            throw new InvalidOperationException("Cannot apply WHERE clause at this point in the query chain");
        }

        /// <summary>
        /// Adds an OFFSET clause to the query for paging support
        /// </summary>
        /// <param name="offset">The number of rows to skip</param>
        /// <returns>An Offset query object</returns>
        public Offset<T, R> Offset(int offset)
        {
            if (_select != null)
            {
                return new Offset<T, R>(_select, offset);
            }
            
            throw new InvalidOperationException("Cannot apply OFFSET clause at this point in the query chain");
        }

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            if (_offset_query != null)
            {
                _offset_query.ToSql(qb);
                qb.Take(_pageSize);
                return qb;
            }
            
            if (_where != null)
            {
                _where.ToSql(qb);
                qb.LimitOffset(_pageSize, _offset);
                return qb;
            }
            
            if (_select != null)
            {
                _select.ToSql(qb);
                qb.LimitOffset(_pageSize, _offset);
                return qb;
            }
            
            throw new InvalidOperationException("Query is in an invalid state");
        }
    }
}