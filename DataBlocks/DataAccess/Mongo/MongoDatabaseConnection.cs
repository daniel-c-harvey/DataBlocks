using DataAccess;
using MongoDB.Driver;

namespace DataBlocks.DataAccess.Mongo
{
    internal class MongoDatabaseConnection : DatabaseConnection<IMongoDatabase>
    {
        public override IDatabase<IMongoDatabase> Database { get; }

        public MongoDatabaseConnection(IMongoClient client, string databaseName)
            : base(databaseName) 
        {
            Database = new DatabaseContainer<IMongoDatabase>(client.GetDatabase(DatabaseName));
        }

    }
}
