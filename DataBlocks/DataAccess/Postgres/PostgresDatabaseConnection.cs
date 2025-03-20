using DataAccess;

namespace DataBlocks.DataAccess.Postgres;

internal class PostgresDatabaseConnection : DatabaseConnection<IPostgresDatabase>
{
    public override IDatabase<IPostgresDatabase> Database { get; }
    
    public PostgresDatabaseConnection(string connectionString, string databaseName) : base(databaseName)
    {
        Database = new DatabaseContainer<IPostgresDatabase>(new PostgresDatabase(connectionString, databaseName));
    }

}
