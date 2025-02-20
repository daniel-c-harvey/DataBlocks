using System.Reflection;
using System.Text;
using DataBlocks.Migrations;

namespace ScheMigrator.DDL;

public static class DDLGenerator
{
    public static string GenerateDDL(Type type, ISqlGenerator sqlGenerator, string schema = "public")
    {
        var tableName = type.Name.ToLower();
        var columns = GetColumns(type);

        if (!columns.Any())
        {
            throw new ArgumentException($"No columns found in type {type.Name}. Make sure properties are marked with [Column] attribute.");
        }

        var sb = new StringBuilder();

        // Generate DDL in steps using the SQL generator
        sb.AppendLine(sqlGenerator.GenerateCreateSchema(schema));
        sb.AppendLine();

        sb.AppendLine(sqlGenerator.GenerateCreateTempTable(schema, tableName));
        sb.AppendLine();

        sb.AppendLine(sqlGenerator.GenerateCreateTable(schema, tableName, columns));
        sb.AppendLine();

        // Add new columns
        foreach (var column in columns)
        {
            sb.AppendLine(sqlGenerator.GenerateAddColumn(schema, tableName, column));
            sb.AppendLine();
        }

        // Modify existing columns
        foreach (var column in columns)
        {
            sb.AppendLine(sqlGenerator.GenerateModifyColumn(schema, tableName, column));
            sb.AppendLine();
        }

        // Drop unused columns
        sb.AppendLine(sqlGenerator.GenerateDropUnusedColumns(schema, tableName, columns.Select(c => c.Name)));
        sb.AppendLine();

        // Cleanup
        sb.AppendLine(sqlGenerator.GenerateCleanup());

        return sb.ToString();
    }

    private static IEnumerable<ColumnInfo> GetColumns(Type type)
    {
        const string COLUMN_ATTR = "SqlColumnAttribute";
        
        return type.GetProperties()
            .Select(p => (Property: p, Attribute: p.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name == COLUMN_ATTR)))
            .Where(x => x.Attribute != null)
            .Select(x => 
            {
                var attrType = x.Attribute!.GetType();
                return new ColumnInfo
                {
                    Name = (attrType.GetProperty("Name")?.GetValue(x.Attribute) as string) 
                        ?? x.Property.Name.ToLower(),
                    PropertyType = x.Property.PropertyType,
                    IsPrimaryKey = (bool?)attrType.GetProperty("IsPrimaryKey")?.GetValue(x.Attribute) ?? false,
                    IsNullable = (bool?)attrType.GetProperty("IsNullable")?.GetValue(x.Attribute) ?? true
                };
            });
    }
}
