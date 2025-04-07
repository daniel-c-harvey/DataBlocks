using DataBlocks.DataAccess;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Models;

[ScheModel]
public class TestModelA : IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<TestModelA>("test-schema");

    [ScheKey("id")]
    public long ID { get; set; }
    [ScheData("name")]
    public string Name { get; set; }

    [ScheData("age")]
    public int Age { get; set; }
    
    [ScheData("birth_date")]
    public DateTime BirthDate { get; set; }

    [ScheData("deleted")]
    public bool Deleted { get; set; }
    [ScheData("created")]
    public DateTime Created { get; set; }
    [ScheData("modified")]
    public DateTime Modified { get; set; }
}