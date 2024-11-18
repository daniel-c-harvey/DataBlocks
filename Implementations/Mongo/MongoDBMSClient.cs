using MongoDB.Driver;

namespace DataAccess
{
    internal class MongoDBMSClient : DBMSClient<IMongoClient, IMongoDatabase>, ICLient<IMongoClient>
    {
        public MongoDBMSClient(string connectionString) 
            : base(connectionString) 
        {
            Client = new MongoClient(ConnectionString);
        }

        public IDatabase<TDatabase> GetDatabase<TDatabase>(string databaseName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetDatabaseNames()
        {
            return Client.ListDatabaseNames().ToEnumerable();
        }

        public override void SetDatabase(string databaseName)
        {
            Connection = new MongoDatabaseConnection(Client, databaseName);
        }
    }
}
