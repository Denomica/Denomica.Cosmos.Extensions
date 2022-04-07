using Denomica.Cosmos.Extensions.Tests.Configuration;
using Denomica.Cosmos.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace Denomica.Cosmos.Extensions.Tests
{
    [TestClass]
    public class DataAccessTests
    {

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var configRoot = new ConfigurationBuilder()
                .SetBasePath(context.TestDeploymentDir)
                .AddJsonFile("test.settings.json", optional: false)

                .Build();

            ServiceProvider = new ServiceCollection()
                .AddSingleton(configRoot)
                .AddSingleton<IConfiguration>(configRoot)
                .AddSingleton<ConnectionOptions>(sp =>
                {
                    var root = sp.GetRequiredService<IConfiguration>();
                    var config = new ConnectionOptions();
                    root.Bind("denomica:cosmos:extensions:tests", config);
                    return config;
                })
                .AddSingleton<CosmosClient>(sp =>
                {
                    var options = sp.GetRequiredService<ConnectionOptions>();
                    return new CosmosClient(
                        options.ConnectionString, 
                        clientOptions: new CosmosClientOptions
                        {
                            SerializerOptions = new CosmosSerializationOptions
                            {
                                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                            }
                        }
                    );
                })
                .AddSingleton<Container>(sp =>
                {
                    var client = sp.GetRequiredService<CosmosClient>();
                    var options = sp.GetRequiredService<ConnectionOptions>();

                    return client.GetContainer(options.DatabaseId, options.ContainerId);
                })
                .BuildServiceProvider();

            Options = ServiceProvider.GetRequiredService<ConnectionOptions>();
            DataContainer = ServiceProvider.GetRequiredService<Container>();
            Proxy = new ContainerProxy(DataContainer);
        }

        private static ServiceProvider ServiceProvider = null!;
        private static ConnectionOptions Options = null!;
        private static Container DataContainer = null!;
        private static ContainerProxy Proxy = null!;


        [TestInitialize]
        public async Task TestInit()
        {
            var tasks = new List<Task<ResponseMessage>>();
            var items = Proxy.QueryItemsAsync<ContainerItem>(new QueryDefinition("select c.id,c.partition from c"));
            await foreach(var item in items)
            {
                tasks.Add(Proxy.DeleteItemAsync(item.Id, item.Partition));
            }

            await Task.WhenAll(tasks);

            var errors = from x in tasks where !x.Result.StatusCode.IsSuccess() select x.Result;
            if(errors.Any())
            {
                throw new Exception("Unable to clear all items from container before running unit test.");
            }
        }



        [TestMethod]
        [Description("Bulk loads the container ensuring that no HTTP 429 is returned.")]
        public async Task BulkLoad01()
        {
            int itemCount = 2000;
            var upsertTasks = new List<Task<ItemResponse<Dictionary<string, object>>>>();
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
            var errors = from x in upsertTasks where !x.Result.StatusCode.IsSuccess() select x.Result;
            Assert.AreEqual(0, errors.Count(), "There must be no error responses.");

            var count = await this.GetContainerCountAsync();
            Assert.AreEqual(itemCount, count);
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
            Assert.IsTrue(response.StatusCode.IsSuccess());
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

        public string Partition { get; set; } = null!;

    }

}