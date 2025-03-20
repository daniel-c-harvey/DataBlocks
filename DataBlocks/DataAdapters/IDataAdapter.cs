using System.Linq.Expressions;
using DataBlocks.DataAccess;
using NetBlocks.Models;

namespace DataBlocks.DataAdapters
{
    public interface IDataAdapter<TModel> where TModel : IModel
    {
        Task<ResultContainer<IEnumerable<TModel>>> GetPage(int page, int pageSize);
        Task<ResultContainer<TModel>> GetByID(long id);
        Task<ResultContainer<IEnumerable<TModel>>> GetByPredicate(Expression<Func<TModel, bool>> predicate);
        Task<Result> Insert(TModel model);
        Task<Result> Insert(IEnumerable<TModel> models);
        Task<Result> Update(TModel model);
        Task<Result> Delete(TModel model);
    }
}
