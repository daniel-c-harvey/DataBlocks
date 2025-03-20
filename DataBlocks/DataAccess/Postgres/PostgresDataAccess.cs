namespace DataBlocks.DataAccess.Postgres;

public class PostgresDataAccess : DataAccess<IPostgresClient, IPostgresDatabase>
{
    public PostgresDataAccess(string connectionString, string databaseName)
    : base(connectionString, databaseName)
    {
        DBClient = new PostgresDBMSClient(connectionString);
        DBClient.SetDatabase(databaseName);
    }
}
