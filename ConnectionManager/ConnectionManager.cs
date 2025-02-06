using System.Text.Json;
using NetBlocks.Models;

namespace DataBlocks.ConnectionManager;

public class ConnectionManager
{
    private readonly string _configPath = Path.Combine(Directory.GetCurrentDirectory(), "connections.json");

    public async Task SaveConnectionAsync(ConnectionInfo connection)
    {
        var connections = await LoadConnectionsAsync();
        connections.Add(connection);
        await SaveConnectionsAsync(connections);
    }

    public async Task RemoveConnectionAsync(IList<ConnectionInfo> connections, ConnectionInfo connection)
    {
        connections.Remove(connection);
        await SaveConnectionsAsync(connections);
    }

    public async Task<IList<ConnectionInfo>> LoadConnectionsAsync()
    {
        if (!File.Exists(_configPath))
            return new List<ConnectionInfo>();

        string json = await File.ReadAllTextAsync(_configPath);
        return JsonSerializer.Deserialize<List<ConnectionInfo>>(json) 
               ?? new List<ConnectionInfo>();
    }

    private async Task SaveConnectionsAsync(IList<ConnectionInfo> connections)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(connections, options);
        await File.WriteAllTextAsync(_configPath, json);
    }

    public async Task<ResultContainer<PostgresConnection>> ConnectAsync(ConnectionInfo connectionInfo)
    {
        var connection = new PostgresConnection(connectionInfo);
        await connection.ConnectAsync();
        return ResultContainer<PostgresConnection>.CreatePassResult(connection);
    }
}