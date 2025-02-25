using DataBlocks.Migrations;

namespace ScheMigratorTests.Models;

[ScheModel]
public class TestModelA
{
    [SqlColumn]
    public int Id { get; set; }
}