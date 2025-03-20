﻿using DataBlocks.Migrations;
using ScheMigrator.Migrations;
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