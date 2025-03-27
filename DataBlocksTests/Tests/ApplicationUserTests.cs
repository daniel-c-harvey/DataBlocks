using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataBlocks.DataAccess;
using DataBlocks.DataAccess.Postgres;
using DataBlocks.DataAdapters;
using DataBlocksTests.Models;
using MongoDB.Driver;
using NetBlocks.Models;
using NetBlocks.Models.Environment;

namespace DataBlocksTests.Tests
{
    [TestFixture]
    public static class ApplicationUserTests
    {
        private const int MODEL_COUNT = 5;
        private static IList<ApplicationUser> models = new List<ApplicationUser>()
        {
            new() { ID = 1, UserName = "Phil", NormalizedUserName = "phil", Email = "phil@example.com", EmailConfirmed = true, PasswordHash = "password", PhoneNumber = "1234567890", Created = DateTime.Now },
            new() { ID = 2, UserName = "John", NormalizedUserName = "john", Email = "john@example.com", EmailConfirmed = true, PasswordHash = "password", PhoneNumber = "1234567890", Created = DateTime.Now },
            new() { ID = 3, UserName = "Jane", NormalizedUserName = "jane", Email = "jane@example.com", EmailConfirmed = true, PasswordHash = "password", PhoneNumber = "1234567890", Created = DateTime.Now },
            new() { ID = 4, UserName = "Jim", NormalizedUserName = "jim", Email = "jim@example.com", EmailConfirmed = true, PasswordHash = "password", PhoneNumber = "1234567890", Created = DateTime.Now },
            new() { ID = 5, UserName = "Jill", NormalizedUserName = "jill", Email = "jill@example.com", EmailConfirmed = true, PasswordHash = "password", PhoneNumber = "1234567890", Created = DateTime.Now },
        };

        private static IDataAdapter<ApplicationUser> pAdapter;
        private static IDataAdapter<ApplicationUser> mAdapter;

        private static IEnumerable TargetTestCases()
        {
            Dictionary<Lazy<IDataAdapter<ApplicationUser>>, string> adapters = new()
            {
                // {new Lazy<IDataAdapter<ApplicationUser>>(() => mAdapter), "MongoDB"}, 
                {new Lazy<IDataAdapter<ApplicationUser>>(() => pAdapter), "PostgreSQL"}
            };
            IEnumerable<int> targetIndex = Enumerable.Range(0, MODEL_COUNT);

            foreach (var adapter in adapters)
            {
                foreach (int index in targetIndex)
                {
                    yield return new TestCaseData(adapter.Key, index).SetName(adapter.Value);
                }
            }
            // yield return new TestCaseData(new Lazy<IDataAdapter<ApplicationUser>>(() => mAdapter)).SetName("MongoDB");
            // yield return new TestCaseData(new Lazy<IDataAdapter<ApplicationUser>>(() => pAdapter)).SetName("PostgreSQL");
        }

        private static IEnumerable AdapterTestCases()
        {
            // yield return new TestCaseData(new Lazy<IDataAdapter<ApplicationUser>>(() => mAdapter)).SetName("MongoDB");
            yield return new TestCaseData(new Lazy<IDataAdapter<ApplicationUser>>(() => pAdapter)).SetName("PostgreSQL");
        }

        [SetUp]
        public static void Setup()
        {
            var connections = LoadConnections();
            var pConnection = connections.ConnectionStrings[1];
            if (pConnection is null) throw new Exception("Connection is null");
            var mConnection = connections.ConnectionStrings[0];
            if (mConnection is null) throw new Exception("Connection is null");

            var mQueryBuilder = QueryBuilderFactory.Create<IMongoDatabase>();
            if (mQueryBuilder is null) throw new Exception("Query builder is null");

            var mDataAccess = DataAccessFactory.Create<IMongoClient, IMongoDatabase>(mConnection.ConnectionString, mConnection.DatabaseName);
            if (mDataAccess is null) throw new Exception("Data access is null");

            var pQueryBuilder = QueryBuilderFactory.Create<IPostgresDatabase>();
            if (pQueryBuilder is null) throw new Exception("Query builder is null");

            var pDdataAccess = DataAccessFactory.Create<IPostgresClient, IPostgresDatabase>(pConnection.ConnectionString, pConnection.DatabaseName);
            if (pDdataAccess is null) throw new Exception("Data access is null");

            pAdapter = DataAdapterFactory.Create<IPostgresDatabase, ApplicationUser>(pDdataAccess,
                                                                                pQueryBuilder,
                                                                                DataSchema.Create<ApplicationUser>("test-schema"));

            mAdapter = DataAdapterFactory.Create<IMongoDatabase, ApplicationUser>(mDataAccess,
                                                                             mQueryBuilder,
                                                                             DataSchema.Create<ApplicationUser>("test-schema"));
        }


        [Test, Order(1)]
        [TestCaseSource(nameof(AdapterTestCases))]
        public static async Task InsertAllModels(Lazy<IDataAdapter<ApplicationUser>> adapter)
        {
            foreach (var model in models)
            {
                var insert = await adapter.Value.Insert(model);
                Assert.That(insert.Success, Is.True);
            }
        }

        [Test, Order(2)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task QueryByName(Lazy<IDataAdapter<ApplicationUser>> adapter, int targetIndex)
        {
            ApplicationUser target = models[targetIndex];

            var byPredicate = await adapter.Value.GetByPredicate(m => m.NormalizedUserName == target.NormalizedUserName);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byPredicate);
                AssertModelResultHasElements(byPredicate);
                AssertModelResultHasTarget(byPredicate.Value.First(), target);
            });
        }

        [Test, Order(3)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task QueryById(Lazy<IDataAdapter<ApplicationUser>> adapter, int targetIndex)
        {
            ApplicationUser target = models[targetIndex];
            var byId = await adapter.Value.GetByID(target.ID);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byId);
                AssertModelResultHasTarget(byId.Value, target);
            });
        }

        [Test, Order(4)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task QueryByPage(Lazy<IDataAdapter<ApplicationUser>> adapter, int targetIndex)
        {
            ApplicationUser target = models[targetIndex];

            var byPage = await adapter.Value.GetPage(0, 25);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byPage);
                AssertModelResultHasElements(byPage);
                AssertModelResultHasTarget(byPage.Value.First(x => x.ID == target.ID), target);
            });
        }

        [Test, Order(5)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task UpdateTarget(Lazy<IDataAdapter<ApplicationUser>> adapter, int targetIndex)
        {
            ApplicationUser target = models[targetIndex];
            target.UserName = "BillyBob";

            var update = await adapter.Value.Update(target);
            Assert.That(update.Success, Is.True);

            var byId = await adapter.Value.GetByID(target.ID);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byId);
                AssertModelResultHasTarget(byId.Value, target);
            });
        }

        [Test, Order(6)]
        [TestCaseSource(nameof(AdapterTestCases))]
        public static async Task DeleteAll(Lazy<IDataAdapter<ApplicationUser>> dataAdapter)
        {
            Result result = new Result();
            foreach (var model in models)
            {
                result.Merge(await dataAdapter.Value.Delete(model));
            }
            Assert.That(result.Success, Is.True);

            var page = await dataAdapter.Value.GetPage(0, 25);
            Assert.Multiple(() =>
            {
                Assert.That(page.Success, Is.True);
                Assert.That(page.Value, Is.Not.Null);
                Assert.That(page.Value.Any(), Is.Not.True);
            });
        }


        private static Connections LoadConnections()
        {
            string json = File.ReadAllText("./environment/connections.json");
            Connections? connections = System.Text.Json.JsonSerializer.Deserialize<Connections>(json);
            if (connections is null) throw new Exception("Connections is null");
            if (connections.ConnectionStrings.Count < 1) throw new Exception("No connection strings found");
            return connections;
        }

        private static void AssertModelResultIsValid<T>(ResultContainer<T> result)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Success, Is.True);
                Assert.That(result.Value, Is.Not.Null);
            });
        }

        private static void AssertModelResultHasElements(ResultContainer<IEnumerable<ApplicationUser>> result)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Value?.Any(), Is.True);
                Assert.That(result.Value?.First(), Is.Not.Null);
            });
        }

        private static void AssertModelResultHasTarget(ApplicationUser model, ApplicationUser target)
        {
            Assert.Multiple(() =>
            {
                Assert.That(model.UserName, Is.EqualTo(target.UserName));
                Assert.That(model.Email, Is.EqualTo(target.Email));
                Assert.That(model.Id, Is.EqualTo(target.Id));

            });
        }

    }
}