using DataAccess;
using NetBlocks.Models;

namespace DataBlocks.DataAccess
{
    internal class DatabaseContainer<TDatabase> : IDatabase<TDatabase>
    {
        public TDatabase Database { get; }

        internal DatabaseContainer(TDatabase database)
        {
            Database = database;
        }

        public async Task<IEnumerable<T>> ExecQuery<T>(IDataQuery<TDatabase, IEnumerable<T>> query)
        {
            return await query.Query(Database);
        }

        public async Task<TModel> ExecQuery<TModel>(IDataQuery<TDatabase, TModel> query)
        {
            return await query.Query(Database);
        }

        public async Task<Result> ExecQuery<TModel>(IDataQuery<TDatabase, Result> query)
        {
            return await query.Query(Database);
        }
    }
}
