namespace DataBlocks.ConnectionManager;

public class ConnectionInfo
{
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    public bool IsValid => !string.IsNullOrEmpty(Host) &&
                           !string.IsNullOrEmpty(Username) &&
                           !string.IsNullOrEmpty(Password) &&
                           !string.IsNullOrEmpty(Database);

    public string ToConnectionString()
    {
        return $"Host={Host};Username={Username};Password={Password};Database={Database}";
    }


    public override bool Equals(object? obj)
    {
        if (obj is not ConnectionInfo other)
            return false;

        return Host == other.Host &&
               Username == other.Username &&
               Password == other.Password &&
               Database == other.Database;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Host, Username, Password, Database);
    }

    public void Reset()
    {
        Host = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        Database = string.Empty;
    }


    public static bool operator ==(ConnectionInfo? left, ConnectionInfo? right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(ConnectionInfo? left, ConnectionInfo? right)
    {
        return !(left == right);
    }
}