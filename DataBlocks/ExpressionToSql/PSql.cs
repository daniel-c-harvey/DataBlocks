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
        /// Creates a new PostgreSQL select query with a Table object
        /// </summary>
        public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, Table table)
        {
            return Sql.Create(selector, null, table, new PostgreSqlDialect());
        }

        /// <summary>
        /// Creates a new PostgreSQL select query with LIMIT clause
        /// </summary>
        public static Limit<T, R> Limit<T, R>(Expression<Func<T, R>> selector, int take, string tableName = null)
        {
            return new Limit<T, R>(selector, take, new Table<T> { Name = tableName }, new PostgreSqlDialect());
        }

        /// <summary>
        /// Creates a new PostgreSQL select query with LIMIT clause and a Table object
        /// </summary>
        public static Limit<T, R> Limit<T, R>(Expression<Func<T, R>> selector, int take, Table table)
        {
            return new Limit<T, R>(selector, take, table, new PostgreSqlDialect());
        }
        
        // /// <summary>
        // /// Extension method to join with a DataSchema
        // /// </summary>
        // public static Join<T, T2, R> Join<T, T2, R>(this Select<T, R> select, DataSchema schema, Expression<Func<T, T2, bool>> joinCondition, JoinType joinType = JoinType.Inner)
        // {
        //     // Use the existing schema object
        //     var table = new Table<T2> { Name = schema.TableName, Schema = schema.SchemaName };
        //     return select.Join<T2>(table, joinCondition, joinType);
        // }
    }

    public static class QUtil
    {
        public static bool IsIn<T, R>(Expression<Func<T, R>> selector, string paramName)
        {
            // this method is used in the SQL Expression builder to provide a clean way of offering an agnostic IN clause to the DataAdapters
            // it should never actually be called, and is instead just used for metaprogramming
            throw new NotImplementedException();
        }
    }
} 