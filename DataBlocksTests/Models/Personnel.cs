using DataBlocks.DataAccess;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Models
{
    [ScheModel]
    public class Personnel : IModel
    {
        public static DataSchema Schema { get; } = DataSchema.Create<Personnel>("test-schema");

        [ScheKey("id")]
        public long ID { get; set; }
        
        [ScheData("first_name")]
        public string FirstName { get; set; }

        [ScheData("middle_name")]
        public string MiddleName { get; set; }

        [ScheData("last_name")]
        public string LastName { get; set; }

        [ScheData("title")]
        public string Title { get; set; }

        [ScheData("status")]
        public string Status { get; set; }

        [ScheData("prefix")]
        public string Prefix { get; set; }

        [ScheData("suffix")]
        public string Suffix { get; set; }

        [ScheData("gender")]
        public string Gender { get; set; }

        [ScheData("administrative_notes")]
        public string AdministrativeNotes { get; set; }

        [ScheData("approved_site_member")]
        public bool ApprovedSiteMember { get; set; }

        [ScheData("date_last_purchased")]
        public DateTime? DateLastPurchased { get; set; }

        [ScheData("date_last_event_registration")]
        public DateTime? DateLastEventRegistration { get; set; }

        [ScheData("email_bounced")]
        public bool EmailBounced { get; set; }

        [ScheData("has_registered_for_event")]
        public bool HasRegisteredForEvent { get; set; }

        [ScheData("individual_responsibilities")]
        public string IndividualResponsibilities { get; set; }

        [ScheData("internal_comments")]
        public string InternalComments { get; set; }

        [ScheData("invoicing_contact")]
        public bool InvoicingContact { get; set; }

        [ScheData("member_suspended")]
        public bool MemberSuspended { get; set; }

        [ScheData("primary_contact")]
        public bool PrimaryContact { get; set; }

        [ScheData("remove_from_all_email_messages")]
        public bool RemoveFromAllEmailMessages { get; set; }

        [ScheData("auto_renew")]
        public bool AutoRenew { get; set; }

        [ScheData("key_contact")]
        public bool KeyContact { get; set; }

        [ScheData("last_login_date")]
        public DateTime? LastLoginDate { get; set; }

        [ScheData("last_renewal_date")]
        public DateTime? LastRenewalDate { get; set; }

        [ScheData("member_number")]
        public string MemberNumber { get; set; }

        [ScheData("member_type")]
        public string MemberType { get; set; }

        [ScheData("mc_username")]
        public string MCUsername { get; set; }

        [ScheData("deleted")]
        public bool Deleted { get; set; }

        [ScheData("created")]
        public DateTime Created { get; set; }

        [ScheData("modified")]
        public DateTime Modified { get; set; }
    }
}
