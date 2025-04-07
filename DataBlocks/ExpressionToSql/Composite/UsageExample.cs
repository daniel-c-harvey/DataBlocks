using System;
using DataBlocks.DataAccess;
using ScheMigrator.Migrations;

namespace ExpressionToSql.Composite
{
    /// <summary>
    /// Examples showing how to use the composite query functionality with PSql
    /// </summary>
    public static class UsageExample
    {
        /// <summary>
        /// Run examples of composite queries using the PSql static class
        /// </summary>
        public static void RunExamples()
        {
            // Sample model schemas
            var modelASchema = DataSchema.Create<ModelA>("schema_a");
            var modelBSchema = DataSchema.Create<ModelB>("schema_b");
            var modelCSchema = DataSchema.Create<ModelC>("schema_c");
            
            // Example 1: Using PSqlExtensions directly
            Console.WriteLine("Example 1: Using PSqlExtensions directly");
            var query1 = PSqlExtensions.SelectComposite<ModelA, ModelA>(
                a => a,
                modelASchema)
                .Join<ModelB>(
                    modelBSchema,
                    (a, b) => a.BId == b.Id)
                .Where((a, b) => !a.Deleted && !b.Deleted);
                
            Console.WriteLine(query1.ToSql());
            Console.WriteLine();
            
            // Example 2: Using static methods directly for more complex scenarios
            Console.WriteLine("Example 2: Using static methods directly");
            var query2 = PSqlC.SelectComposite<ModelA, ModelA>(
                a => a,
                modelASchema)
                .Join<ModelB>(
                    modelBSchema,
                    (a, b) => a.BId == b.Id)
                .Join<ModelC>(
                    modelCSchema,
                    (b, c) => b.CId == c.Id)
                .Where((a, b, c) => !a.Deleted && !b.Deleted && !c.Deleted);
                
            Console.WriteLine(query2.ToSql());
            Console.WriteLine();
            
            // Example 3: Production-like usage with string queries
            Console.WriteLine("Example 3: Using table/schema names directly");
            var query3 = PSqlExtensions.SelectComposite<ModelA, ModelA>(
                a => a,
                "users")
                .Join<ModelB>(
                    new Table<ModelB> { Name = "profiles", Schema = "public" },
                    (a, b) => a.BId == b.Id)
                .Where((a, b) => a.Name.Contains("John") && !b.Deleted);
                
            Console.WriteLine(query3.ToSql());
        }
    }
} 