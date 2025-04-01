using System.Collections.Generic;

namespace ScheMigrator.Migrations;

public enum SqlImplementation
{
    PostgreSQL,
    SQLite
}

public static class SqlImplementationUtil
{
    public static IList<SqlImplementation> Implementations = new[] { SqlImplementation.PostgreSQL, SqlImplementation.SQLite };
}