// using System;
// using System.Linq.Expressions;
// using DataBlocks.DataAccess;
// using ExpressionToSql.Composite;

// namespace ExpressionToSql.Extensions
// {
//     /// <summary>
//     /// Extension methods to add Composite Query functionality to PSql
//     /// </summary>
//     public static class PSqlCompositeExtensions
//     {
//         /// <summary>
//         /// Creates a new composite Select query for PostgreSQL
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
//             this PSql _, 
//             Expression<Func<TRoot, TResult>> selector, 
//             DataSchema schema)
//         {
//             return Composite.PSqlCompositeExtensions.SelectComposite<TRoot, TResult>(selector, schema);
//         }
        
//         /// <summary>
//         /// Creates a new composite Select query for PostgreSQL with a custom table
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
//             this PSql _, 
//             Expression<Func<TRoot, TResult>> selector, 
//             Table table)
//         {
//             return Composite.PSqlCompositeExtensions.SelectComposite<TRoot, TResult>(selector, table);
//         }
        
//         /// <summary>
//         /// Creates a new composite Select query for PostgreSQL with a table name
//         /// </summary>
//         public static CompositeSelect<TRoot, TResult> SelectComposite<TRoot, TResult>(
//             this PSql _, 
//             Expression<Func<TRoot, TResult>> selector, 
//             string tableName = null)
//         {
//             return Composite.PSqlCompositeExtensions.SelectComposite<TRoot, TResult>(selector, tableName);
//         }
//     }
// } 