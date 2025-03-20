using DataAccess;
using NetBlocks.Models;

namespace DataBlocks.DataAccess;

public abstract class DataAccess<TClient, TDatabase> : IDataAccess<TDatabase>
{
    protected DBMSClient<TClient, TDatabase>? DBClient { get; set; }
    public DataAccess(string connectionString, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) { throw new ArgumentNullException(nameof(connectionString)); }
        if (string.IsNullOrWhiteSpace(databaseName)) { throw new ArgumentNullException(nameof(databaseName)); }
    }

    public async Task<ResultContainer<string>> GetConnectionString()
    {
        if (DBClient == null) { throw new ArgumentNullException(nameof(DBClient)); }

        return new ResultContainer<string>(DBClient.ConnectionString);
    }

    public async Task<ResultContainer<string>> GetDatabaseName()
    {
        if (DBClient?.Connection == null) { throw new ArgumentNullException(nameof(DBClient.Connection)); }

        return new ResultContainer<string>(DBClient.Connection.DatabaseName);
    }

    // public abstract ResultContainer<IEnumerable<string>> GetDatabaseNames();

    // public abstract Result ChangeConnection(Connection connection, string databaseName);

    public async Task<ResultContainer<IEnumerable<TModel>>> ExecQuery<TModel>(IDataQuery<TDatabase, ResultContainer<IEnumerable<TModel>>> query)
    {
        if (DBClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

        return await DBClient.Connection.Database.ExecQuery(query);
    }

    public async Task<ResultContainer<TModel>> ExecQueryOne<TModel>(IDataQuery<TDatabase, ResultContainer<TModel>> query)
    {
        if (DBClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

        return await DBClient.Connection.Database.ExecQuery(query);
    }

    public async Task<Result> ExecNonQuery(IDataQuery<TDatabase, Result> query)
    {
        if (DBClient?.Connection == null) { throw new ArgumentNullException("Connection"); }

        return await DBClient.Connection.Database.ExecQuery(query);
    }
}
