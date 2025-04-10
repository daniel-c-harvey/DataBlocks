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
        /// Creates a new composite From query for PostgreSQL using a schema
        /// </summary>
        public static CompositeFrom<TRoot> From<TRoot>(DataSchema schema)
        {
            var table = new Table<TRoot> { Name = schema.CollectionName, Schema = schema.SchemaName };
            return new CompositeFrom<TRoot>(table, new PostgreSqlDialect());
        }
        
        /// <summary>
        /// Creates a new composite From query for PostgreSQL using a custom table
        /// </summary>
        public static CompositeFrom<TRoot> From<TRoot>(Table table)
        {
            return new CompositeFrom<TRoot>(table, new PostgreSqlDialect());
        }
        
        /// <summary>
        /// Creates a new composite From query for PostgreSQL using a table name
        /// </summary>
        public static CompositeFrom<TRoot> From<TRoot>(string tableName = null)
        {
            var table = new Table<TRoot> { Name = tableName ?? typeof(TRoot).Name };
            return new CompositeFrom<TRoot>(table, new PostgreSqlDialect());
        }
    }
}