using Denomica.Cosmos.Extensions.Tests.Configuration;
using Denomica.Cosmos.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

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
        }

        private static ServiceProvider ServiceProvider = null!;
        private static ConnectionOptions Options = null!;
        private static Container DataContainer = null!;


        [TestInitialize]
        public async Task TestInit()
        {
            var items = DataContainer.QueryAsync<ContainerItem>(new QueryDefinition("select c.id,c.partition from c"));
            await foreach(var item in items)
            {
                await DataContainer.DeleteItemAsync(item.Id, item.Partition);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            
        }

    }

    public class ContainerItem
    {
        public string Id { get; set; } = null!;

        public string Partition { get; set; } = null!;

    }
}