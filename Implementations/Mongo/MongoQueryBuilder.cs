using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NetBlocks.Models;

namespace DataAccess
{
    public class MongoQueryBuilder : IQueryBuilder<IMongoDatabase>
    {
        public IDataQuery<IMongoDatabase, ResultContainer<TModel>> BuildRetrieve<TModel>(string collection) where TModel : IModel
        {
            return new MongoQuery<TModel, ResultContainer<TModel>>((database) =>
            {
                var modelResults = new ResultContainer<TModel>();
                try
                {
                    modelResults.Value = database.GetCollection<MongoObject<TModel>>(collection)
                    .Find(_ => true) // todo build in filtering control
                    .ToEnumerable()
                    .Select(m => m.Document)
                    .First();
                }
                catch (Exception ex) 
                {
                    return modelResults.Fail($"Database error: {ex.Message}");
                }
                return modelResults;
            });
        }
        
        public IDataQuery<IMongoDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(string collection, int pageIndex, int pageSize) where TModel : IModel
        {
            return new MongoQuery<TModel, ResultContainer<IEnumerable<TModel>>>((database) =>
            {
                var modelResults = new ResultContainer<IEnumerable<TModel>>();
                try
                {
                    modelResults.Value = database.GetCollection<MongoObject<TModel>>(collection)
                    .Find(_ => true)
                    .Skip(pageIndex * pageSize)
                    .Limit(pageSize)
                    .ToEnumerable()
                    .Select(m => m.Document)
                    .ToList();
                }
                catch (Exception ex)
                {
                    return modelResults.Fail($"Database error: {ex.Message}");
                }
                return modelResults;
            });
        }

        public IDataQuery<IMongoDatabase, Result> BuildInsert<TModel>(string collection, TModel value) where TModel : IModel
        {
            return new MongoQuery<TModel, Result>((database) =>
            {
                database.GetCollection<MongoObject<TModel>>(collection)
                    .InsertOne(new MongoObject<TModel>() { Document = value});

                return Result.CreatePassResult();
            });
        }

        public IDataQuery<IMongoDatabase, Result> BuildReplace<TModel>(string collection, TModel value) where TModel : IModel
        {
            return new MongoQuery<TModel, Result>(database =>
            {
                var filter = Builders<MongoObject<TModel>>.Filter.Eq(m => m.Document.ID, value.ID);

                var result = database.GetCollection<MongoObject<TModel>>(collection)
                    .ReplaceOne(filter, new MongoObject<TModel>() { Document = value });

                return result.IsAcknowledged
                    ? Result.CreatePassResult()
                    : Result.CreateFailResult($"Mongo QueryBuilder Database error occured.");
            });
        }

        public IDataQuery<IMongoDatabase, Result> BuildDelete<TModel>(string collection, TModel value) where TModel : IModel
        {
            return new MongoQuery<TModel, Result>(database =>
            {
                try
                {
                    var result = database.GetCollection<MongoObject<TModel>>(collection)
                        .DeleteOne(model => model.Document.ID == value.ID);
                    
                    return result.IsAcknowledged
                        ? Result.CreatePassResult()
                        : Result.CreateFailResult($"Mongo QueryBuilder Database error occured.");
                }
                catch (Exception ex)
                {
                    return Result.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }        
    }
}