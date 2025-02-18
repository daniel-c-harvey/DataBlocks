using NetBlocks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    internal class DatabaseContainer<TDatabase> : IDatabase<TDatabase>
    {
        internal TDatabase Database { get; }

        internal DatabaseContainer(TDatabase database)
        {
            Database = database;
        }

        IEnumerable<T> IDatabase<TDatabase>.ExecQuery<T>(IDataQuery<TDatabase, IEnumerable<T>> query)
        {
            return query.Query(Database);
        }

        TModel IDatabase<TDatabase>.ExecQuery<TModel>(IDataQuery<TDatabase, TModel> query)
        {
            return query.Query(Database);
        }

        public Result ExecQuery<TModel>(IDataQuery<TDatabase, Result> query)
        {
            return query.Query(Database);
        }
    }
}
