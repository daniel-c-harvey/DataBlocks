using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataBlocks.DataAdapters;
using DataBlocksTests.Models;
using NetBlocks.Models;
using NetBlocks.Models.Environment;

namespace DataBlocksTests.Tests
{
    public static class TestModelTester
    {
        public static Connections LoadConnections()
        {
            string json = File.ReadAllText("./environment/connections.json");
            Connections? connections = System.Text.Json.JsonSerializer.Deserialize<Connections>(json);
            if (connections is null) throw new Exception("Connections is null");
            if (connections.ConnectionStrings.Count < 1) throw new Exception("No connection strings found");
            return connections;
        }
        
        public static async Task TestModelA(IDataAdapter<TestModelA> dataAdapter)
        {
            IList<TestModelA> newModels = new List<TestModelA>() { new() { ID = 1, Name = "Phil", Age = 25, BirthDate = new DateTime(2000, 1, 1) },
                new() { ID = 2, Name = "John", Age = 30, BirthDate = new DateTime(1995, 5, 14) },
                new() { ID = 3, Name = "Jane", Age = 20, BirthDate = new DateTime(2005, 11, 11) },
                new() { ID = 4, Name = "Jim", Age = 35, BirthDate = new DateTime(1990, 7, 16) },
                new() { ID = 5, Name = "Jill", Age = 25, BirthDate = new DateTime(2000, 3, 26) } };

            Random x = new Random();
            TestModelA target = newModels[x.Next(0, newModels.Count)];
            
            await TestModelA_Insert(dataAdapter, newModels);
            await TestModelA_QueryByName(dataAdapter, target);
            await TestModelA_QueryById(dataAdapter, target);
            await TestModelA_QueryByPage(dataAdapter, target);
            
            target = newModels[x.Next(0, newModels.Count)];
            target.Age += 1;
            target.Name = "Billy Bob";

            await TestModelA_UpdateTarget(dataAdapter, target);
            await TestModelA_QueryById(dataAdapter, target);
            
            await TestModelA_DeleteAll(dataAdapter, newModels);
        }

        private static async Task TestModelA_DeleteAll(IDataAdapter<TestModelA> dataAdapter, IEnumerable<TestModelA> models)
        {
            Result result = new Result();
            foreach (var model in models)
            {
                result.Merge(await dataAdapter.Delete(model));
            }
            Assert.That(result.Success, Is.True);

            var page = await dataAdapter.GetPage(0, 25);
            Assert.Multiple(() =>
            {
                Assert.That(page.Success, Is.True);
                Assert.That(page.Value, Is.Not.Null);
                Assert.That(page.Value.Any(), Is.Not.True);
            });
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
        
        private static async Task TestModelA_Insert(IDataAdapter<TestModelA> adapter, IEnumerable<TestModelA> newModels)
        {
            foreach (var model in newModels)
            {
                var insert = await adapter.Insert(model);
                Assert.That(insert.Success, Is.True);
            }
        }

        private static async Task TestModelA_QueryByName(IDataAdapter<TestModelA> adapter, TestModelA target)
        {
            var byPredicate = await adapter.GetByPredicate(m => m.Name == target.Name);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byPredicate);
                AssertModelResultHasElements(byPredicate);
                AssertModelResultHasTarget(byPredicate.Value.First(), target);
            });
        }
        
        private static async Task TestModelA_QueryById(IDataAdapter<TestModelA> adapter, TestModelA target)
        {
            var byId = await adapter.GetByID(target.ID);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byId);
                AssertModelResultHasTarget(byId.Value, target);
            });
        }

        private static async Task TestModelA_QueryByPage(IDataAdapter<TestModelA> adapter, TestModelA target)
        {
            
            var byPage = await adapter.GetPage(0, 25);
            Assert.Multiple(() =>
            {
                AssertModelResultIsValid(byPage);
                AssertModelResultHasElements(byPage);
                AssertModelResultHasTarget(byPage.Value.First(x => x.ID == target.ID), target);
            });
        }

        private static async Task TestModelA_UpdateTarget(IDataAdapter<TestModelA> adapter, TestModelA target)
        {
            var update = await adapter.Update(target);
            Assert.That(update.Success, Is.True);
        }
    }
}