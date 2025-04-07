using DataBlocks.DataAccess.Postgres;

namespace DataBlocks.DataAccess;

public interface ICompositeModel<TJoin, TLeftDataModel, TRightDataModel> : IJoinModel<TLeftDataModel, TRightDataModel>
where TJoin : IJoinModel<TLeftDataModel, TRightDataModel>
where TLeftDataModel : IModel
where TRightDataModel : IModel
{
    static JoinChain<TJoin, TLeftDataModel, TRightDataModel> Join { get; } = JoinChain<TJoin, TLeftDataModel, TRightDataModel>.CreateJoin();
}

public interface ILinkModel<TJoin, TLeftDataModel, TRightDataModel> : IJoinModel<TLeftDataModel, TRightDataModel>, IConstituentModel<TLeftDataModel>
    where TJoin : IJoinModel<TLeftDataModel, TRightDataModel>
    where TLeftDataModel : IModel
    where TRightDataModel : IModel
{
    static JoinChain<TJoin, TLeftDataModel, TRightDataModel> 
        Join<TPrevJoin, TPrevDataModel>(JoinChain<TPrevJoin, TPrevDataModel, TLeftDataModel> chain)
    where  TPrevJoin : IJoinModel<TPrevDataModel, TLeftDataModel>
    where TPrevDataModel : IModel
    {
        return chain.Join<TJoin, TRightDataModel>();
    }
}