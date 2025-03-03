using DataBlocks.Migrations;

namespace ScheMigrator.Migrations;

public interface ISqlGenerator
{
    // Core DDL generation methods
    string GenerateCreateSchema();
    string GenerateCreateTempTable();
    string GenerateCreateTable(IEnumerable<ColumnInfo> columns);
    string GenerateAddColumn(ColumnInfo column);
    string GenerateModifyColumn(ColumnInfo column);
    string GenerateDropUnusedColumns(IEnumerable<string> validColumns);
    string GenerateCleanup();
    
    // Block generation methods
    string GenerateOpenBlock();
    string GenerateCloseBlock();
}