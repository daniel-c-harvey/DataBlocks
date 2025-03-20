namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents a SQL query with LIMIT clause for database paging
    /// </summary>
    public class Limit<T, R> : Query
    {
        private readonly int _limit;
        private readonly Select<T, R> _select;
        private readonly Where<T, R> _where;
        private readonly Offset<T, R> _offset;

        internal Limit(Expression<Func<T, R>> select, int limit, Table table, ISqlDialect dialect)
            : base(dialect)
        {
            _limit = limit;
            _select = new Select<T, R>(select, limit, table, dialect);
        }
        
        internal Limit(Select<T, R> select, int limit)
            : base(select.Dialect)
        {
            _limit = limit;
            _select = select;
        }

        internal Limit(Where<T, R> where, int limit)
            : base(where.Dialect)
        {
            _limit = limit;
            _where = where;
        }

        internal Limit(Offset<T, R> offset, int limit)
            : base(offset.Dialect)
        {
            _limit = limit;
            _offset = offset;
        }

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
            if (_offset != null)
            {
                // Use base query but apply limit and offset in one operation
                if (_offset._where != null)
                {
                    _offset._where.ToSql(qb);
                    qb.LimitOffset(_limit, _offset._offset);
                    return qb;
                }
                else if (_offset._select != null)
                {
                    _offset._select.ToSql(qb);
                    qb.LimitOffset(_limit, _offset._offset);
                    return qb;
                }
            }
            
            if (_where != null)
            {
                _where.ToSql(qb);
                qb.Take(_limit);
                return qb;
            }
            
            if (_select != null)
            {
                return _select.ToSql(qb);
            }
            
            throw new InvalidOperationException("Query is in an invalid state");
        }
    }
} 