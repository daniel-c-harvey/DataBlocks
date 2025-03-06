
namespace DataBlocks.ConnectionManager
{
    public interface ISqlConnection : IDisposable
    {
        Task ConnectAsync();
        Task ExecuteScriptAsync(string sql);
    }
}