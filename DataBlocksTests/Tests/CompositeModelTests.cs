// using System;
// using System.Threading.Tasks;
// using DataBlocks.DataAccess;
// using DataBlocks.DataAccess.Postgres;
// using DataBlocks.DataAdapters;
// using DataBlocksTests.Models.DomainModels;
// using NUnit.Framework;
//
// namespace DataBlocksTests.Tests
// {
//     [TestFixture]
//     public class CompositeModelTests
//     {
//         private static IDataAccess<IPostgresDatabase> dataAccess;
//         private static ICompositeDataAdapter<Personnel> personnelCompositeAdapter;
//
//         [OneTimeSetUp]
//         public static void SetUp()
//         {
//             // This is just an example of how to set up the composite adapter
//             // The actual connection details would depend on your test environment
//             
//             // Get the database connection
//             var connectionString = "Host=localhost;Database=testdb;Username=user;Password=pass";
//             dataAccess = new PostgresDataAccess(connectionString, "testdb");
//             
//             // Create the composite query builder
//             var queryBuilder = new PostgresCompositeQueryBuilder();
//             
//             // Create the composite adapter
//             personnelCompositeAdapter = CompositeDataAdapterFactory.Create<IPostgresDatabase, Personnel>(
//                 dataAccess,
//                 queryBuilder,
//                 DataSchema.Create<Personnel>("test-schema")
//             );
//         }
//
//         [Test]
//         public async Task GetPersonnelWithContacts_ReturnsPersonnelWithContactsLoaded()
//         {
//             // Arrange
//             const long personnelId = 123; // The ID of a personnel that exists in the test database
//             
//             // Act
//             var result = await personnelCompositeAdapter.GetByID(personnelId);
//             
//             // Assert
//             Assert.That(result.Success, Is.True);
//             Assert.That(result.Value, Is.Not.Null);
//             Assert.That(result.Value.Contacts, Is.Not.Null);
//             Assert.That(result.Value.Contacts.Count, Is.GreaterThan(0));
//             
//             // Additional assertions about the data
//             var personnel = result.Value;
//             Console.WriteLine($"Retrieved personnel: {personnel.FirstName} {personnel.LastName}");
//             Console.WriteLine($"Number of contacts: {personnel.Contacts.Count}");
//             
//             foreach (var contact in personnel.Contacts)
//             {
//                 Console.WriteLine($"Contact: {contact.ContactType} - {contact.Value}");
//             }
//         }
//     }
// } 