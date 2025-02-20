namespace DataBlocks.Migrations;

public class ScheModelAttribute : Attribute
{
    public SqlImplementation SqlImplementation { get; }

    public ScheModelAttribute(SqlImplementation sqlImplementation)
    {
        SqlImplementation = sqlImplementation;
    }
}