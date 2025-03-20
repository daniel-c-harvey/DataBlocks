namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents a SQL query with TOP/LIMIT clause
    /// </summary>
    public class Top<T, R> : Select<T, R>
    {
        private readonly int _take;

        internal Top(Expression<Func<T, R>> select, int take, Table table, ISqlDialect dialect)
            : base(select, take, table, dialect)
        {
            _take = take;
        }

        public Where<T, R> Where(Expression<Func<T, bool>> predicate)
        {
            return new Where<T, R>(this, predicate);
        }
    }
} 