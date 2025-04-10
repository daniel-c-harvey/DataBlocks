// using System;
// using DataBlocks.DataAccess;
// using ScheMigrator.Migrations;
//
// namespace ExpressionToSql.Composite
// {
//     /// <summary>
//     /// Sample models for demonstration
//     /// </summary>
//     public class ModelA
//     {
//         [ScheData("id")]
//         public int Id { get; set; }
//         
//         [ScheData("b_id")]
//         public int BId { get; set; }
//         
//         [ScheData("name")]
//         public string Name { get; set; }
//         
//         [ScheData("deleted")]
//         public bool Deleted { get; set; }
//     }
//     
//     public class ModelB
//     {
//         [ScheData("id")]
//         public int Id { get; set; }
//         
//         [ScheData("c_id")]
//         public int CId { get; set; }
//         
//         [ScheData("description")]
//         public string Description { get; set; }
//         
//         [ScheData("deleted")]
//         public bool Deleted { get; set; }
//     }
//     
//     public class ModelC
//     {
//         [ScheData("id")]
//         public int Id { get; set; }
//         
//         [ScheData("value")]
//         public string Value { get; set; }
//         
//         [ScheData("deleted")]
//         public bool Deleted { get; set; }
//     }
//     
//     /// <summary>
//     /// Demonstration of how to use the composite query functionality
//     /// </summary>
//     public static class CompositeQueryDemo
//     {
//         /// <summary>
//         /// Example of using the composite query functionality
//         /// </summary>
//         public static void RunDemo()
//         {
//             // Create schemas for each model
//             var schemaA = DataSchema.Create<ModelA>("schema_a");
//             var schemaB = DataSchema.Create<ModelB>("schema_b");
//             var schemaC = DataSchema.Create<ModelC>("schema_c");
//             
//             // Example 1: Simple select from one table
//             var query1 = PSqlC.SelectComposite<ModelA, ModelA>(
//                 a => a, 
//                 schemaA);
//             
//             Console.WriteLine("Simple select:");
//             Console.WriteLine(query1.ToSql());
//             Console.WriteLine();
//             
//             // Example 2: Select with JOIN between two tables
//             var query2 = PSqlC.SelectComposite<ModelA, ModelA>(
//                 a => a, 
//                 schemaA)
//                 .Join<ModelB>(
//                     schemaB, 
//                     (a, b) => a.BId == b.Id)
//                 .Where((a, b) => !a.Deleted && !b.Deleted);
//             
//             Console.WriteLine("Select with JOIN:");
//             Console.WriteLine(query2.ToSql());
//             Console.WriteLine();
//             
//             // Example 3: Select with multiple JOINs
//             var query3 = PSqlC.SelectComposite<ModelA, ModelA>(
//                 a => a, 
//                 schemaA)
//                 .Join<ModelB>(
//                     schemaB, 
//                     (a, b) => a.BId == b.Id)
//                 .Join<ModelC>(
//                     schemaC, 
//                     (b, c) => b.CId == c.Id)
//                 .Where((a, b, c) => !a.Deleted && !b.Deleted && !c.Deleted);
//             
//             Console.WriteLine("Select with multiple JOINs:");
//             Console.WriteLine(query3.ToSql());
//             Console.WriteLine();
//         }
//     }
// } 