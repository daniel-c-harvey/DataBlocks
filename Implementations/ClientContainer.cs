//using DataAccess;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DataAccess
//{
//    internal class ClientContainer<TClient> : ICLient<TClient>
//    {
//        //public TClient StrongClient { get; }

//        public ClientContainer(TClient client)
//        {
//            StrongClient = client;
//        }

//        public IDatabase<TDatabase> GetDatabase<TDatabase>(string databaseName)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<string> GetDatabaseNames()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
