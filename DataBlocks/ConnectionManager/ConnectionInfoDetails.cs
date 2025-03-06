using DataBlocks.Migrations;

namespace DataBlocks.ConnectionManager;

public class ConnectionInfoDetails
{
    public ConnectionInfo ConnectionInfo { get; set; }
    public bool IsConnected { get; set; }
    public ISqlConnection? Connection { get; set; }
}