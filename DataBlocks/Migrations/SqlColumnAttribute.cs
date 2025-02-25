namespace DataBlocks.Migrations;

[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnAttribute : Attribute
{
    public string Name { get; }
    public bool IsPrimaryKey { get; } = false;
    public bool IsNullable { get; } = false;

    public SqlColumnAttribute(string name)
    {
        Name = name;
    }

    public SqlColumnAttribute(string name, bool isNullable)
    {
        Name = name;
        IsNullable = isNullable;
    }
    
    public SqlColumnAttribute(string name, bool isPrimaryKey, bool isNullable)
    {
        Name = name;
        IsPrimaryKey = isPrimaryKey;
        IsNullable = isNullable;
    }
}