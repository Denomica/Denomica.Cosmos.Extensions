using Denomica.Cosmos.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.Json;
using Microsoft.Azure.Cosmos.Linq;

namespace Denomica.Cosmos.Extensions.Tests
{
    [TestClass]
    public class DataAccessTests
    {

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            var connectionString = $"{context.Properties["connectionString"]}";
            var databaseId = $"{context.Properties["databaseId"]}";
            var containerId = $"{context.Properties["containerId"]}";

            var client = new CosmosClient(connectionString, new CosmosClientOptions { 
                SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } 
            });
            var database = client.GetDatabase(databaseId);
            var container = database.GetContainer(containerId);

            try
            {
                var deleteResponse = await container.DeleteContainerAsync();
            }
            catch { }

            var containerResponse = await database.CreateContainerAsync(new ContainerProperties { Id = containerId, PartitionKeyPath = "/partition" });
            DataContainer = database.GetContainer(containerId);
            Proxy = new ContainerProxy(database.GetContainer(containerId), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private static Container DataContainer = null!;
        private static ContainerProxy Proxy = null!;
        private static QueryService Query = null!;


        [TestInitialize]
        [TestCleanup]
        public async Task ClearContainer()
        {
            var tasks = new List<Task>();
            var items = Proxy.QueryItemsAsync(new QueryDefinition("select c.id,c.partition from c"));
            await foreach(var item in items)
            {
                PartitionKey partition = PartitionKey.None;
                var idProperty = item.GetProperty("id");
                var id = idProperty.GetString() ?? throw new NullReferenceException();
                if(item.TryGetProperty("partition", out var p))
                {
                    partition = Proxy.CreatePartitionKey(p);
                }
                tasks.Add(Proxy.DeleteItemAsync(id, partition));
            }

            await Task.WhenAll(tasks);

            var errors = from x in tasks where !x.IsCompletedSuccessfully select x;
            if(errors.Any())
            {
                throw new Exception("Unable to clear all items from container.");
            }
        }



        [TestMethod]
        [Description("Bulk loads the container ensuring that no HTTP 429 is returned.")]
        public async Task BulkLoad01()
        {
            int itemCount = 5000;
            var upsertTasks = new List<Task<Dictionary<string, object>>>();
            for(var i = 0; i < itemCount; i++)
            {
                var doc = new Dictionary<string, object>
                {
                    { "Id", Guid.NewGuid() },
                    { "FirstName", $"First Name {i}" },
                    { "LastName", $"Last name {i}" },
                    { "EmployeeNumber", i }
                };

                upsertTasks.Add(Proxy.UpsertItemAsync(doc));
            }

            await Task.WhenAll(upsertTasks);
            var errors = from x in upsertTasks where !x.IsCompletedSuccessfully select x.Result;
            Assert.AreEqual(0, errors.Count(), "There must be no error responses.");

            var count = await this.GetContainerCountAsync();
            Assert.AreEqual(itemCount, count);
        }



        [TestMethod]
        [Description("Deletes an item from the container.")]
        public async Task Delete01()
        {
            object partition = Guid.NewGuid();

            var item = new Item1 { Partition = $"{partition}" };
            var upserted = await Proxy.UpsertItemAsync(item);

            await Proxy.DeleteItemAsync(item.Id, $"{partition}");

            var count = await this.GetContainerCountAsync();
            Assert.AreEqual(0, count, "All items must have been deleted from the container.");
        }



        [TestMethod]
        [Description("Stores a few items and attempts to get the first item matching a query and specifies a derived type to return the result as.")]
        public async Task GetFirstOrDefault01()
        {
            var itm1 = new ChildItem1 { DisplayName = "Child #1" };
            var itm2 = new ChildItem1 { DisplayName = "Child #2" };

            await Proxy.UpsertItemAsync(itm1);
            await Proxy.UpsertItemAsync(itm2);

            var itm1b = await Proxy.QuerySingleItemAsync<Item1>(Proxy.GetItemLinqQueryable<Item1>().Where(x => x.Id == itm1.Id), typeof(ChildItem1)) as ChildItem1;
            var itm2b = await Proxy.QuerySingleItemAsync<Item1>(Proxy.GetItemLinqQueryable<Item1>().Where(x => x.Id == itm2.Id), typeof(ChildItem1)) as ChildItem1;

            Assert.IsNotNull(itm1b);
            Assert.IsNotNull(itm2b);

            Assert.AreEqual(itm1.DisplayName, itm1b.DisplayName);
            Assert.AreEqual(itm2.DisplayName, itm2b.DisplayName);
        }


        [TestMethod]
        [Description("Stores items in the database and queries for them using a QueryDefinition object.")]
        public async Task Query01()
        {
            var partitionCount = 50;
            var itemCount = 10;

            var upsertTasks = new List<Task>();
            for(var p = 0; p < partitionCount; p++)
            {
                for(var i = 0; i < itemCount; i++)
                {
                    upsertTasks.Add(Proxy.UpsertItemAsync(new { Id = Guid.NewGuid(), Partition = p, Value = p * i }));
                }
            }

            await Task.WhenAll(upsertTasks);

            for(var p = 0; p < partitionCount; p++)
            {
                var query = new QueryDefinitionBuilder()
                    .AppendQueryText("select * from c")
                    .AppendQueryText(" where")

                    .AppendQueryText(" c[\"partition\"] = @partition")
                    .WithParameter("@partition", p)

                    .AppendQueryText(" order by c[\"value\"] desc")
                    .Build();

                var items = await Proxy.QueryItemsAsync(query).ToListAsync();
                Assert.AreEqual(itemCount, items.Count);

                var prevValue = p * itemCount + 1;
                foreach(var item in items)
                {
                    var valueProp = item.GetProperty("value");
                    var val = valueProp.GetInt32();

                    Assert.IsTrue(prevValue >= val);
                    prevValue = val;
                }

            }
        }

        [TestMethod]
        [Description("Creates a couple of items and queries for them with Linq expressions.")]
        public async Task Query02()
        {
            var partitions = new List<string>();
            var partitionCount = 100;
            var itemCountPerPartition = 20;

            var upsertTasks = new List<Task>();
            for(var p = 0; p < partitionCount; p++)
            {
                var partition = $"{Guid.NewGuid()}";
                partitions.Add(partition);

                for(var i = 0; i < itemCountPerPartition; i++)
                {
                    upsertTasks.Add(Proxy.UpsertItemAsync(new Item1 { Id = Guid.NewGuid().ToString(), Index = i, Partition = partition }));
                }
            }

            await Task.WhenAll(upsertTasks);

            foreach (var p in partitions)
            {
                var query = Proxy.Container
                    .GetItemLinqQueryable<Item1>()
                    .Where(x => x.Partition == p)
                    .OrderBy(x => x.Index)
                    .ToQueryDefinition();

                var items = await Proxy.QueryItemsAsync<Item1>(query).ToListAsync();
                Assert.AreEqual(itemCountPerPartition, items.Count);
                CollectionAssert.AllItemsAreUnique(new List<int>(from x in items select x.Index));
            }
        }

        [TestMethod]
        [Description("Creates a few items and queries data using Linq expressions.")]
        public async Task Query03()
        {
            var ids = new string[] {"1", "2", "3"};

            int i = 0;
            foreach(var id in ids)
            {
                await Proxy.UpsertItemAsync(new Item1 { Id = id, Index = i, Partition = "p" });
                i++;
            }

            var item1 = await Proxy.QuerySingleItemAsync<Item1>(Proxy.GetItemLinqQueryable<Item1>().Where(x => x.Partition == "p"));
            Assert.IsNotNull(item1);

            var item2 = await Proxy.QuerySingleItemAsync<Item1>(Proxy.GetItemLinqQueryable<Item1>().OrderByDescending(x => x.Index));
            Assert.IsNotNull(item2);
            Assert.AreEqual(ids.Last(), item2.Id);
        }

        [TestMethod]
        [Description("Creates a lot of items, queries for them in a specific order and ensures that they are returned in the right order.")]
        public async Task Query04()
        {
            int count = 1000;
            var items = new List<Item1>();

            var upsertTasks = new List<Task>();
            for(var i = 0; i < count; i++)
            {
                var item = new Item1 { Id = Guid.NewGuid().ToString(), Index = i };
                items.Add(item);
                upsertTasks.Add(Proxy.UpsertItemAsync(item));
            }

            await Task.WhenAll(upsertTasks);
            Assert.AreEqual(count, items.Count);

            var query = from x in Proxy.GetItemLinqQueryable<Item1>() orderby x.Index select x;
            await foreach(var item in Proxy.QueryItemsAsync(query))
            {
                var firstItem = items.First();
                items.RemoveAt(0);

                Assert.AreEqual(firstItem.Id, item.Id);
            }

            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        [Description("Create a set of items in the database and use the query method that supports paging with continuation tokens.")]
        public async Task Query05()
        {
            var itemCount = 1000;
            int pageItemCount = 20; // Must produce the same number of items on each page.
            var source = new List<ChildItem1>();
            for(var i = 0; i < itemCount; i++)
            {
                var item = new ChildItem1 { Index = i, DisplayName = $"Item #{i}", Partition = Guid.NewGuid().ToString() };
                source.Add(item);
                await Proxy.UpsertItemAsync(item);
            }

            var results = new List<ChildItem1>();
            string? continuationToken = null;
            var query = new QueryDefinition("select * from c order by c.id");
            do
            {
                var result = await Proxy.QueryItemsAsync<ChildItem1>(query, new QueryOptions { ContinuationToken = continuationToken, MaxItemCount = pageItemCount });
                continuationToken = result.ContinuationToken;

                Assert.AreEqual(pageItemCount, result.Items.Count());
                results.AddRange(result.Items);
            } while (continuationToken?.Length > 0);

            CollectionAssert.AreEqual(source.OrderBy(x => x.Id).ToList(), results);
        }

        [TestMethod]
        [Description("Creates a set of items and queries for the results using the generic query method.")]
        public async Task Query06()
        {
            var itemCount = 100;
            var pageItemCount = 10;
            var source = new List<Item1>();

            for(var i = 0; i < itemCount; i++)
            {
                var item = new Item1 { Index = i };
                source.Add(await Proxy.UpsertItemAsync(item));
            }

            source = source.OrderBy(x => x.Id).ToList();
            var results = new List<Item1>();
            var query = from x in Proxy.GetItemLinqQueryable<Item1>() orderby x.Id select x;

            QueryResult<Item1>? result = await Proxy.QueryItemsAsync(query, new QueryOptions { MaxItemCount = pageItemCount });
            while(result?.Items?.Count() > 0)
            {
                Assert.AreEqual(result.Items.Count(), pageItemCount);
                Assert.IsFalse(result.Items.Any(x => results.Any(y => y.Id == x.Id)), "Results must not include previously returned results.");
                results.AddRange(result.Items);
                result = await result.GetNextResultAsync();
            }

            CollectionAssert.AreEqual(source, results);
        }

        [TestMethod]
        [Description("Create an uneven number of items and page through the results and make sure that each page contains items.")]
        public async Task Query07()
        {
            for(var i = 0; i < 57; i++)
            {
                await Proxy.UpsertItemAsync(new { Id = Guid.NewGuid() });
            }

            var result = await Proxy.QueryItemsAsync(new QueryDefinition("select * from c"), new QueryOptions { MaxItemCount = 11 });
            while(null != result)
            {
                Assert.IsTrue(result.Items.Any(), "There must be items in each result.");
                result = await result.GetNextResultAsync();
            }
        }


        [TestMethod]
        [Description("Inserts one item and checks the item count.")]
        public async Task SelectCount01()
        {
            var response = await Proxy.UpsertItemAsync(new { Id = Guid.NewGuid() });
            var query = new QueryDefinition("select count(1) from c");
            var result = await Proxy.QueryItemsAsync<Dictionary<string, JsonElement>>(query).ToListAsync();
            Assert.AreEqual(1, result.Count);
            var count = result.First()["$1"];
            Assert.AreEqual(1, count.GetInt32());
        }


        [TestMethod]
        [Description("Upserts a document into the database and expects successful result.")]
        public async Task Upsert01()
        {
            var doc = new Dictionary<string, object>
            {
                { "Id", Guid.NewGuid() },
                { "Partition", Guid.NewGuid() }
            };

            var response = await Proxy.UpsertItemAsync(doc);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        [Description("Upserts an item typed as a parent type, but assumes that it will be returned as the actual type.")]
        public async Task Upsert02()
        {
            var displayName = "Child Item";
            Item1 item = new ChildItem1 { DisplayName = displayName, Partition = Guid.NewGuid().ToString() };
            Item1 upserted = await Proxy.UpsertItemAsync(item);
            Assert.IsNotNull(upserted);
            Assert.IsTrue(upserted is ChildItem1);
            var item2 = upserted as ChildItem1;
            Assert.IsNotNull(item2);
            Assert.AreEqual(displayName, item2.DisplayName);
        }



        private async Task<int> GetContainerCountAsync()
        {
            var query = new QueryDefinition("select count(1) from c");
            var result = await Proxy.QueryItemsAsync<Dictionary<string, JsonElement>>(query).ToListAsync();
            var count = result.First()["$1"];
            return count.GetInt32();
        }
    }

    public class ContainerItem
    {
        public string Id { get; set; } = null!;

        public object Partition { get; set; } = null!;
    }

    public class Item1
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Partition { get; set; } = null!;

        public int Index { get; set; }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is Item1 && this.ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return $"{this.Id}|{this.Partition}|{this.Index}";
        }
    }

    public class ChildItem1 : Item1
    {
        public string? DisplayName { get; set; }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ChildItem1 && this.ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{this.DisplayName}";
        }
    }
}