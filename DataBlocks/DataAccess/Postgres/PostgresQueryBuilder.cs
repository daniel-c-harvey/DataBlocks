using Dapper;
using ExpressionToSql;
using ExpressionToSql.Dapper;
using DataBlocks.Migrations;
using NetBlocks.Models;
using System.Linq.Expressions;
using NetBlocks.Utilities;

namespace DataBlocks.DataAccess.Postgres
{
    public class PostgresQueryBuilder : IQueryBuilder<IPostgresDatabase>
    {
        
        public IDataQuery<IPostgresDatabase, ResultContainer<TModel>> BuildRetrieveById<TModel>(string collection, long id) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<TModel>>(async (database) =>
            {
                var modelResults = new ResultContainer<TModel>();
                try
                {
                    var query = PSql.Select<TModel, TModel>(x => x, collection).Where(x => x.ID == id);
                    var result = await database.Connection.QueryAsync(query);
                    if (result != null)
                    {
                        modelResults.Value = result.First();
                    }
                }
                catch (Exception ex)
                {
                    return modelResults.Fail($"Database error: {ex.Message}");
                }
                return modelResults;
            });
        }

        public IDataQuery<IPostgresDatabase, Result> BuildDelete<TModel>(string collection, TModel value) where TModel : IModel
        {
            return new PostgresQuery<Result>(async (database) =>
            {
                try
                {
                    string sql = $"DELETE FROM {collection} WHERE id = @id";
                    await database.Connection.QueryAsync(sql, new { id = value.ID });
                    return Result.CreatePassResult();
                }
                catch (Exception ex)
                {
                    return Result.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }

        public IDataQuery<IPostgresDatabase, Result> BuildInsert<TModel>(string collection, TModel value) where TModel : IModel
        {
            return new PostgresQuery<Result>(async (database) =>
            {
                var result = new Result();
                try
                {
                    
                    var properties = typeof(TModel).GetProperties()
                        .Select(p => new {
                            Property = p,
                            ScheData = p.GetCustomAttributes(typeof(ScheDataAttribute), true).FirstOrDefault() as ScheDataAttribute
                        })
                        .Where(x => x.ScheData != null)
                        .ToList();

                    var columnNames = string.Join(", ", properties.Select(p => p.ScheData!.Name));
                    var paramNames = string.Join(", ", properties.Select(p => "@" + p.Property.Name));

                    var sql = $"INSERT INTO {collection} ({columnNames}) VALUES ({paramNames})";

                    await database.Connection.ExecuteAsync(sql, value);
                    result.Pass();
                }
                catch (Exception ex)
                {
                    return result.Fail($"Database error: {ex.Message}");
                }
                return result;
            });
        }

        public IDataQuery<IPostgresDatabase, Result> BuildReplace<TModel>(string collection, TModel value) where TModel : IModel
        {
            return new PostgresQuery<Result>(async (database) =>
            {
                var result = new Result();
                try
                {
                    var properties = typeof(TModel).GetProperties()
                        .Select(p => new {
                            Property = p,
                            ScheData = p.GetCustomAttributes(typeof(ScheDataAttribute), true).FirstOrDefault() as ScheDataAttribute
                        })
                        .Where(x => x.ScheData != null)
                        .Where(x => !typeof(IModel).GetProperties()
                                                        .Select(p => p.Name)
                                                        .Contains(x.Property.Name) // remove the base-type fields from the update
                                                    || x.Property.Name == nameof(IModel.Modified))// except for the modified field which should be updated
                        .ToList();

                    var originalModifiedDate = value.Modified;
                    Model.PrepareForUpdate(value);
                    
                    var parameters = new DynamicParameters(value);
                    parameters.Add("OriginalModified", originalModifiedDate); // Add original modified date for WHERE clause comparison
                    
                    var sql = $"""
                               UPDATE {collection} 
                                    SET {string.Join(", ", properties.Select(p => $"{p.ScheData!.Name} = @{p.Property.Name}"))} 
                               WHERE id = @{nameof(IModel.ID)}
                                 AND modified = @OriginalModified 
                               """;

                    await database.Connection.ExecuteAsync(sql, parameters);
                    result.Pass();
                }
                catch (Exception ex)
                {
                    return result.Fail($"Database error: {ex.Message}");
                }
                return result;
            });
        }

        public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(string collection, int pageIndex, int pageSize) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                try
                {
                    var x = PSql.Select((TModel x) => x, collection).Where(x => !x.Deleted).Page(pageIndex+1, pageSize);
                    var results = await database.Connection.QueryAsync(x);
                    return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }

        public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(string collection, Expression<Func<TModel, bool>> predicate) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                try
                {
                    var sql = PSql.Select((TModel x) => x, collection).Where(predicate.And(x => !x.Deleted));
                    var results = await database.Connection.QueryAsync(sql);
                    return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }
    }
} 