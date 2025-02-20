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
        DDLGenerator.GenerateDDL(typeof(TestModelA), new PostgreSqlGenerator());
        Assert.Pass();
    }
    
}