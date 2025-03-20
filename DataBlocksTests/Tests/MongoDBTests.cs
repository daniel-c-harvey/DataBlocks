using DataBlocks.DataAccess;
using DataBlocks.DataAdapters;
using DataBlocksTests.Models;
using MongoDB.Driver;
using NetBlocks.Models.Environment;

namespace DataBlocksTests.Tests;

[TestFixture]
public class MongoDBTests
{
    private Connection? connection;
    private IQueryBuilder<IMongoDatabase>? queryBuilder;
    private IDataAccess<IMongoDatabase>? dataAccess;
    
    [SetUp]
    public void Setup()
    {
        var connections = TestModelTester.LoadConnections();
        connection = connections.ConnectionStrings[0];

        if (connection is null) throw new Exception("Connection is null");
        queryBuilder = QueryBuilderFactory.Create<IMongoDatabase>();
        if (queryBuilder is null) throw new Exception("Query builder is null");

        dataAccess = DataAccessFactory.Create<IMongoClient, IMongoDatabase>(connection.ConnectionString, connection.DatabaseName);
        if (dataAccess is null) throw new Exception("Data access is null");
    }

    [Test]
    public async Task TestWriteReadDeleteCollection()
    {
        if (dataAccess is null) throw new Exception("Data access is null");
        if (queryBuilder is null) throw new Exception("Query builder is null");

        IDataAdapter<TestModelA> adapter = DataAdapterFactory.Create<IMongoDatabase, TestModelA>(dataAccess, 
                                                                                                 queryBuilder, 
                                                                                                 DataSchema.Create<TestModelA>("test-schema"));
        await TestModelTester.TestModelA(adapter);
    }
}