
using DataBlocks.Migrations;
using DataBlocksTests.Models;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Tests;


[TestFixture]
public class ScheMigratorTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void ShouldGeneratePostgreSql()
    {
        var ddl = ScheModelGenerator.GenerateModelDDL<TestModelA>(SqlImplementation.PostgreSQL, "public");
        Assert.Pass();
    }
    
    [Test]
    public void ShouldGenerateSqLite()
    {
        var ddl = ScheModelGenerator.GenerateModelDDL<TestModelA>(SqlImplementation.SQLite, string.Empty);
        Assert.Pass();
    }
}