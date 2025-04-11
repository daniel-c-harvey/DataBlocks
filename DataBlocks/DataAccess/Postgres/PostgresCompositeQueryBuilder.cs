using System.Linq.Expressions;
using ExpressionToSql.Composite;
using ExpressionToSql.Dapper;
using NetBlocks.Models;
using Dapper;
using ExpressionToSql;
using MongoDB.Driver;
using NetBlocks.Utilities;

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
                        .Join(TLinkDataModel.Schema, TCompositeModel.Predicate, JoinType.Left)
                        .Join(TTargetDataModel.Schema, TLinkModel.Predicate, JoinType.Left)
                        .Where((root, link, target) => !root.Deleted && !target.Deleted && root.ID == key)
                        .Select((root, link, target) => new CompositeContainer<TDataModel, TTargetDataModel> { CompositeModel = root, TargetModel = target });

                    // Take her to Dapper Town
                    var result = await database.Connection.QueryAsync(query, TCompositeModel.GetMap(), TCompositeModel.SplitOn);
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
                    var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                        .Join(TLinkDataModel.Schema, TCompositeModel.Predicate, JoinType.Left)
                        .Join(TTargetDataModel.Schema, TLinkModel.Predicate, JoinType.Left)
                        .Where((root, link, target) => !root.Deleted && !target.Deleted)
                        .Select((root, link, target) => new CompositeContainer<TDataModel, TTargetDataModel> { CompositeModel = root, TargetModel = target });

                    // Take her to Dapper Town
                    var result = await database.Connection.QueryAsync(query, TCompositeModel.GetMap(), TCompositeModel.SplitOn);
                    modelResults.Value = result
                        .GroupBy(model => model.ID)
                        .Select(group => group.First())
                        .ToList();
                    return modelResults;
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
                    
                    var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                        .PageByRoot(pageIndex, pageSize)
                        .Join(TLinkDataModel.Schema, TCompositeModel.Predicate, JoinType.Left)
                        .Join(TTargetDataModel.Schema, TLinkModel.Predicate, JoinType.Left)
                        .Where((root, link, target) => !root.Deleted && !target.Deleted)
                        .Select((root, link, target) => new CompositeContainer<TDataModel, TTargetDataModel> { CompositeModel = root, TargetModel = target });

                    // Take her to Dapper Town
                    var result = await database.Connection.QueryAsync(query, TCompositeModel.GetMap(), TCompositeModel.SplitOn);
                    modelResults.Value = result
                        .GroupBy(model => model.ID)
                        .Select(group => group.First())
                        .ToList();
                    return modelResults;
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
                    var wrappedPredicate = predicate.And((root, target) => !root.Deleted && !target.Deleted)
                        .WrapPredicate<TDataModel, TLinkDataModel, TTargetDataModel>();
                    
                    var query = PSqlC.From<TDataModel>(TDataModel.Schema)
                        .Join(TLinkDataModel.Schema, TCompositeModel.Predicate)
                        .Join(TTargetDataModel.Schema, TLinkModel.Predicate)
                        .Where(wrappedPredicate)
                        .Select((root, link, target) => new CompositeContainer<TDataModel, TTargetDataModel> { CompositeModel = root, TargetModel = target });
                    
                    var result = await database.Connection.QueryAsync(query, TCompositeModel.GetMap(), TCompositeModel.SplitOn);
                    
                    modelResults.Value = result
                        .GroupBy(model => model.ID)
                        .Select(group => group.First())
                        .ToList();
                    return modelResults;
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

internal static class PQBExtensions
{
    public static Expression<Func<T1, T2, T3, bool>> WrapPredicate<T1, T2, T3>(
        this Expression<Func<T1, T3, bool>> expression)
    {
        // Create new parameters for the three-parameter function
        var param1 = Expression.Parameter(typeof(T1), "x");
        var param2 = Expression.Parameter(typeof(T2), "y"); // This parameter will be ignored
        var param3 = Expression.Parameter(typeof(T3), "z");
    
        // Replace the original parameters with the new ones (ignoring param2)
        var visitor = new ExpressionExtensions.ReplaceParametersVisitor(
            new[] { expression.Parameters[0], expression.Parameters[1] },
            new[] { param1, param3 });
    
        var newBody = visitor.Visit(expression.Body);
    
        // Create a new lambda with three parameters
        return Expression.Lambda<Func<T1, T2, T3, bool>>(
            newBody, 
            param1, 
            param2, // Include the ignored parameter in the lambda
            param3);
    }
}
