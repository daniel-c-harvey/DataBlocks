using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    internal class MongoQuery<T, TReturn> : IDataQuery<IMongoDatabase, TReturn>
    {
        public Func<IMongoDatabase, TReturn> Query { get; }

        public MongoQuery(Func<IMongoDatabase, TReturn> query) {  Query = query; }
    }
}
