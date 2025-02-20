
using DataBlocks.Migrations;

namespace ScheMigrator.DDL
{
    internal static class SqlGeneratorFactory
    {
        private static Dictionary<SqlImplementation, ISqlGenerator> Implementations = new();

        internal static ISqlGenerator Build(SqlImplementation sqlImplementation)
        {
            if(!Implementations.TryGetValue(sqlImplementation, out var sqlGenerator))
            {
                sqlGenerator = NewInstance(sqlImplementation);
            }

            return sqlGenerator;

        }

        private static ISqlGenerator NewInstance(SqlImplementation sqlImplementation)
        {
            switch (sqlImplementation)
            {
                case SqlImplementation.PostgreSQL:
                    return new PostgreSqlGenerator();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
