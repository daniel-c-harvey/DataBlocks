using System.Linq.Expressions;
using DataAccess;
using MongoDB.Driver;
using NetBlocks.Models;

namespace DataBlocks.DataAccess.Mongo
{
    public class MongoQueryBuilder : IQueryBuilder<IMongoDatabase>
    {
        public IDataQuery<IMongoDatabase, ResultContainer<TModel>> BuildRetrieve<TModel>(DataSchema target, long id) where TModel : IModel
        {
            return new MongoQuery<ResultContainer<TModel>>(async (database) =>
            {
                var modelResults = new ResultContainer<TModel>();
                try
                {
                    modelResults.Value = database.GetCollection<MongoObject<TModel>>(target.GetCollectionName())
                    .Find(m => m.Document.ID == id) 
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

        public IDataQuery<IMongoDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel, TKey>(DataSchema target, Expression<Func<TModel, TKey>> keySelector, IList<TKey> keys) where TModel : IModel
        {
            throw new NotImplementedException();
        }

        public IDataQuery<IMongoDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target) where TModel : IModel
        {
            throw new NotImplementedException();
        }

        public IDataQuery<IMongoDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target, int pageIndex, int pageSize) where TModel : IModel
        {
            return new MongoQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                var modelResults = new ResultContainer<IEnumerable<TModel>>();
                try
                {
                    modelResults.Value = database.GetCollection<MongoObject<TModel>>(target.GetCollectionName())
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

        public IDataQuery<IMongoDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target, Expression<Func<TModel, bool>> predicate) where TModel : IModel
        {
            return new MongoQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                var modelResults = new ResultContainer<IEnumerable<TModel>>();
                try
                {
                    modelResults.Value = database.GetCollection<MongoObject<TModel>>(target.GetCollectionName())
                        .Find(MongoObjectPredicate(predicate))
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

        public IDataQuery<IMongoDatabase, Result> BuildInsert<TModel>(DataSchema target, TModel value) where TModel : IModel
        {
            return new MongoQuery<Result>(async (database) =>
            {
                database.GetCollection<MongoObject<TModel>>(target.GetCollectionName())
                    .InsertOne(new MongoObject<TModel>() { Document = value});

                return Result.CreatePassResult();
            });
        }

        public IDataQuery<IMongoDatabase, Result> BuildReplace<TModel>(DataSchema target, TModel value) where TModel : IModel
        {
            return new MongoQuery<Result>(async database =>
            {
                var filter = Builders<MongoObject<TModel>>.Filter.Eq(m => m.Document.ID, value.ID);

                var result = database.GetCollection<MongoObject<TModel>>(target.GetCollectionName())
                    .ReplaceOne(filter, new MongoObject<TModel>() { Document = value });

                return result.IsAcknowledged
                    ? Result.CreatePassResult()
                    : Result.CreateFailResult($"Mongo QueryBuilder Database error occurred.");
            });
        }

        public IDataQuery<IMongoDatabase, Result> BuildDelete<TModel>(DataSchema target, TModel value) where TModel : IModel
        {
            return new MongoQuery<Result>(async database =>
            {
                try
                {
                    var result = database.GetCollection<MongoObject<TModel>>(target.GetCollectionName())
                        .DeleteOne(model => model.Document.ID == value.ID);
                    
                    return result.IsAcknowledged
                        ? Result.CreatePassResult()
                        : Result.CreateFailResult($"Mongo QueryBuilder Database error occurred.");
                }
                catch (Exception ex)
                {
                    return Result.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }        

        private Expression<Func<MongoObject<TModel>, bool>> MongoObjectPredicate<TModel>(Expression<Func<TModel, bool>> predicate) where TModel : IModel
        {
            // Create a parameter for MongoObject<TModel>
            var parameter = Expression.Parameter(typeof(MongoObject<TModel>), "mo");
            
            // Access the Document property
            var documentProperty = Expression.Property(parameter, "Document");
            
            // Replace the parameter in the original expression
            var visitor = new ExpressionReplacer(predicate.Parameters[0], documentProperty);
            var newBody = visitor.Visit(predicate.Body);
            
            // Create a new lambda expression with the new body and parameter
            return Expression.Lambda<Func<MongoObject<TModel>, bool>>(newBody, parameter);
        }

        // Helper class to replace expression parameters
        private class ExpressionReplacer : ExpressionVisitor
        {
            private readonly Expression _from;
            private readonly Expression _to;

            public ExpressionReplacer(Expression from, Expression to)
            {
                _from = from;
                _to = to;
            }

            public override Expression Visit(Expression node)
            {
                return node == _from ? _to : base.Visit(node);
            }
        }
    }
}