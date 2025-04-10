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
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
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
                    var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                        .Join(TLinkDataModel.Schema, TCompositeModel.Predicate)
                        .Join(TTargetDataModel.Schema, TLinkModel.Predicate)
                        .Where((root, link, target) => root.ID == key)
                        .Select((root, link, target) => new CompositeContainer<TDataModel, TTargetDataModel> { CompositeModel = root, TargetModel = target });

                    // Take her to Dapper Town
                    var result = await database.Connection.QueryAsync<TDataModel, 
                                                                      TLinkDataModel, 
                                                                      TTargetDataModel, 
                                                                      TCompositeModel, 
                                                                      TTargetModel,
                                                                      CompositeContainer<TDataModel, TTargetDataModel>>(query, TCompositeModel.GetMap(), TCompositeModel.SplitOn);
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
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TCompositeModel>>>(async (database) =>
            {
                var modelResults = new ResultContainer<IEnumerable<TCompositeModel>>();
                try
                {
                    // // Create composite query with the enhanced SELECT capabilities
                    // var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                    //     .Join(TLinkDataModel.Schema, TCompositeModel.Predicate)
                    //     .Join(TTargetDataModel.Schema, TLinkModel.Predicate)
                    //     .Select((root, link, target) => new { root, target } as dynamic);
                    //
                    // // Execute using Dapper's multi-mapping capabilities
                    // var result = await database.Connection.QueryAsync<TDataModel, 
                    //                                                  TLinkDataModel, 
                    //                                                  TTargetDataModel, 
                    //                                                  TCompositeModel, 
                    //                                                  TTargetModel>(query, TCompositeModel.Map, TCompositeModel.SplitOn);
                    //
                    // modelResults.Value = result;
                    // return modelResults;
                    throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    return modelResults.Fail($"Database error: {ex.Message}");
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
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TCompositeModel>>>(async (database) =>
            {
                var modelResults = new ResultContainer<IEnumerable<TCompositeModel>>();
                try
                {
                    // // Create composite query with pagination
                    // var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                    //     .Join(TLinkDataModel.Schema, TCompositeModel.Predicate)
                    //     .Join(TTargetDataModel.Schema, TLinkModel.Predicate)
                    //     .Select((root, link, target) => new { root, target } as dynamic);
                    //
                    // // TODO: Add pagination parameters to the query
                    //
                    // // Execute using Dapper's multi-mapping capabilities
                    // var result = await database.Connection.QueryAsync<TDataModel, 
                    //                                                  TLinkDataModel, 
                    //                                                  TTargetDataModel, 
                    //                                                  TCompositeModel, 
                    //                                                  TTargetModel>(query, TCompositeModel.Map, TCompositeModel.SplitOn);
                    //
                    // modelResults.Value = result;
                    // return modelResults;
                    throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    return modelResults.Fail($"Database error: {ex.Message}");
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
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>
    {
        try
        {
            return new PostgresQuery<ResultContainer<IEnumerable<TCompositeModel>>>(async (database) =>
            {
                var modelResults = new ResultContainer<IEnumerable<TCompositeModel>>();
                try
                {
                    // // Create composite query with the predicate
                    // var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                    //     .Join(TLinkDataModel.Schema, TCompositeModel.Predicate)
                    //     .Join(TTargetDataModel.Schema, TLinkModel.Predicate)
                    //     .Where((root, link, target) => predicate.Compile()(root, target)) // Adapt the predicate
                    //     .Select((root, link, target) => new { root, target } as dynamic);
                    //
                    // // Execute using Dapper's multi-mapping capabilities
                    // var result = await database.Connection.QueryAsync<TDataModel, 
                    //                                                  TLinkDataModel, 
                    //                                                  TTargetDataModel, 
                    //                                                  TCompositeModel, 
                    //                                                  TTargetModel>(query, TCompositeModel.Map, TCompositeModel.SplitOn);
                    //
                    // modelResults.Value = result;
                    // return modelResults;
                    throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    return modelResults.Fail($"Database error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error building predicate composite query: {ex.Message}", ex);
        }
    }
}