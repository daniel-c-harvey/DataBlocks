using System.Text.Json.Serialization;
using DataBlocks.DataAccess;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Models
{
    [ScheModel]
    public class PersonnelContact : IModel, ILinkageModel<PersonnelContact>
    {
        public static DataSchema Schema { get; } = DataSchema.Create<PersonnelContact>("test-schema");

        [ScheKey("id")]
        public long ID { get; set; }

        [ScheData("personnel_id")]
        public long PersonnelId { get; set; }

        [ScheData("contact_id")]
        public long ContactId { get; set; }

        [ScheData("contact_type")]
        public string ContactType { get; set; }

        [ScheData("is_primary")]
        public bool IsPrimary { get; set; }

        [ScheData("deleted")]
        public bool Deleted { get; set; }

        [ScheData("created")]
        public DateTime Created { get; set; }

        [ScheData("modified")]
        public DateTime Modified { get; set; }

        // Linkage model properties
        [JsonIgnore]
        public IList<long> ForeignIDs =>  [PersonnelId, ContactId];
    }
}
