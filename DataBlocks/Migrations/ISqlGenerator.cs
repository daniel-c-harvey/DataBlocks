namespace DataBlocks.Migrations;

public interface ISqlGenerator
{
    // Core DDL generation methods
    string GenerateCreateSchema(string schema);
    string GenerateCreateTempTable(string schema, string tableName);
    string GenerateCreateTable(string schema, string tableName, IEnumerable<ColumnInfo> columns);
    string GenerateAddColumn(string schema, string tableName, ColumnInfo column);
    string GenerateModifyColumn(string schema, string tableName, ColumnInfo column);
    string GenerateDropUnusedColumns(string schema, string tableName, IEnumerable<string> validColumns);
    string GenerateCleanup();

    // Type mapping method
    string MapCSharpType(Type type);
}