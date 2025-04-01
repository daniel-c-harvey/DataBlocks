using Dapper;
using ExpressionToSql;
using ExpressionToSql.Dapper;
using ScheMigrator.Migrations;
using NetBlocks.Models;
using System.Linq.Expressions;
using NetBlocks.Utilities;

namespace DataBlocks.DataAccess.Postgres
{
    public class PostgresQueryBuilder : IQueryBuilder<IPostgresDatabase>
    {
        private readonly PostgreSqlDialect _dialect = new PostgreSqlDialect();

        public IDataQuery<IPostgresDatabase, ResultContainer<TModel>> BuildRetrieve<TModel>(DataSchema target, long id) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<TModel>>(async (database) =>
            {
                var modelResults = new ResultContainer<TModel>();
                try
                {
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    var query = PSql.Select<TModel, TModel>(x => x, table).Where(x => x.ID == id);
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

        private class KeysParameter<TKey>
        {
            public IList<TKey> Keys { get; set; }
        }
        
        public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel, TKey>(DataSchema target, Expression<Func<TModel, TKey>> keySelector, IList<TKey> keys) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                try
                {
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    var sql = 
                        PSql
                            .Select((TModel x) => x, table)
                            .Where(x => !x.Deleted && QUtil.IsIn(keySelector, nameof(KeysParameter<TKey>.Keys)));
                    var results = await database.Connection.QueryAsync(sql, new KeysParameter<TKey> { Keys = keys });
                    return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }

        public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                try
                {
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    var sql = PSql.Select((TModel x) => x, table).Where(x => !x.Deleted);
                    var results = await database.Connection.QueryAsync(sql);
                    return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }
        
        public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target, int pageIndex, int pageSize) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                try
                {
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    var x = PSql.Select((TModel x) => x, table).Where(x => !x.Deleted).Page(pageIndex+1, pageSize);
                    var results = await database.Connection.QueryAsync(x);
                    return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }

        public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target, Expression<Func<TModel, bool>> predicate) where TModel : IModel
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TModel>>>(async (database) =>
            {
                try
                {
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    var sql = PSql.Select((TModel x) => x, table).Where(predicate.And(x => !x.Deleted));
                    var results = await database.Connection.QueryAsync(sql);
                    return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }

        public IDataQuery<IPostgresDatabase, Result> BuildDelete<TModel>(DataSchema target, TModel value) where TModel : IModel
        {
            return new PostgresQuery<Result>(async (database) =>
            {
                try
                {
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    string sql = $"DELETE FROM {_dialect.FormatSchemaName(table.Schema)}.{_dialect.EscapeIdentifier(table.Name)} WHERE id = @id";
                    await database.Connection.QueryAsync(sql, new { id = value.ID });
                    return Result.CreatePassResult();
                }
                catch (Exception ex)
                {
                    return Result.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }

        private class MaximumID<TKey>
        {
            public TKey MaxID { get; set; }
        }
        
        public IDataQuery<IPostgresDatabase, Result> BuildInsert<TModel>(DataSchema target, TModel value) where TModel : IModel
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

                    var columnNames = string.Join(", ", properties.Select(p => _dialect.EscapeIdentifier(p.ScheData!.FieldName)));
                    var paramNames = string.Join(", ", properties.Select(p => "@" + p.Property.Name));

                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };

                    if (value.ID == 0)
                    {
                        var id = properties.FirstOrDefault(p => p.Property.Name == nameof(value.ID))?.ScheData?.FieldName ?? throw new Exception("ID not found");
                        var idQuery =
                            $"""
                             SELECT COALESCE(MAX({id}),0) AS MaxID
                             FROM {_dialect.FormatSchemaName(table.Schema)}.{_dialect.EscapeIdentifier(table.Name)}
                             """;
                        var idResult = await database.Connection.QuerySingleAsync<MaximumID<long>>(idQuery);
                        value.ID = idResult.MaxID + 1;
                    }
                    
                    var sql = $"INSERT INTO {_dialect.FormatSchemaName(table.Schema)}.{_dialect.EscapeIdentifier(table.Name)} ({columnNames}) VALUES ({paramNames})";

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

        public IDataQuery<IPostgresDatabase, Result> BuildReplace<TModel>(DataSchema target, TModel value) where TModel : IModel
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
                    
                    var table = new Table<TModel> { Name = target.CollectionName, Schema = target.SchemaName };
                    var sql = $"""
                               UPDATE {_dialect.FormatSchemaName(table.Schema)}.{_dialect.EscapeIdentifier(table.Name)} 
                                    SET {string.Join(", ", properties.Select(p => $"{_dialect.EscapeIdentifier(p.ScheData!.FieldName)} = @{p.Property.Name}"))} 
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
    }
} 