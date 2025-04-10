using DataBlocks.DataAccess.Postgres;

namespace DataBlocks.DataAccess;

public interface ICompositeModel<TCompositeModel, TTargetModel, TLeftDataModel, TRightDataModel, TTargetDataModel> : IJoinModel<TLeftDataModel, TRightDataModel>
where TCompositeModel : ICompositeModel<TCompositeModel, TTargetModel, TLeftDataModel, TRightDataModel, TTargetDataModel>
where TTargetModel : IConstituentModel<TTargetDataModel>
where TLeftDataModel : IModel
where TRightDataModel : IModel
where TTargetDataModel : IModel
{
    static JoinChain<TCompositeModel, TLeftDataModel, TRightDataModel> Join { get; } = JoinChain<TCompositeModel, TLeftDataModel, TRightDataModel>.CreateJoin();
    static abstract Func<TCompositeModel, TTargetModel, TCompositeModel> GetMap();
    static abstract string SplitOn { get; }
}

public interface ILinkModel<TJoin, TLeftDataModel, TRightDataModel> : IJoinModel<TLeftDataModel, TRightDataModel>
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