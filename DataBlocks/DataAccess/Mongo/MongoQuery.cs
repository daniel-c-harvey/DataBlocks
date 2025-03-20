using MongoDB.Driver;

namespace DataBlocks.DataAccess.Mongo
{
    internal class MongoQuery<TReturn> : IDataQuery<IMongoDatabase, TReturn>
    {
        public Func<IMongoDatabase, Task<TReturn>> Query { get; }

        public MongoQuery(Func<IMongoDatabase, Task<TReturn>> query) {  Query = query; }
    }
}
