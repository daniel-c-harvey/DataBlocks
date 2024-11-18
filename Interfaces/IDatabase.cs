using NetBlocks.Models;

namespace DataAccess
{
    internal interface IDatabase<TDatabase>
    {
        IEnumerable<TModel> ExecQuery<TModel>(IDataQuery<TDatabase, IEnumerable<TModel>> query);
        TModel ExecQuery<TModel>(IDataQuery<TDatabase, TModel> query);
        Result ExecQuery<TModel>(IDataQuery<TDatabase, Result> query);
    }
}
