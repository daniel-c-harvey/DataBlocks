using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
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
