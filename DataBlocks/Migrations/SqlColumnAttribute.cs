namespace DataBlocks.Migrations;

[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnAttribute : Attribute
{
    public string Name { get; }
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; } = true;

    public SqlColumnAttribute(string name = null)
    {
        Name = name;
    }
}