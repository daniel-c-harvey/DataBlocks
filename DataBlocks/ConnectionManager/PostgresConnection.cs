using ScheMigrator.Migrations;
using Npgsql;

namespace DataBlocks.ConnectionManager;

public class PostgresConnection : ISqlConnection
{
    private readonly string _connectionString;
    private NpgsqlConnection? _connection;

    public PostgresConnection(ConnectionInfo connectionInfo)
    {
        _connectionString = connectionInfo.ToConnectionString();
    }

    public async Task ConnectAsync()
    {
        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync();
    }

    public async Task ExecuteScriptAsync(string sql)
    {
        if (_connection == null)
            throw new InvalidOperationException("Connection not established. Call ConnectAsync first.");

        await using var command = new NpgsqlCommand(sql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
} 