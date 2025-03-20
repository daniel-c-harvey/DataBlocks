using DataAccess;

namespace DataBlocks.DataAccess.Postgres;

public class PostgresDBMSClient : DBMSClient<IPostgresClient, IPostgresDatabase>
{
    public PostgresDBMSClient(string connectionString)
        : base(connectionString)
    {
        Client = new PostgresClient(connectionString);
    }

    public override void SetDatabase(string databaseName)
    {
        Connection = new PostgresDatabaseConnection(ConnectionString, databaseName);
    }
}
