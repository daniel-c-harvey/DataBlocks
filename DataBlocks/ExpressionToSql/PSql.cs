namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Extensions for PostgreSQL specific operations
    /// </summary>
    public static class PSql
    {
        /// <summary>
        /// Creates a new PostgreSQL select query
        /// </summary>
        public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, string tableName = null)
        {
            return Sql.Create(selector, null, tableName, new PostgreSqlDialect());
        }

        /// <summary>
        /// Creates a new PostgreSQL select query with LIMIT clause
        /// </summary>
        public static Limit<T, R> Limit<T, R>(Expression<Func<T, R>> selector, int take, string tableName = null)
        {
            return new Limit<T, R>(selector, take, new Table<T> { Name = tableName }, new PostgreSqlDialect());
        }
    }
} 