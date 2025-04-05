using Npgsql;

namespace DataBlocks.DataAccess.Postgres;

public class PostgresDatabase : IPostgresDatabase
{
    public NpgsqlConnection Connection { get; private init; }

    public string DatabaseName { get; private init; }

    public string ConnectionString { get; private init; }

    public PostgresDatabase(string connectionString, string databaseName)
    {
        Connection = new NpgsqlConnection(connectionString);
        Connection.Open();
        DatabaseName = databaseName;
        ConnectionString = connectionString;
    }    
}