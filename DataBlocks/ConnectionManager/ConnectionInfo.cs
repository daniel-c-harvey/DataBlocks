using System.Text.Json.Serialization;
using ScheMigrator.Migrations;
using NetBlocks.Interfaces;

namespace DataBlocks.ConnectionManager;

public class ConnectionInfo : ICloneable<ConnectionInfo>
{
    public int Id { get; }
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public SqlImplementation Implementation { get; set; } = SqlImplementation.PostgreSQL;

    [JsonIgnore]
    public bool IsValid
    {
        get
        {
            switch (Implementation)
            {
                case SqlImplementation.PostgreSQL:
                    return !string.IsNullOrEmpty(Host) &&
                           !string.IsNullOrEmpty(Username) &&
                           !string.IsNullOrEmpty(Password) &&
                           !string.IsNullOrEmpty(Database) &&
                           Enum.IsDefined(typeof(SqlImplementation), Implementation);
                case SqlImplementation.SQLite:
                    return !string.IsNullOrEmpty(Database) &&
                           Enum.IsDefined(typeof(SqlImplementation), Implementation);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public ConnectionInfo(int id)
    {
        Id = id;
    }

    public string ToConnectionString()
    {
        switch (Implementation)
        {
            case SqlImplementation.PostgreSQL:
                return $"Host={Host};Username={Username};Password={Password};Database={Database}";
            case SqlImplementation.SQLite:
                return $"Data Source={Database};Password={Password}Mode=ReadWrite;";
            default:
                throw new NotImplementedException();
        }
    }
    
    public string ToDisplayString()
    {
        return $"{Host} - {Username} - {Database}";
    }


    public override bool Equals(object? obj)
    {
        if (obj is not ConnectionInfo other)
            return false;

        return Id == other.Id && 
               Host == other.Host &&
               Username == other.Username &&
               Password == other.Password &&
               Database == other.Database &&
               Implementation == other.Implementation;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Host, Username, Password, Database, Implementation);
    }

    public void Reset()
    {
        Host = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        Database = string.Empty;
        Implementation = SqlImplementation.PostgreSQL;
    }

    public ConnectionInfo Clone()
    {
        return new ConnectionInfo(this.Id)
        {
            Host = this.Host,
            Username = this.Username,
            Password = this.Password,
            Database = this.Database,
            Implementation = this.Implementation
        };
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