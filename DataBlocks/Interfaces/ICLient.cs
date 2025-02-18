using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    internal interface ICLient<TClient>
    {
        //TClient StrongClient { get; }
        IDatabase<TDatabase> GetDatabase<TDatabase>(string databaseName);
        IEnumerable<string> GetDatabaseNames();

    }
}
