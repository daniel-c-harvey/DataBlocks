namespace ExpressionToSql
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Extensions for MS SQL Server specific operations
    /// </summary>
    public static class TSql
    {
        /// <summary>
        /// Creates a new MS SQL Server select query
        /// </summary>
        public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, string tableName = null)
        {
            return Sql.Create(selector, null, tableName, new TSqlDialect());
        }

        /// <summary>
        /// Creates a new MS SQL Server select query with TOP clause
        /// </summary>
        public static Top<T, R> Top<T, R>(Expression<Func<T, R>> selector, int take, string tableName = null)
        {
            return new Top<T, R>(selector, take, new Table<T> { Name = tableName }, new TSqlDialect());
        }
    }
} 