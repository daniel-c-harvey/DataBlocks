using System;
using System.Linq.Expressions;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;

namespace DataBlocksTests.Models.DomainModels
{
    public class PersonnelContact : ILinkModel<PersonnelContact, Models.PersonnelContact, Models.Contact>
    {
        public long ID { get; set; }
        public long PersonnelID { get; set; }
        public long ContactID { get; set; }

        public static Expression<Func<Models.PersonnelContact, Models.Contact, bool>> Predicate =>
            (pc, c) => pc.ContactId == c.ID;
    }
} 