using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;

namespace DataBlocks.DataAdapters
{
    public class PostgresAdapter<TModel> : DataAdapter<IPostgresDatabase, PostgresDataAccess, PostgresQueryBuilder, TModel>
        where TModel : IModel
    {
        public PostgresAdapter(PostgresDataAccess dataAccess, PostgresQueryBuilder queryBuilder, DataSchema schema)
            : base(dataAccess, queryBuilder, schema)
        {
        }
    }
} 