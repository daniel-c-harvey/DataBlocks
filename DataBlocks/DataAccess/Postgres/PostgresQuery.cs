namespace DataBlocks.DataAccess.Postgres;

public class PostgresQuery<TReturn> : IDataQuery<IPostgresDatabase, TReturn>
{
    public Func<IPostgresDatabase, Task<TReturn>> Query { get; }

    public PostgresQuery(Func<IPostgresDatabase, Task<TReturn>> query)
    {
        Query = query;
    }
}
