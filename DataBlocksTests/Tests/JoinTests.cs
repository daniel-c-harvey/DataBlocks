using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;
using DataBlocks.DataAdapters;
using DataBlocksTests.Models;
using DataBlocksTests.Models.DomainModels;
using NetBlocks.Models.Environment;
using NetBlocks.Utilities;

namespace DataBlocksTests.Tests;

[TestFixture]
public class JoinTests
{
    private const int PERSONNEL_COUNT = 5;
    private const int CONTACT_COUNT = 10;
    private const int PERSONNEL_CONTACT_COUNT = 10;
    private static IList<Models.Personnel> personnel = new List<Models.Personnel>()
    {
        new() { ID = 1, FirstName = "Phil", LastName = "Jones", Gender = "M", MemberNumber = "123" },
        new() { ID = 2, FirstName = "Jane", LastName = "Smith", Gender = "F", MemberNumber = "456" },
        new() { ID = 3, FirstName = "John", LastName = "Doe", Gender = "M", MemberNumber = "789" },
        new() { ID = 4, FirstName = "Alice", LastName = "Johnson", Gender = "F", MemberNumber = "101" },
        new() { ID = 5, FirstName = "Bob", LastName = "Brown", Gender = "M", MemberNumber = "102" }
    };

    private static IList<Models.Contact> contacts = new List<Models.Contact>()
    {
        new() { ID = 1, ContactType = "Primary", Phone = "1234567890", Email = "phil@example.com", AddressLine1 = "123 Main St, Anytown, USA" },
        new() { ID = 2, ContactType = "Secondary", Phone = "4567891230" },
        new() { ID = 3, ContactType = "Primary", Phone = "1234567890", Email = "jane@example.com", AddressLine1 = "456 Main St, Anytown, USA" },
        new() { ID = 4, ContactType = "Secondary", Phone = "4567891230" },
        new() { ID = 5, ContactType = "Primary", Phone = "1234567890", Email = "john@example.com", AddressLine1 = "789 Main St, Anytown, USA" },
        new() { ID = 6, ContactType = "Secondary", Phone = "4567891230" },
        new() { ID = 7, ContactType = "Primary", Phone = "1234567890", Email = "alice@example.com", AddressLine1 = "101 Main St, Anytown, USA" },
        new() { ID = 8, ContactType = "Secondary", Phone = "4567891230" },
        new() { ID = 9, ContactType = "Primary", Phone = "1234567890", Email = "bob@example.com", AddressLine1 = "101 Main St, Anytown, USA" },
        new() { ID = 10, ContactType = "Secondary", Phone = "4567891230" }
    };

    private static IList<Models.PersonnelContact> personnelContacts = new List<Models.PersonnelContact>()
    {
        new() { PersonnelId = 1, ContactId = 1 },
        new() { PersonnelId = 1, ContactId = 2 },
        new() { PersonnelId = 2, ContactId = 3 },
        new() { PersonnelId = 2, ContactId = 4 },
        new() { PersonnelId = 3, ContactId = 5 },
        new() { PersonnelId = 3, ContactId = 6 },
        new() { PersonnelId = 4, ContactId = 7 },
        new() { PersonnelId = 4, ContactId = 8 },
        new() { PersonnelId = 5, ContactId = 9 },
        new() { PersonnelId = 5, ContactId = 10 }
    };

    private static IDataAccess<IPostgresDatabase> dataAccess;
    private static IDataAdapter<Models.Personnel> personnelAdapter;
    private static IDataAdapter<Models.Contact> contactAdapter;
    private static IDataAdapter<Models.PersonnelContact> personnelContactAdapter;
    
    private static ICompositeDataAdapter<Models.DomainModels.Personnel, Models.Personnel, 
                                         Models.DomainModels.PersonnelContact, Models.PersonnelContact, 
                                         Models.DomainModels.Contact, Models.Contact> personnelAdapterComposite;

    [OneTimeSetUp]
    public static void SetUp()
    {
        var connections = LoadConnections();
        var pConnection = connections.ConnectionStrings[1];
        if (pConnection is null) throw new Exception("Connection is null");

        var pQueryBuilder = QueryBuilderFactory.Create<IPostgresDatabase>();
        if (pQueryBuilder is null) throw new Exception("Query builder is null");

        dataAccess = DataAccessFactory.Create<IPostgresClient, IPostgresDatabase>(pConnection.ConnectionString, pConnection.DatabaseName);
        if (dataAccess is null) throw new Exception("Data access is null");

        personnelAdapter = DataAdapterFactory.Create<IPostgresDatabase, Models.Personnel>
        (
            dataAccess,
            pQueryBuilder,
            DataSchema.Create<Models.Personnel>("test-schema")
        );

        contactAdapter = DataAdapterFactory.Create<IPostgresDatabase, Models.Contact>
        (
            dataAccess,
            pQueryBuilder,
            DataSchema.Create<Models.Contact>("test-schema")
        );

        personnelContactAdapter = DataAdapterFactory.Create<IPostgresDatabase, Models.PersonnelContact>
        (
            dataAccess,
            pQueryBuilder,
            DataSchema.Create<Models.PersonnelContact>("test-schema")
        );
        
        
        var personnelTask = personnelAdapter.Insert(personnel);
        personnelTask.Wait();
        var contactTask =  contactAdapter.Insert(contacts);
        contactTask.Wait();
        var personnelContactTask = personnelContactAdapter.Insert(personnelContacts);
        personnelContactTask.Wait();
        
        if (!personnelTask.Result.Success) throw new Exception("Failed to insert personnel");
        if (!contactTask.Result.Success) throw new Exception("Failed to insert contacts");
        if (!personnelContactTask.Result.Success) throw new Exception("Failed to insert personnel contacts");
        
        var pcQueryBuilder = new PostgresCompositeQueryBuilder();
        
        personnelAdapterComposite = CompositeDataAdapterFactory
            .Create<IPostgresDatabase,
                    Models.DomainModels.Personnel, Models.Personnel, 
                    Models.DomainModels.PersonnelContact, Models.PersonnelContact, 
                    Models.DomainModels.Contact, Models.Contact>
                (dataAccess, pcQueryBuilder);
    }

    private static Connections LoadConnections()
    {
        string json = File.ReadAllText("./environment/connections.json");
        Connections? connections = System.Text.Json.JsonSerializer.Deserialize<Connections>(json);
        if (connections is null) throw new Exception("Connections is null");
        if (connections.ConnectionStrings.Count < 1) throw new Exception("No connection strings found");
        return connections;
    }

    [Test]
    public static async Task ShouldQueryPersonnel()
    {
        var personnelResult = await personnelAdapter.GetAll();
        if (!personnelResult.Success || personnelResult.Value is null) throw new Exception("Failed to get personnel");

        var thesePersonnel = personnelResult.Value.ToList();
        Assert.That(thesePersonnel.Count, Is.EqualTo(PERSONNEL_COUNT));
    }

    [Test]
    public static async Task ShouldQueryContacts()
    {
        var contactResult = await contactAdapter.GetAll();
        if (!contactResult.Success || contactResult.Value is null) throw new Exception("Failed to get contacts");

        var theseContacts = contactResult.Value.ToList();
        Assert.That(theseContacts.Count, Is.EqualTo(CONTACT_COUNT));
    }

    [Test]
    public static async Task ShouldQueryPersonnelContacts()
    {
        var personnelContactResult = await personnelContactAdapter.GetAll();
        if (!personnelContactResult.Success || personnelContactResult.Value is null) throw new Exception("Failed to get personnel contacts");

        var thesepersonnelContacts = personnelContactResult.Value.ToList();
        Assert.That(thesepersonnelContacts.Count, Is.EqualTo(PERSONNEL_CONTACT_COUNT));
    }

    [Test]
    public static async Task ShouldQueryPersonnelWithContacts()
    {
        var result = await personnelAdapterComposite.GetByID(personnel.First().ID);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ID, Is.EqualTo(personnel.First().ID));
        Assert.That(result.Value.Contacts, Is.Not.Null);
        Assert.That(result.Value.Contacts, Is.Not.Empty);
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        if (personnelAdapter != null) personnel.ForEach(async p => await personnelAdapter.Delete(p));
        if (contactAdapter != null) contacts.ForEach(async c => await contactAdapter.Delete(c));
        if (personnelContactAdapter != null) personnelContacts.ForEach(async pc => await personnelContactAdapter.Delete(pc));
    }
    
}