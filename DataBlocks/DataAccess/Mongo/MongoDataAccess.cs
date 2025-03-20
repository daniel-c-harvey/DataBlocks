using MongoDB.Driver;
using NetBlocks.Models;
using NetBlocks.Models.Environment;

namespace DataBlocks.DataAccess.Mongo;

public class MongoDataAccess : DataAccess<IMongoClient, IMongoDatabase>
{

    public MongoDataAccess(string connectionString, string databaseName) 
    : base(connectionString, databaseName)
    {
        DBClient = new MongoDBMSClient(connectionString);
        DBClient.SetDatabase(databaseName);
    }

    public ResultContainer<IEnumerable<string>> GetDatabaseNames()
    {
        if (DBClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

        ResultContainer<IEnumerable<string>> result = new();
        result.Value = ((MongoDBMSClient)DBClient).GetDatabaseNames().ToList();
        return result;
    }

    public Result ChangeConnection(Connection connection, string databaseName)
    {
        if (connection == null) { throw new ArgumentNullException("Connection"); }

        DBClient = new MongoDBMSClient(connection.ConnectionString);
        DBClient.SetDatabase(databaseName);
        return Result.CreatePassResult();
    }
}
