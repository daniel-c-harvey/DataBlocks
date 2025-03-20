using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Mongo;
using DataBlocks.DataAdapters;
using MongoDB.Driver;

namespace DataAccess
{
    public class MongoAdapter<TModel> : DataAdapter<IMongoDatabase, MongoDataAccess, MongoQueryBuilder, TModel>
    where TModel : IModel
    {
        public MongoAdapter(MongoDataAccess dataAccess, MongoQueryBuilder queryBuilder, DataSchema schema) 
        : base(dataAccess, queryBuilder, schema) 
        {
            MongoModelMapper.RegisterModel<TModel>();
        }
    }
}
