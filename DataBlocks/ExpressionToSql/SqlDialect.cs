namespace ExpressionToSql
{
    /// <summary>
    /// Interface defining SQL dialect-specific operations
    /// </summary>
    public interface ISqlDialect
    {
        /// <summary>
        /// Format the TAKE/LIMIT clause
        /// </summary>
        string FormatTake(int count);

        /// <summary>
        /// Format the OFFSET/SKIP clause
        /// </summary>
        string FormatOffset(int offset);

        /// <summary>
        /// Determine if OFFSET should come before LIMIT in this dialect
        /// </summary>
        bool OffsetBeforeLimit { get; }

        /// <summary>
        /// Format the combined LIMIT and OFFSET for optimal dialect-specific syntax
        /// </summary>
        /// <param name="limit">The limit value</param>
        /// <param name="offset">The offset value</param>
        /// <returns>Formatted SQL for limit and offset</returns>
        string FormatLimitOffset(int limit, int offset);

        /// <summary>
        /// Format parameter names
        /// </summary>
        string FormatParameter(string parameterName);

        /// <summary>
        /// Escape identifier names (tables, columns)
        /// </summary>
        string EscapeIdentifier(string identifier);

        /// <summary>
        /// Get the default schema name for this dialect
        /// </summary>
        string DefaultSchema { get; }

        /// <summary>
        /// Format a schema name properly for this dialect
        /// </summary>
        string FormatSchemaName(string schema);
    }

    /// <summary>
    /// SQL Server dialect implementation
    /// </summary>
    public class TSqlDialect : ISqlDialect
    {
        public string DefaultSchema => "dbo";

        public bool OffsetBeforeLimit => true;

        public string FormatTake(int count)
        {
            return $"TOP {count}";
        }

        public string FormatOffset(int offset)
        {
            return $"OFFSET {offset} ROWS";
        }

        public string FormatLimitOffset(int limit, int offset)
        {
            return $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";
        }

        public string FormatParameter(string parameterName)
        {
            return $"@{parameterName}";
        }

        public string EscapeIdentifier(string identifier)
        {
            return identifier.StartsWith("[") && identifier.EndsWith("]")
                ? identifier
                : $"[{identifier}]";
        }

        public string FormatSchemaName(string schema)
        {
            return EscapeIdentifier(schema);
        }
    }

    /// <summary>
    /// PostgreSQL dialect implementation
    /// </summary>
    public class PostgreSqlDialect : ISqlDialect
    {
        public string DefaultSchema => "public";

        public bool OffsetBeforeLimit => false;

        public string FormatTake(int count)
        {
            return $"LIMIT {count}";
        }

        public string FormatOffset(int offset)
        {
            return $"OFFSET {offset}";
        }

        public string FormatLimitOffset(int limit, int offset)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        public string FormatParameter(string parameterName)
        {
            return $"@{parameterName}";
        }

        public string EscapeIdentifier(string identifier)
        {
            return identifier.StartsWith("\"") && identifier.EndsWith("\"")
                ? identifier
                : $"\"{identifier}\"";
        }

        public string FormatSchemaName(string schema)
        {
            return EscapeIdentifier(schema);
        }
    }

    /// <summary>
    /// SQLite dialect implementation
    /// </summary>
    public class SQLiteDialect : ISqlDialect
    {
        public string DefaultSchema => "";  // SQLite doesn't use schemas

        public bool OffsetBeforeLimit => false;

        public string FormatTake(int count)
        {
            return $"LIMIT {count}";
        }

        public string FormatOffset(int offset)
        {
            return $"OFFSET {offset}";
        }

        public string FormatLimitOffset(int limit, int offset)
        {
            return $"LIMIT {limit} OFFSET {offset}";
        }

        public string FormatParameter(string parameterName)
        {
            return $"@{parameterName}";
        }

        public string EscapeIdentifier(string identifier)
        {
            return identifier.StartsWith("\"") && identifier.EndsWith("\"")
                ? identifier
                : $"\"{identifier}\"";
        }

        public string FormatSchemaName(string schema)
        {
            // SQLite doesn't support schemas, so we ignore this
            return "";
        }
    }
} 