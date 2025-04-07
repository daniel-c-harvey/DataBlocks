// using System;
// using System.Linq.Expressions;
// using DataBlocks.DataAccess;
//
// namespace ExpressionToSql.Composite
// {
//     /// <summary>
//     /// Example of how PSql could be extended to include composite functionality directly
//     /// This is for demonstration purposes only - not for direct implementation
//     /// </summary>
//     public static class UpdatedPSql
//     {
//         // Original methods (from PSql)
//         
//         /// <summary>
//         /// Creates a new PostgreSQL select query
//         /// </summary>
//         public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, string tableName = null)
//         {
//             return Sql.Create(selector, null, tableName, new PostgreSqlDialect());
//         }
//
//         /// <summary>
//         /// Creates a new PostgreSQL select query with a Table object
//         /// </summary>
//         public static Select<T, R> Select<T, R>(Expression<Func<T, R>> selector, Table table)
//         {
//             return Sql.Create(selector, null, table, new PostgreSqlDialect());
//         }
//         
//         // New composite methods
//         
//         /// <summary>
//         /// Creates a new composite PostgreSQL select query with DataSchema
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> CompositeSelect<TRoot, TResult>(
//             Expression<Func<TRoot, TResult>> selector, 
//             DataSchema schema)
//         {
//             var table = new Table<TRoot> { Name = schema.CollectionName, Schema = schema.SchemaName };
//             return new CompositeSelect<TRoot, TResult>(selector, table, new PostgreSqlDialect());
//         }
//         
//         /// <summary>
//         /// Creates a new composite PostgreSQL select query with a custom table
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> CompositeSelect<TRoot, TResult>(
//             Expression<Func<TRoot, TResult>> selector, 
//             Table table)
//         {
//             return new CompositeSelect<TRoot, TResult>(selector, table, new PostgreSqlDialect());
//         }
//         
//         /// <summary>
//         /// Creates a new composite PostgreSQL select query with a table name
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> CompositeSelect<TRoot, TResult>(
//             Expression<Func<TRoot, TResult>> selector, 
//             string tableName = null)
//         {
//             var table = new Table<TRoot> { Name = tableName ?? typeof(TRoot).Name };
//             return new CompositeSelect<TRoot, TResult>(selector, table, new PostgreSqlDialect());
//         }
//     }
// } 