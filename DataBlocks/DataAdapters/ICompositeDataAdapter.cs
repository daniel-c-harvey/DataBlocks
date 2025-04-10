using System.Linq.Expressions;
using DataBlocks.DataAccess;
using NetBlocks.Models;

namespace DataBlocks.DataAdapters;

public interface ICompositeDataAdapter<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>
    where TDataModel : IModel
    where TLinkDataModel : IModel
    where TTargetDataModel : IModel
    where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
    where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
    where TTargetModel : IConstituentModel<TTargetDataModel>
{
    Task<ResultContainer<IEnumerable<TCompositeModel>>> GetAll();
    Task<ResultContainer<IEnumerable<TCompositeModel>>> GetPage(int page, int pageSize);
    Task<ResultContainer<TCompositeModel>> GetByID(long id);
    Task<ResultContainer<IEnumerable<TCompositeModel>>> GetByPredicate(Expression<Func<TDataModel, TTargetDataModel, bool>> predicate);
}