using System.Text.Json.Serialization;
using DataAccess;
using DataBlocks.DataAccess;
using DataBlocks.Migrations;

namespace DataBlocksTests.Models;

[ScheModel]
public class TestModelA : ModelBase, IModel
{
    [ScheData("name")]
    public string Name { get; set; }

    [ScheData("age")]
    public int Age { get; set; }
    
    [ScheData("birth_date")]
    public DateTime BirthDate { get; set; }
}