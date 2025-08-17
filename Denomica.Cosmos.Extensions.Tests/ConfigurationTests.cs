using Denomica.Cosmos.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions.Tests
{
    [TestClass]
    public class ConfigurationTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ConnectionOptions = new CosmosConnectionOptions
            {
                ConnectionString = $"{context.Properties["connectionString"]}",
                DatabaseId = $"{context.Properties["databaseId"]}",
                ContainerId = $"{context.Properties["containerId"]}"
            };
        }

        private static CosmosConnectionOptions ConnectionOptions { get; set; } = default!;


        [TestMethod]
        public void Configure01()
        {
            var provider = new ServiceCollection()
                .AddCosmosExtensions()
                .WithConnectionOptions((opt, sp) =>
                {
                    opt.ConnectionString = ConnectionOptions.ConnectionString;
                    opt.DatabaseId = ConnectionOptions.DatabaseId;
                    opt.ContainerId = ConnectionOptions.ContainerId;
                })
                .Services

                .BuildServiceProvider();

            var client = provider.GetService<CosmosClient>();
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void Configure02()
        {
            var provider = new ServiceCollection()
                .AddCosmosExtensions()
                .WithConnectionOptions((opt, sp) =>
                {
                    opt.ConnectionString = ConnectionOptions.ConnectionString;
                    opt.DatabaseId = ConnectionOptions.DatabaseId;
                    opt.ContainerId = ConnectionOptions.ContainerId;
                })
                .WithCosmosClientOptions((opt, sp) =>
                {
                    opt.SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                        IgnoreNullValues = true,
                        Indented = true
                    };
                })
                .Services

                .BuildServiceProvider();

            var client = provider.GetRequiredService<CosmosClient>();
            Assert.IsTrue(client.ClientOptions.SerializerOptions.Indented);
            Assert.IsTrue(client.ClientOptions.SerializerOptions.IgnoreNullValues);
            Assert.AreEqual(CosmosPropertyNamingPolicy.CamelCase, client.ClientOptions.SerializerOptions.PropertyNamingPolicy);
        }
    }
}
