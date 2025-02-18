using MongoDB.Driver;

namespace DataAccess;

internal abstract class DatabaseConnection<TDatabase> : IDatabaseConnection<TDatabase>
{
    
    public string DatabaseName { get; }
    public abstract IDatabase<TDatabase> Database { get; }

    public DatabaseConnection(string databaseName)
    {

        DatabaseName = databaseName;
    }
}
