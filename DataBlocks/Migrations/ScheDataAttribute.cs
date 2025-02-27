namespace DataBlocks.Migrations;

[AttributeUsage(AttributeTargets.Property)]
public class ScheDataAttribute : Attribute
{
    public string Name { get; }
    public bool IsNullable { get; } = false;
    public bool IsPrimaryKey { get; protected set; } = false;

    public ScheDataAttribute(string name)
    {
        Name = name;
    }

    public ScheDataAttribute(string name, bool isNullable)
    {
        Name = name;
        IsNullable = isNullable;
    }
}

public class ScheKeyAttribute : ScheDataAttribute
{
    public ScheKeyAttribute(string name) : base(name, false)
    {
        IsPrimaryKey = true;
    }
}