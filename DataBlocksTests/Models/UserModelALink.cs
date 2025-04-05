using DataBlocks.DataAccess;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Models;

[ScheModel]
public class UserModelALink : IModel
{
    [ScheKey("id")]
    public long ID { get; set; }
    [ScheData("application_user_id")]
    public long ApplicationUserID { get; set; }
    [ScheData("test_model_a_id")]
    public long TestModelAID { get; set; }
    [ScheData("deleted")]
    public bool Deleted { get; set; }
    [ScheData("created")]
    public DateTime Created { get; set; }
    [ScheData("modified")]
    public DateTime Modified { get; set; }
}