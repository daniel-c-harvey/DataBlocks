// using System;
// using System.Linq.Expressions;
// using DataBlocks.DataAccess;
// using ExpressionToSql.Composite;
//
// namespace ExpressionToSql
// {
//     /// <summary>
//     /// Extensions for the PSql class to support composite queries
//     /// </summary>
//     public static class PSqlExtensions
//     {
//         /// <summary>
//         /// Creates a new composite Select query for PostgreSQL
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
//             Expression<Func<TRoot, TResult>> selector, 
//             DataSchema schema)
//         {
//             return Composite.PSqlC.SelectComposite<TRoot, TResult>(selector, schema);
//         }
//         
//         /// <summary>
//         /// Creates a new composite Select query for PostgreSQL with a custom table
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
//             Expression<Func<TRoot, TResult>> selector, 
//             Table table)
//         {
//             return Composite.PSqlC.SelectComposite<TRoot, TResult>(selector, table);
//         }
//         
//         /// <summary>
//         /// Creates a new composite Select query for PostgreSQL with a table name
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
//             Expression<Func<TRoot, TResult>> selector, 
//             string tableName = null)
//         {
//             return Composite.PSqlC.SelectComposite<TRoot, TResult>(selector, tableName);
//         }
//     }
// } 