using System.Linq.Expressions;
using NetBlocks.Models;

namespace DataBlocks.DataAccess;

internal class CompositeContainer<TCompositeDataModel, TTargetDataModel>
where TCompositeDataModel : IModel
where TTargetDataModel : IModel
{
    public required TCompositeDataModel CompositeModel { get; set; }
    public required TTargetDataModel TargetModel { get; set; }
}

public interface ICompositeQueryBuilder<TDatabase>
{
    IDataQuery<TDatabase, ResultContainer<TCompositeModel>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(long key) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
    IDataQuery<TDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>() 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
    IDataQuery<TDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(int pageIndex, int pageSize) 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
    IDataQuery<TDatabase, ResultContainer<IEnumerable<TCompositeModel>>> BuildRetrieve<TCompositeModel, TDataModel, TLinkModel, TLinkDataModel, TTargetModel, TTargetDataModel>(Expression<Func<TDataModel, TTargetDataModel, bool>> predicate) // todo take the target as a predicate parameter as well. 
        where TDataModel : IModel
        where TLinkDataModel : IModel
        where TTargetDataModel : IModel
        where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TDataModel, TLinkDataModel, TTargetDataModel>
        where TLinkModel : ILinkModel<TLinkModel, TLinkDataModel, TTargetDataModel>
        where TTargetModel : IConstituentModel<TTargetDataModel>;
}