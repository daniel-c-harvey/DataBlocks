using System.Linq.Expressions;
using ExpressionToSql.Composite;
using ExpressionToSql.Dapper;
using NetBlocks.Models;
using Dapper;

namespace DataBlocks.DataAccess.Postgres;

public class PostgresCompositeQueryBuilder : ICompositeQueryBuilder<IPostgresDatabase>
{
    public IDataQuery<IPostgresDatabase, ResultContainer<TCompositeModel>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(long key) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<TCompositeModel>>(async (database) =>
            {
                var modelResults = new ResultContainer<TCompositeModel>();
                try
                {                    
                    var query = PSqlC.SelectComposite<TDataModel, TDataModel>(x => x, TDataModel.Schema)
                        .Join(TLinkDataModel.Schema, TCompositeModel.Predicate)
                        .Join(TTargetDataModel.Schema, TLinkModel.Predicate)
                        .Where((root, link, target) => root.ID == key);

                    // Take her to Dapper Town
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
        catch (Exception ex)
        {
            throw new Exception($"Error building composite query: {ex.Message}", ex);
        }
    }

    public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>() 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TCompositeModel>>>(async (database) =>
            {
                try
                {                    
                    // Execute the query using Dapper
                    // var results = await database.Connection.QueryAsync<TModel>(sql);
                    // return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                    throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TCompositeModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error building composite query: {ex.Message}", ex);
        }
    }

    public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TCompositeModel>>> 
        BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(int pageIndex, int pageSize) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TCompositeModel>>>(async (database) =>
            {
                try
                {
                    // Execute the query using Dapper
                    // var results = await database.Connection.QueryAsync<TModel>(sql);
                    // return ResultContainer<IEnumerable<TModel>>.CreatePassResult(results);
                    throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TCompositeModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error building paginated composite query: {ex.Message}", ex);
        }
    }

    public IDataQuery<IPostgresDatabase, ResultContainer<IEnumerable<TCompositeModel>>> 
        BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(Expression<Func<TDataModel, TTargetDataModel, bool>> predicate) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TCompositeModel>>>(async (database) =>
            {
                try
                {
                    // var table = new Table<TModel> { Name = schema.CollectionName, Schema = schema.SchemaName };
                    
                    // Create composite query
                    // var query = PSqlC.SelectComposite<TModel, TModel>(x => x, table);
                    
                    // Execute the query using Dapper
                    // var results = await database.Connection.QueryAsync<TModel>(sql);
                    throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    return ResultContainer<IEnumerable<TCompositeModel>>.CreateFailResult($"Database error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error building predicate composite query: {ex.Message}", ex);
        }
    }
}