using DataBlocks.Migrations;

namespace ScheMigratorTests.Models;

[ScheModel]
public class TestModelA
{
    [ScheKey("id")]
    public int Id { get; set; }

    [ScheData("name")]
    public string Name { get; set; }

    [ScheData("age")]
    public int Age { get; set; }
}