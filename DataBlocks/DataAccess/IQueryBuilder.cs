using System.Linq.Expressions;
using NetBlocks.Models;

namespace DataBlocks.DataAccess
{
    public interface IQueryBuilder<TDatabase>
    {
        IDataQuery<TDatabase, ResultContainer<TModel>> BuildRetrieveById<TModel>(DataSchema target, long key) where TModel : IModel;
        IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target, int pageIndex, int pageSize) where TModel : IModel;
        IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> BuildRetrieve<TModel>(DataSchema target, Expression<Func<TModel, bool>> predicate) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildInsert<TModel>(DataSchema target, TModel value) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildReplace<TModel>(DataSchema target, TModel value) where TModel : IModel;
        IDataQuery<TDatabase, Result> BuildDelete<TModel>(DataSchema target, TModel value) where TModel : IModel;
    }
}