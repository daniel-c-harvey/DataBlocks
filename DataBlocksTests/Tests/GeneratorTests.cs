using System.Runtime.InteropServices;
using DataBlocksTests.Models;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Tests;

[TestFixture]
public class GeneratorTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void ShouldGeneratePostgreSqlTestModelA()
    {
        var ddl = ScheModelGenerator.GenerateModelDDL<TestModelA>(SqlImplementation.PostgreSQL, "public");
        Assert.Pass();
    }
    
    [Test]
    public void ShouldGeneratePostgreSqlUser()
    {
        var ddl = ScheModelGenerator.GenerateModelDDL<ApplicationUser>(SqlImplementation.PostgreSQL, "test-schema");
        Console.Write(ddl);
        Assert.Pass();
    }

    [Test]
    public void ShouldGeneratePostgreSqlUserByTask()
    {
        var paths = new List<string>(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
        {
            Path.Combine(AppContext.BaseDirectory, "Microsoft.Extensions.Identity.Stores.dll"),
            Path.Combine(AppContext.BaseDirectory, "ScheMigrator.dll"),
            Path.Combine(AppContext.BaseDirectory, "DataBlocks.dll")
        };
        var resolver = new System.Reflection.PathAssemblyResolver(paths);
        var context = new System.Reflection.MetadataLoadContext(resolver);
        var type = context.LoadFromAssemblyPath(typeof(ApplicationUser).Assembly.Location)
            .GetType(typeof(ApplicationUser).FullName!)!;

        string ddl = ScheModelGenerator.GenerateModelDDL(type, SqlImplementation.PostgreSQL, "test-schema");
        Console.Write(ddl);
        Assert.Pass();
    }
    [Test]
    public void ShouldGenerateSqLite()
    {
        var ddl = ScheModelGenerator.GenerateModelDDL<TestModelA>(SqlImplementation.SQLite, string.Empty);
        Assert.Pass();
    }
}