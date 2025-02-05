namespace DataBlocks.ConnectionManager;

public class ConnectionInfo
{
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    public string ToConnectionString()
    {
        return $"Host={Host};Username={Username};Password={Password};Database={Database}";
    }
} 