using System.Linq.Expressions;
using NetBlocks.Models;

namespace DataBlocks.DataAccess
{
    public interface IQueryBuilder<TDatabase>
    {
        IDataQuery<TDatabase, ResultContainer<TModel>> BuildRetrieveById<TModel>(string collection, long key) where TModel : IModel;
        IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(string collection, int pageIndex, int pageSize) where TModel : IModel;
        IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(string collection, Expression<Func<TModel, bool>> predicate) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildInsert<TModel>(string collection, TModel value) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildReplace<TModel>(string collection, TModel value) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildDelete<TModel>(string collection, TModel value) where TModel : IModel;
    }
}