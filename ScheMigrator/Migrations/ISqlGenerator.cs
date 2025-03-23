using System.Collections.Generic;

namespace ScheMigrator.Migrations
{
    public interface ISqlGenerator
    {
        // Core DDL generation methods
        string GenerateCreateSchema();
        string GenerateCreateTempTable();
        string GenerateCreateTable(IEnumerable<ColumnInfo> columns);
        string GenerateAddColumn(ColumnInfo column);
        string GenerateModifyColumn(ColumnInfo column);
        string GenerateDropUnusedColumns(IEnumerable<ColumnInfo> validColumns);
        string GenerateCleanup();

        // Block generation methods
        string GenerateOpenBlock();
        string GenerateCloseBlock();

        // New method for table recreation
        string GenerateTableRecreation(IEnumerable<ColumnInfo> columns);
    }
}