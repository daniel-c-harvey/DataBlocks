using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBlocks.Migrations;

namespace DataBlocks.ConnectionManager
{
    public static class ConnectionFactory
    {
        public static ISqlConnection MakeConnection(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null || !connectionInfo.IsValid)
            {
                throw new ArgumentNullException(nameof(connectionInfo));
            }

            switch (connectionInfo.Implementation)
            {
                case SqlImplementation.PostgreSQL:
                    return new PostgresConnection(connectionInfo);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
