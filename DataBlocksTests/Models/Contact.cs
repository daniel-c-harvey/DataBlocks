using DataBlocks.DataAccess;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Models
{
    [ScheModel]
    public class Contact : IModel
    {
        public static DataSchema Schema { get; } = DataSchema.Create<Contact>("test-schema");

        [ScheKey("id")]
        public long ID { get; set; }

        [ScheData("contact_type")]
        public string ContactType { get; set; }

        // Primary Address
        [ScheData("address_line_1")]
        public string AddressLine1 { get; set; }

        [ScheData("address_line_2")]
        public string AddressLine2 { get; set; }

        [ScheData("city")]
        public string City { get; set; }

        [ScheData("state")]
        public string State { get; set; }

        [ScheData("zip")]
        public string Zip { get; set; }

        [ScheData("country")]
        public string Country { get; set; }

        // Contact Methods
        [ScheData("email")]
        public string Email { get; set; }
        
        [ScheData("phone")]
        public string Phone { get; set; }

        [ScheData("deleted")]
        public bool Deleted { get; set; }

        [ScheData("created")]
        public DateTime Created { get; set; }

        [ScheData("modified")]
        public DateTime Modified { get; set; }
    }
}
