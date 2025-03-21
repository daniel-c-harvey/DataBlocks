// using DataBlocks.DataAccess;
// using DataBlocks.DataAccess.Postgres;
// using DataBlocks.DataAdapters;
// using DataBlocksTests.Models;
// using NetBlocks.Models.Environment;

// namespace DataBlocksTests.Tests;

// [TestFixture]
// public class PostgresTests
// {
//     private Connection? connection;
//     private IQueryBuilder<IPostgresDatabase>? queryBuilder;
//     private IDataAccess<IPostgresDatabase>? dataAccess;
    
//     [SetUp]
//     public void Setup()
//     {
//         // var connections = TestModelTester.LoadConnections();
//         // connection = connections.ConnectionStrings[1];

//         // if (connection is null) throw new Exception("Connection is null");
//         // queryBuilder = QueryBuilderFactory.Create<IPostgresDatabase>();
//         // if (queryBuilder is null) throw new Exception("Query builder is null");

//         // dataAccess = DataAccessFactory.Create<IPostgresClient, IPostgresDatabase>(connection.ConnectionString, connection.DatabaseName);
//         // if (dataAccess is null) throw new Exception("Data access is null");
//     }

//     [Test]
//     public async Task TestWriteReadDeletePostgresTable()
//     {
//         if (dataAccess is null) throw new Exception("Data access is null");
//         if (queryBuilder is null) throw new Exception("Query builder is null");

//         IDataAdapter<TestModelA> adapter = DataAdapterFactory.Create<IPostgresDatabase, TestModelA>(dataAccess, 
//             queryBuilder, 
//             DataSchema.Create<TestModelA>("test-schema"));
        
//         await TestModelTester.TestModelA(adapter);
//     }
// }