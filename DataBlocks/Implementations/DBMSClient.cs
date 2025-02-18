using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    internal abstract class DBMSClient<TClient, TDatabase>
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
