using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScheMigrator.Migrations
{
    public class PostgreSqlGenerator : ISqlGenerator
    {
        private static readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>
    {
        { "Int32", "integer" },
        { "Int64", "bigint" },
        { "String", "text" },
        { "DateTime", "timestamp" },
        { "DateTimeOffset", "timestamptz" },
        { "Boolean", "boolean" },
        { "Decimal", "numeric" },
        { "Double", "double precision" },
        { "Single", "real" },
        { "Guid", "uuid" },
        { "Byte[]", "bytea" }
    };

        private string _schema;
        private string _tableName;
        private string _tempTableName;

        public PostgreSqlGenerator(string schema, string tableName)
        {
            _schema = schema;
            _tableName = tableName;
            _tempTableName = $"temp_table_info_{schema}_{tableName}";
        }

        public static string MapCSharpType(Type type)
        {
            var typeName = type.Name;
            if (Regex.Match(type.Name, @"Nullable`\d").Success)
            {
                var underlyingType = type.GetGenericArguments()[0];
                typeName = underlyingType.Name;
            }

            if (!_typeMap.TryGetValue(typeName, out string dbType))
            {
                throw new ArgumentException($"Unsupported type: {typeName}");
            }
            return dbType;
        }

        public string GenerateCreateSchema()
        {
            return $"CREATE SCHEMA IF NOT EXISTS {_schema};";
        }

        public string GenerateCreateTempTable()
        {
            return $"""
                CREATE TEMP TABLE IF NOT EXISTS {_tempTableName} AS
                SELECT column_name, data_type, is_nullable
                FROM information_schema.columns 
                WHERE table_schema = '{_schema}' 
                AND table_name = '{_tableName}';
                """;
        }

        public string GenerateCreateTable(IEnumerable<ColumnInfo> columns)
        {
            var columnDefinitions = columns.Select(c =>
                $"            {c.Name} {MapCSharpType(c.PropertyType)}" +
                $"{(c.IsPrimaryKey ? " PRIMARY KEY" : "")}" +
                $"{(!c.IsNullable ? " NOT NULL" : "")}");

            return $"""
                CREATE TABLE IF NOT EXISTS {_schema}.{_tableName} (
                    {string.Join(",\n", columnDefinitions)}
                    );
                """;
        }

        public string GenerateAddColumn(ColumnInfo column)
        {
            return $"""
                ALTER TABLE {_schema}.{_tableName}
                    ADD COLUMN IF NOT EXISTS {column.Name} {MapCSharpType(column.PropertyType)}{(!column.IsNullable ? " NOT NULL" : "")};
                """;
        }

        public string GenerateModifyColumn(ColumnInfo column)
        {
            return $"""
                IF EXISTS (
                    SELECT 1 FROM {_tempTableName}
                    WHERE column_name = '{column.Name}'
                    AND (
                        data_type != '{MapCSharpType(column.PropertyType).ToLower()}'
                        OR is_nullable != '{(column.IsNullable ? "YES" : "NO")}'
                    )
                ) THEN
                    ALTER TABLE {_schema}.{_tableName}
                    ALTER COLUMN {column.Name} TYPE {MapCSharpType(column.PropertyType)} USING {column.Name}::{MapCSharpType(column.PropertyType)},
                    ALTER COLUMN {column.Name} {(column.IsNullable ? "DROP NOT NULL" : "SET NOT NULL")};
                END IF;
                """;
        }

        public string GenerateDropUnusedColumns(IEnumerable<ColumnInfo> validColumns)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"""
                       FOR _col IN (
                           SELECT column_name FROM {_tempTableName}
                           WHERE column_name NOT IN ({string.Join(", ", validColumns.Select(c => $"'{c.Name}'"))})
                       )
                       LOOP
                           EXECUTE format('ALTER TABLE %I.%I DROP COLUMN %I', '{_schema}', '{_tableName}', _col.column_name);
                       END LOOP;
                       """);

            return sb.ToString();
        }

        public string GenerateCleanup()
        {
            return $"DROP TABLE IF EXISTS {_tempTableName};";
        }

        public string GenerateOpenBlock()
        {
            return """
               DO $$
               DECLARE
                   _col record;
               BEGIN
               """;
        }

        public string GenerateCloseBlock()
        {
            return "END $$;";
        }

        public string GenerateTableRecreation(IEnumerable<ColumnInfo> columns)
        {
            // PostgreSQL supports ALTER TABLE operations directly, so we don't need
            // to recreate the table. This method is primarily for SQLite.
            return "-- PostgreSQL supports ALTER TABLE operations directly, no need for table recreation";
        }
    }
}