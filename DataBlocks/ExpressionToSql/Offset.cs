namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents a SQL query with OFFSET clause for paging support
    /// </summary>
    public class Offset<T, R> : Query
    {
        internal readonly int _offset;
        internal readonly Where<T, R> _where;
        internal readonly Select<T, R> _select;

        internal Offset(Where<T, R> where, int offset) 
            : base(where.Dialect)
        {
            _where = where;
            _offset = offset;
        }

        internal Offset(Select<T, R> select, int offset)
            : base(select.Dialect)
        {
            _select = select;
            _offset = offset;
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

        internal override QueryBuilder ToSql(QueryBuilder qb)
        {
            if (_where != null)
            {
                _where.ToSql(qb);
            }
            else if (_select != null)
            {
                _select.ToSql(qb);
            }

            qb.Offset(_offset);
            return qb;
        }
    }
} 