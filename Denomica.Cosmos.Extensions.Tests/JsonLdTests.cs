using Denomica.Cosmos.Extensions.Tests.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Schema.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Denomica.Cosmos.Extensions.JsonLd;
using Denomica.Cosmos.Extensions.JsonLd.Model;
using Denomica.Text.Json;
using Denomica.Cosmos.Extensions.JsonLd.Services;
using Microsoft.Extensions.DependencyInjection;
using Denomica.Cosmos.Extensions.JsonLd.Configuration;

namespace Denomica.Cosmos.Extensions.Tests
{
    using JsonDictionary = Dictionary<string, object?>;
    using JsonList = List<object?>;

    [TestClass]
    public class JsonLdTests
    {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            var provider = new ServiceCollection()
                .AddCosmosExtensions()
                .WithConnectionOptions((opt, sp) =>
                {
                    opt.ConnectionString = this.TestContext.Properties["connectionString"]?.ToString() ?? throw new NullReferenceException("'connectionString' is not set in test context properties.");
                    opt.DatabaseId = this.TestContext.Properties["databaseId"]?.ToString() ?? throw new NullReferenceException("'databaseId' is not set in test context properties.");
                    opt.ContainerId = this.TestContext.Properties["containerId"]?.ToString() ?? throw new NullReferenceException("'containerId' is not set in test context properties.");
                })
                .WithJsonLdDefaults()
                .Services

                .BuildServiceProvider();

            this.Normalizer = provider.GetRequiredService<DataNormalizer>();
        }

        private DataNormalizer Normalizer;

        [TestMethod]
        public async Task Normalize001()
        {
            var elem = JsonDocument.Parse(Resources.Thing001).RootElement;
            var normalized = await this.Normalizer.NormalizeAsync(elem);
            var d = JsonUtil.CreateDictionary(normalized);

            Assert.AreEqual("https://schema.org", d["@context"]);
            Assert.IsFalse(d.ContainsKey("@id"));
        }

        [TestMethod]
        public async Task Normalize002()
        {
            var elem = JsonDocument.Parse(Resources.Thing002).RootElement;
            var normalized = await this.Normalizer.NormalizeAsync(elem);
            var d = JsonUtil.CreateDictionary(normalized);

            Assert.IsTrue(d.ContainsKey("@id"));
            var arr = d["description"] as JsonList;
            Assert.IsNotNull(arr);
            var desc = arr.First() as JsonDictionary;
            Assert.IsNotNull(desc);
            Assert.IsFalse(desc.ContainsKey("@context"));
        }
    }
}
