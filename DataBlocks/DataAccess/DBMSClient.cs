using DataAccess;

namespace DataBlocks.DataAccess
{
    public abstract class DBMSClient<TClient, TDatabase>
    {
        public string ConnectionString { get; }
        public TClient Client { get; protected set; }
        public IDatabaseConnection<TDatabase>? Connection { get; protected set; }

        public abstract void SetDatabase(string databaseName);

        public DBMSClient(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
