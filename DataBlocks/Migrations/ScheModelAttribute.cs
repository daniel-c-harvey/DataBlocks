namespace DataBlocks.Migrations;

public class ScheModelAttribute : Attribute
{
    public SqlImplementation SqlImplementation { get; }
}