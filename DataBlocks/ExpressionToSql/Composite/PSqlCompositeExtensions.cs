using System;
using System.Linq.Expressions;
using DataBlocks.DataAccess;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Extension methods for PostgreSQL composite queries
    /// </summary>
    public static class PSqlC
    {
        /// <summary>
        /// Creates a new composite Select query for PostgreSQL
        /// </summary>
        public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
            Expression<Func<TRoot, TResult>> selector, 
            DataSchema schema)
        {
            var table = new Table<TRoot> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return new CompositeSelect<TRoot, TResult>(selector, table, new PostgreSqlDialect());
        }
        
        /// <summary>
        /// Creates a new composite Select query for PostgreSQL with a custom table
        /// </summary>
        public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
            Expression<Func<TRoot, TResult>> selector, 
            Table table)
        {
            return new CompositeSelect<TRoot, TResult>(selector, table, new PostgreSqlDialect());
        }
        
        /// <summary>
        /// Creates a new composite Select query for PostgreSQL with a table name
        /// </summary>
        public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
            Expression<Func<TRoot, TResult>> selector, 
            string tableName = null)
        {
            var table = new Table<TRoot> { Name = tableName ?? typeof(TRoot).Name };
            return new CompositeSelect<TRoot, TResult>(selector, table, new PostgreSqlDialect());
        }
    }
}