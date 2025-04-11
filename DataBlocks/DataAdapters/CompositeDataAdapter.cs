using System.Linq.Expressions;
using DataBlocks.DataAccess;
using NetBlocks.Models;

namespace DataBlocks.DataAdapters;

public abstract class CompositeDataAdapter<TDatabase, TDataAccess, TQueryBuilder, TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel> 
: ICompositeDataAdapter<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>
    where TDatabase : class
    where TDataAccess : IDataAccess<TDatabase>
    where TQueryBuilder : ICompositeQueryBuilder<TDatabase>
    where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
    where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
    where TTargetModel : IConstituentModel<TTargetDataModel>
    where TDataModel : IModel
    where TLinkDataModel : IModel
    where TTargetDataModel : IModel
{
    protected TDataAccess DataAccess;
    protected TQueryBuilder QueryBuilder;

    public CompositeDataAdapter(TDataAccess dataAccess, TQueryBuilder queryBuilder)
    {
        DataAccess = dataAccess;
        QueryBuilder = queryBuilder;
    }
    
    public virtual async Task<ResultContainer<IEnumerable<TCompositeModel>>> GetAll()
    {
        var modelResults = new ResultContainer<IEnumerable<TCompositeModel>>();
        try
        {
            modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>());
            return modelResults;
        }
        catch (Exception ex) 
        {
            return modelResults.Fail(ex.Message);
        }
    }

    public virtual async Task<ResultContainer<IEnumerable<TCompositeModel>>> GetPage(int pageIndex, int pageSize)
    {
        var modelResults = new ResultContainer<IEnumerable<TCompositeModel>>();
        try
        {
            modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(pageIndex, pageSize));
            return modelResults;
        }
        catch (Exception ex) 
        {
            return modelResults.Fail(ex.Message);
        }
    }

    public virtual async Task<ResultContainer<TCompositeModel>> GetByID(long id)
    {
        var modelResults = new ResultContainer<TCompositeModel>();
        try
        {
            modelResults = await DataAccess.ExecQueryOne(QueryBuilder.BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(id));
            return modelResults;
        }
        catch (Exception ex) 
        {
            return modelResults.Fail(ex.Message);
        }
    }

    public virtual async Task<ResultContainer<IEnumerable<TCompositeModel>>> GetByPredicate(Expression<Func<TDataModel, TTargetDataModel, bool>> predicate)
    {
        var modelResults = new ResultContainer<IEnumerable<TCompositeModel>>();
        try
        {
            modelResults = await DataAccess.ExecQuery(QueryBuilder.BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(predicate));
            return modelResults;
        }
        catch (Exception ex) 
        {
            return modelResults.Fail(ex.Message);
        }
    }
}

