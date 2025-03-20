using DataAccess;

namespace DataBlocks.DataAccess
{
    internal interface ICLient<TClient>
    {
        //TClient StrongClient { get; }
        IDatabase<TDatabase> GetDatabase<TDatabase>(string databaseName);
        IEnumerable<string> GetDatabaseNames();

    }
}
