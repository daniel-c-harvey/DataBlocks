using Microsoft.Data.Sqlite;

namespace DataBlocks.ConnectionManager;

public class SQLiteConnection : ISqlConnection
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public SQLiteConnection(ConnectionInfo connectionInfo)
    {
        _connectionString = connectionInfo.ToConnectionString();   
    }
    
    public async Task ConnectAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();
    }

    public async Task ExecuteScriptAsync(string sql)
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not established. Call ConnectAsync first.");
        
        await using var command = new SqliteCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}