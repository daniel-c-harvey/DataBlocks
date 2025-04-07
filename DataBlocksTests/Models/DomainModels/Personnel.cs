using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;
using ExpressionToSql;

namespace DataBlocksTests.Models.DomainModels;

public class Personnel : ICompositeModel<Personnel, Models.Personnel, Models.PersonnelContact>
{
    public long ID { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string Title { get; set; }
    public string Status { get; set; }
    public string Prefix { get; set; }
    public string Suffix { get; set; }
    public string Gender { get; set; }
    public string AdministrativeNotes { get; set; }
    public bool ApprovedSiteMember { get; set; }
    public DateTime? DateLastPurchased { get; set; }
    public DateTime? DateLastEventRegistration { get; set; }
    public bool EmailBounced { get; set; }
    public bool HasRegisteredForEvent { get; set; }
    public string IndividualResponsibilities { get; set; }
    public string InternalComments { get; set; }
    public bool InvoicingContact { get; set; }
    public bool MemberSuspended { get; set; }
    public bool PrimaryContact { get; set; }
    public bool RemoveFromAllEmailMessages { get; set; }
    public bool AutoRenew { get; set; }
    public bool KeyContact { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime? LastRenewalDate { get; set; }
    public string MemberNumber { get; set; }
    public string MemberType { get; set; }
    public string MCUsername { get; set; }
    
    // Joins
    public IList<Contact> Contacts { get; set; } = new List<Contact>();
    
    // ICompositeModel implementation

    public static Expression<Func<Models.Personnel, Models.PersonnelContact, bool>> Predicate =>
        (p, pc) => p.ID == pc.PersonnelId;

    public static JoinChain<Personnel, Models.Personnel, Models.PersonnelContact> Join { get; } =
        JoinChain<Personnel, Models.Personnel, Models.PersonnelContact>.CreateJoin();
}