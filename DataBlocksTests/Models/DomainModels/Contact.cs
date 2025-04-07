using System;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;

namespace DataBlocksTests.Models.DomainModels;

public class Contact : IConstituentModel<Models.Contact>
{
    public long ID { get; set; }
    public string ContactType { get; set; }
    public string Value { get; set; }
    public bool IsPrimary { get; set; }
    public string Notes { get; set; }
}