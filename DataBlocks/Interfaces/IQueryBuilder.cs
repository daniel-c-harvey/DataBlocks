using NetBlocks.Models;

namespace DataAccess
{
    public interface IQueryBuilder<TDatabase>
    {
        IDataQuery<TDatabase, ResultContainer<TModel>> BuildRetrieve<TModel>(string collection) where TModel : IModel;
        IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(string collection, int pageIndex, int pageSize) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildInsert<TModel>(string collection, TModel value) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildReplace<TModel>(string collection, TModel value) where TModel : IModel;

        IDataQuery<TDatabase, Result> BuildDelete<TModel>(string collection, TModel value) where TModel : IModel;
    }
}