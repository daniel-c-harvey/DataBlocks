using DataAccess;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Mongo;
using DataBlocks.DataAccess.Postgres;
using MongoDB.Driver;

namespace DataBlocks.DataAdapters
{
    public static class DataAdapterFactory
    {
        public static IDataAdapter<TModel> Create<TDatabase, TModel>(
            IDataAccess<TDatabase> dataAccess, 
            IQueryBuilder<TDatabase> queryBuilder, 
            DataSchema schema)
            where TModel : IModel
        {
            if (dataAccess.GetType().IsAssignableFrom(typeof(MongoDataAccess)) && 
                queryBuilder.GetType().IsAssignableFrom(typeof(MongoQueryBuilder)) &&
                typeof(TDatabase).IsAssignableFrom(typeof(IMongoDatabase)))
            {
                return new MongoAdapter<TModel>(
                    (MongoDataAccess)(object)dataAccess, 
                    (MongoQueryBuilder)(object)queryBuilder, 
                    schema);
            }
            else if (dataAccess.GetType().IsAssignableFrom(typeof(PostgresDataAccess)) && 
                     queryBuilder.GetType().IsAssignableFrom(typeof(PostgresQueryBuilder)) &&
                     typeof(TDatabase).IsAssignableFrom(typeof(IPostgresDatabase)))
            {
                return new PostgresAdapter<TModel>(
                    (PostgresDataAccess)(object)dataAccess, 
                    (PostgresQueryBuilder)(object)queryBuilder, 
                    schema);
            }

            throw new ArgumentException($"No adapter available for the combination of {typeof(TDatabase).Name}");
        }
    }
} 