using System.Linq.Expressions;
using DataBlocks.DataAccess.Postgres;
using NetBlocks.Models;

namespace DataBlocks.DataAccess;

public interface ICompositeQueryBuilder<TDatabase>
{
    IDataQuery<TDatabase, ResultContainer<TCompositeModel>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(long key) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
    IDataQuery<TDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>() 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
    IDataQuery<TDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(int pageIndex, int pageSize) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
    IDataQuery<TDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(Expression<Func<TDataModel, TTargetDataModel, bool>> predicate) // todo take the target as a predicate parameter as well. 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TDataModel, TLinkDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
}