using DataBlocks.Migrations;
using ScheMigrator.DDL;
using ScheMigratorTests.Models;

namespace ScheMigratorTests.Tests;

[TestFixture]
public class MainTest
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void Should()
    {
        var ddl = ScheModelGenerator.GenerateModelDDL<TestModelA>(SqlImplementation.PostgreSQL, "public");
        Assert.Pass();
    }
    
}