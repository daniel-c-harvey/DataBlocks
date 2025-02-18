using NetBlocks.Models;

namespace DataAccess
{
    public interface IDataAdapter<TModel> where TModel : IModel
    {
        ResultContainer<IEnumerable<TModel>> GetPage(int page, int pageSize);
        ResultContainer<TModel> GetByID(int id);
        Result Insert(TModel model);
        Result Insert(IEnumerable<TModel> models);
        Result Update(TModel model);
        Result Delete(TModel model);
    }
}
