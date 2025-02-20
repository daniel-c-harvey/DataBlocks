using DataBlocks.Migrations;

namespace ScheMigratorTests.Models;

[ScheModel(SqlImplementation.PostgreSQL)]
public class TestModelA
{
    [SqlColumn]
    public int Id { get; set; }
}