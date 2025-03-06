namespace DataBlocks.Migrations;

public enum SqlImplementation
{
    PostgreSQL
}

public static class SqlImplementationUtil
{
    public static IList<SqlImplementation> Implementations = [SqlImplementation.PostgreSQL];
}