using System.Text;
using DataBlocks.Migrations;

namespace ScheMigrator.DDL;

public class PostgreSqlGenerator : ISqlGenerator
{
    private readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>
    {
        { "Int32", "integer" },
        { "Int64", "bigint" },
        { "String", "text" },
        { "DateTime", "timestamp" },
        { "Boolean", "boolean" },
        { "Decimal", "numeric" },
        { "Double", "double precision" },
        { "Single", "real" },
        { "Guid", "uuid" },
        { "Byte[]", "bytea" }
    };

    public string MapCSharpType(Type type)
    {
        var typeName = type.Name;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            typeName = Nullable.GetUnderlyingType(type)!.Name;
        }

        if (!_typeMap.TryGetValue(typeName, out string dbType))
        {
            throw new ArgumentException($"Unsupported type: {typeName}");
        }
        return dbType;
    }

    public string GenerateCreateSchema(string schema)
    {
        return $"CREATE SCHEMA IF NOT EXISTS {schema};";
    }

    public string GenerateCreateTempTable(string schema, string tableName)
    {
        return $@"CREATE TEMP TABLE IF NOT EXISTS temp_table_info AS
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_schema = '{schema}' 
AND table_name = '{tableName}';";
    }

    public string GenerateCreateTable(string schema, string tableName, IEnumerable<ColumnInfo> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"DO $$");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = '{schema}' AND table_name = '{tableName}') THEN");
        sb.AppendLine($"        CREATE TABLE {schema}.{tableName} (");

        var columnDefinitions = columns.Select(c =>
            $"            {c.Name} {MapCSharpType(c.PropertyType)}" +
            $"{(c.IsPrimaryKey ? " PRIMARY KEY" : "")}" +
            $"{(!c.IsNullable ? " NOT NULL" : "")}");

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.AppendLine("        );");
        sb.AppendLine("    END IF;");
        sb.AppendLine("END $$;");

        return sb.ToString();
    }

    public string GenerateAddColumn(string schema, string tableName, ColumnInfo column)
    {
        return $@"DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM temp_table_info WHERE column_name = '{column.Name}') THEN
        ALTER TABLE {schema}.{tableName}
        ADD COLUMN {column.Name} {MapCSharpType(column.PropertyType)}{(!column.IsNullable ? " NOT NULL" : "")};
    END IF;
END $$;";
    }

    public string GenerateModifyColumn(string schema, string tableName, ColumnInfo column)
    {
        return $@"DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM temp_table_info
        WHERE column_name = '{column.Name}'
        AND (
            data_type != '{MapCSharpType(column.PropertyType)}'::regtype::text
            OR is_nullable = 'NO' != {(!column.IsNullable).ToString().ToLower()}
        )
    ) THEN
        ALTER TABLE {schema}.{tableName}
        ALTER COLUMN {column.Name} TYPE {MapCSharpType(column.PropertyType)} USING {column.Name}::{MapCSharpType(column.PropertyType)},
        ALTER COLUMN {column.Name} {(column.IsNullable ? "DROP NOT NULL" : "SET NOT NULL")};
    END IF;
END $$;";
    }

    public string GenerateDropUnusedColumns(string schema, string tableName, IEnumerable<string> validColumns)
    {
        return $@"DO $$
DECLARE
    _col record;
BEGIN
    FOR _col IN
        SELECT column_name FROM temp_table_info
        WHERE column_name NOT IN ({string.Join(", ", validColumns.Select(c => $"'{c}'"))})
    LOOP
        EXECUTE 'ALTER TABLE {schema}.{tableName} DROP COLUMN ' || quote_ident(_col.column_name);
    END LOOP;
END $$;";
    }

    public string GenerateCleanup()
    {
        return "DROP TABLE temp_table_info;";
    }
}