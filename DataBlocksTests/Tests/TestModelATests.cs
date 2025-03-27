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
    public static class TestModelATests
    {
        private const int MODEL_COUNT = 5;
        private static IList<TestModelA> models = new List<TestModelA>() 
        { 
            new() { ID = 1, Name = "Phil", Age = 25, BirthDate = new DateTime(2000, 1, 1) },
            new() { ID = 2, Name = "John", Age = 30, BirthDate = new DateTime(1995, 5, 14) },
            new() { ID = 3, Name = "Jane", Age = 20, BirthDate = new DateTime(2005, 11, 11) },
            new() { ID = 4, Name = "Jim", Age = 35, BirthDate = new DateTime(1990, 7, 16) },
            new() { ID = 5, Name = "Jill", Age = 25, BirthDate = new DateTime(2000, 3, 26) } 
        };
        
        private static IDataAdapter<TestModelA> pAdapter;
        private static IDataAdapter<TestModelA> mAdapter;

        private static IEnumerable TargetTestCases()
        {
            Dictionary<Lazy<IDataAdapter<TestModelA>>, string> adapters = new()
            {
                // {new Lazy<IDataAdapter<TestModelA>>(() => mAdapter), "MongoDB"}, 
                {new Lazy<IDataAdapter<TestModelA>>(() => pAdapter), "PostgreSQL"}
            };
            IEnumerable<int> targetIndex = Enumerable.Range(0, MODEL_COUNT);

            foreach (var adapter in adapters)
            {
                foreach (int index in targetIndex)
                {
                    yield return new TestCaseData(adapter.Key, index).SetName(adapter.Value);
                }
            }
            // yield return new TestCaseData(new Lazy<IDataAdapter<TestModelA>>(() => mAdapter)).SetName("MongoDB");
            // yield return new TestCaseData(new Lazy<IDataAdapter<TestModelA>>(() => pAdapter)).SetName("Postgres");
        }

        private static IEnumerable AdapterTestCases()
        {
            // yield return new TestCaseData(new Lazy<IDataAdapter<TestModelA>>(() => mAdapter)).SetName("MongoDB");
            yield return new TestCaseData(new Lazy<IDataAdapter<TestModelA>>(() => pAdapter)).SetName("PostgreSQL");
        }

        [SetUp]
        public static void TestModelA()
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

            pAdapter = DataAdapterFactory.Create<IPostgresDatabase, TestModelA>(pDdataAccess, 
                                                                                pQueryBuilder, 
                                                                                DataSchema.Create<TestModelA>("test-schema"));

            mAdapter = DataAdapterFactory.Create<IMongoDatabase, TestModelA>(mDataAccess, 
                                                                             mQueryBuilder, 
                                                                             DataSchema.Create<TestModelA>("test-schema"));
        }

        
        [Test, Order(1)]
        [TestCaseSource(nameof(AdapterTestCases))]
        public static async Task TestModelA_InsertAllModels(Lazy<IDataAdapter<TestModelA>> adapter)
        {
            foreach (var model in models)
            {
                var insert = await adapter.Value.Insert(model);
                Assert.That(insert.Success, Is.True);
            }
        }

        [Test, Order(2)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task TestModelA_QueryByName(Lazy<IDataAdapter<TestModelA>> adapter, int targetIndex)
        {
            TestModelA target = models[targetIndex];

            var byPredicate = await adapter.Value.GetByPredicate(m => m.Name == target.Name);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byPredicate);
                AssertModelResultHasElements(byPredicate);
                AssertModelResultHasTarget(byPredicate.Value.First(), target);
            });
        }

        [Test, Order(3)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task TestModelA_QueryById(Lazy<IDataAdapter<TestModelA>> adapter, int targetIndex)
        {
            TestModelA target = models[targetIndex];
            var byId = await adapter.Value.GetByID(target.ID);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byId);
                AssertModelResultHasTarget(byId.Value, target);
            });
        }

        [Test, Order(4)]
        [TestCaseSource(nameof(TargetTestCases))]
        public static async Task TestModelA_QueryByPage(Lazy<IDataAdapter<TestModelA>> adapter, int targetIndex)
        {
            TestModelA target = models[targetIndex];

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
        public static async Task TestModelA_UpdateTarget(Lazy<IDataAdapter<TestModelA>> adapter, int targetIndex)
        {
            TestModelA target = models[targetIndex];
            target.Age += 1;
            target.Name = "Billy Bob";

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
        public static async Task TestModelA_DeleteAll(Lazy<IDataAdapter<TestModelA>> dataAdapter)
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

        private static void AssertModelResultHasElements(ResultContainer<IEnumerable<TestModelA>> result)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Value?.Any(), Is.True);
                Assert.That(result.Value?.First(), Is.Not.Null);
            });
        }

        private static void AssertModelResultHasTarget(TestModelA model, TestModelA target)
        {
            Assert.Multiple(() =>
            {
                Assert.That(model.Name, Is.EqualTo(target.Name));
                Assert.That(model.Age, Is.EqualTo(target.Age));
                Assert.That(model.BirthDate, Is.EqualTo(target.BirthDate));
            });
        }
        
    }
}