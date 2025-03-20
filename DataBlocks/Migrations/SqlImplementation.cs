namespace DataBlocks.Migrations;

public enum SqlImplementation
{
    PostgreSQL,
    SQLite
}

public static class SqlImplementationUtil
{
    public static IList<SqlImplementation> Implementations = [SqlImplementation.PostgreSQL, SqlImplementation.SQLite];
}