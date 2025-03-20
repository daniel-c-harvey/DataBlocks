using NetBlocks.Models;

namespace DataBlocks.DataAccess
{
    // public interface IDatabase<TDatabase>
    // {
    //     IEnumerable<TModel> ExecQuery<TModel>(IDataQuery<TDatabase, IEnumerable<TModel>> query);
    //     TModel ExecQuery<TModel>(IDataQuery<TDatabase, TModel> query);
    //     Result ExecQuery<TModel>(IDataQuery<TDatabase, Result> query);
    // }

    public interface IDatabase<TDatabase>
    {
        Task<IEnumerable<TModel>> ExecQuery<TModel>(IDataQuery<TDatabase, IEnumerable<TModel>> query);
        Task<TModel> ExecQuery<TModel>(IDataQuery<TDatabase, TModel> query);
        Task<Result> ExecQuery<TModel>(IDataQuery<TDatabase, Result> query);
    }
}
