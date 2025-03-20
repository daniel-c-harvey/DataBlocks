using Npgsql;

namespace DataBlocks.DataAccess.Postgres;

public interface IPostgresDatabase
{
    NpgsqlConnection Connection { get; }
    string DatabaseName { get; }
}
