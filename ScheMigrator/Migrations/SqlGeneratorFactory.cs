using DataBlocks.Migrations;

namespace ScheMigrator.Migrations
{
    internal static class SqlGeneratorFactory
    {
        private static Dictionary<SqlImplementation, Dictionary<string, Dictionary<string,ISqlGenerator>>> Implementations = new();

        internal static ISqlGenerator Build(SqlImplementation sqlImplementation, string schema, string tableName)
        {
            ISqlGenerator sqlGenerator;
            if(!Implementations.TryGetValue(sqlImplementation, out var schemaImplementations))
            {
                // Build the implementation
                sqlGenerator = NewInstance(sqlImplementation, schema, tableName);

                Implementations.Add(sqlImplementation, new Dictionary<string, Dictionary<string, ISqlGenerator>>
                {
                    { schema, new Dictionary<string, ISqlGenerator>
                        {
                            { tableName, sqlGenerator }
                        }
                    }
                });
            }
            else if (!schemaImplementations.TryGetValue(schema, out var tableNameImplementations))
            {
                // Build the implementation
                sqlGenerator = NewInstance(sqlImplementation, schema, tableName);

                schemaImplementations.Add(schema, new Dictionary<string, ISqlGenerator>
                {
                    { tableName, sqlGenerator }
                });
            }
            else if (!tableNameImplementations.TryGetValue(tableName, out var generator))
            {
                // Build the implementation
                sqlGenerator = NewInstance(sqlImplementation, schema, tableName);

                tableNameImplementations.Add(tableName, sqlGenerator);
            }
            else
            {
                sqlGenerator = generator;
            }

            return sqlGenerator;
        }

        private static ISqlGenerator NewInstance(SqlImplementation sqlImplementation, string schema, string tableName)
        {
            switch (sqlImplementation)
            {
                case SqlImplementation.PostgreSQL:
                    return new PostgreSqlGenerator(schema, tableName);
                case SqlImplementation.SQLite:
                    return new SqliteGenerator(schema, tableName);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
