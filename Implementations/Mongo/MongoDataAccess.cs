using MongoDB.Driver;
using NetBlocks.Models;

namespace DataAccess
{
    public class MongoDataAccess : IDataAccess<IMongoDatabase>
    {
        private MongoDBMSClient? DBMSClient { get; set; }

        public MongoDataAccess(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) { throw new ArgumentNullException(nameof(connectionString)); }
            if (string.IsNullOrWhiteSpace(databaseName)) { throw new ArgumentNullException(nameof(databaseName)); }

            DBMSClient = new MongoDBMSClient(connectionString);
            DBMSClient.SetDatabase(databaseName);
        }

        public ResultContainer<string> GetConnectionString()
        {
            if (DBMSClient == null) { throw new ArgumentNullException(nameof(DBMSClient)); }

            return new ResultContainer<string>(DBMSClient.ConnectionString);
        }

        public ResultContainer<string> GetDatabaseName()
        {
            if (DBMSClient?.Connection == null) { throw new ArgumentNullException(nameof(DBMSClient.Connection)); }

            return new ResultContainer<string>(DBMSClient.Connection.DatabaseName);
        }

        public ResultContainer<IEnumerable<string>> GetDatabaseNames()
        {
            if (DBMSClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

            ResultContainer<IEnumerable<string>> result = new();
            result.Value = DBMSClient.GetDatabaseNames().ToList();
            return result;
        }

        public Result ChangeConnection(Connection connection, string databaseName)
        {
            if (connection == null) { throw new ArgumentNullException("Connection"); }

            DBMSClient = new MongoDBMSClient(connection.ConnectionString);
            DBMSClient.SetDatabase(databaseName);
            return Result.CreatePassResult();
        }

        public ResultContainer<IEnumerable<TModel>> ExecQuery<TModel>(IDataQuery<IMongoDatabase, ResultContainer<IEnumerable<TModel>>> query)
        {
            if (DBMSClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

            return DBMSClient.Connection.Database.ExecQuery(query);
        }

        public ResultContainer<TModel> ExecQueryOne<TModel>(IDataQuery<IMongoDatabase, ResultContainer<TModel>> query)
        {
            if (DBMSClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

            return DBMSClient.Connection.Database.ExecQuery(query);
        }

        public Result ExecNonQuery(IDataQuery<IMongoDatabase, Result> query)
        {
            if (DBMSClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

            return DBMSClient.Connection.Database.ExecQuery(query);
        }
    }
}
