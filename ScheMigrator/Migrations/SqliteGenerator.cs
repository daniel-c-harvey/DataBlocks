using System.Text;
using DataBlocks.Migrations;

namespace ScheMigrator.Migrations;

public class SqliteGenerator : ISqlGenerator
{
    private static readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>
    {
        { "Int32", "INTEGER" },
        { "Int64", "INTEGER" },
        { "String", "TEXT" },
        { "DateTime", "TEXT" },
        { "Boolean", "INTEGER" },
        { "Decimal", "REAL" },
        { "Double", "REAL" },
        { "Single", "REAL" },
        { "Guid", "TEXT" },
        { "Byte[]", "BLOB" }
    };
    
    private string _tableName;
    private string _newTableName;

    public SqliteGenerator(string _, string tableName)
    {
        _tableName = tableName;
        _newTableName = $"new_{tableName}";
    }

    public static string MapCSharpType(Type type)
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

    public string GenerateCreateSchema()
    {
        // SQLite doesn't support schemas
        return string.Empty;
    }

    public string GenerateCreateTempTable()
    {
        return string.Empty;
    }

    public string GenerateCreateTable(IEnumerable<ColumnInfo> columns)
    {
        var columnDefinitions = columns.Select(c =>
            $"            {c.Name} {MapCSharpType(c.PropertyType)}" +
            $"{(c.IsPrimaryKey ? " PRIMARY KEY" : "")}" +
            $"{(!c.IsNullable ? " NOT NULL" : "")}");

        return $"""
                CREATE TABLE IF NOT EXISTS {_tableName} (
                    {string.Join(",\n", columnDefinitions)}
                );
                """;
    }

    public string GenerateAddColumn(ColumnInfo column)
    {
        return $"""
                ALTER TABLE {_tableName}
                ADD COLUMN {column.Name} {MapCSharpType(column.PropertyType)}{(!column.IsNullable ? " NOT NULL DEFAULT 0" : "")};
                """;
    }

    public string GenerateModifyColumn(ColumnInfo column)
    {
        // We'll just return a stub - the actual schema update will be handled by table recreation
        return $"-- Column {column.Name} needs modification - will be handled by table recreation";
    }

    public string GenerateDropUnusedColumns(IEnumerable<ColumnInfo> validColumns)
    {
        // We'll just return a stub - the actual schema update will be handled by recreating the table
        return "-- Dropping unused columns will be handled by table recreation";
    }

    public string GenerateCleanup()
    {
        return string.Empty;
    }

    public string GenerateOpenBlock()
    {
        return $"""
                -- Begin SQLite migration
                PRAGMA foreign_keys = OFF;
                """;
    }

    public string GenerateCloseBlock()
    {
        return $"""
                -- End SQLite migration
                PRAGMA foreign_keys = ON;
                """;
    }
    
    public string GenerateTableRecreation(IEnumerable<ColumnInfo> columns)
    {
        var columnDefinitions = columns.Select(c =>
            $"            {c.Name} {MapCSharpType(c.PropertyType)}" +
            $"{(c.IsPrimaryKey ? " PRIMARY KEY" : "")}" +
            $"{(!c.IsNullable ? " NOT NULL" : "")}");
            
        var columnNames = string.Join(", ", columns.Select(c => c.Name));

        return $"""
                -- Create new table with updated schema
                CREATE TABLE {_newTableName} (
                    {string.Join(",\n", columnDefinitions)}
                );
                
                -- Copy data from old table to new table (only for columns that exist in both)
                INSERT INTO {_newTableName} ({columnNames})
                SELECT {columnNames} FROM {_tableName}
                WHERE EXISTS (SELECT 1 FROM {_tableName});
                
                -- Drop the old table
                DROP TABLE {_tableName};
                
                -- Rename the new table to the original name
                ALTER TABLE {_newTableName} RENAME TO {_tableName};
                """;
    }
} 