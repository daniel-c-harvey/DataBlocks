namespace DataBlocks.Migrations;

public class ColumnInfo
{
    public string Name { get; set; }
    public Type PropertyType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
}