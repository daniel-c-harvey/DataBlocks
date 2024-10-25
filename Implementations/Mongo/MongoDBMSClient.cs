using Core;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
