using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions.Tests
{
    [TestClass]
    public class MiscTests
    {

        [TestMethod]
        public void CreateDefinitionBuilder01()
        {
            // Make sure that the value provider is never called if condition is false.
            var builder = new QueryDefinitionBuilder()
                .AppendQueryText("select * from c")
                .WithParameterIf("@param", () => throw new Exception(), false);
        }

        [TestMethod]
        public async Task CreateDefinitionBuilder02()
        {
            // Make sure that the value provider is never called if condition is false.
            var builder = await new QueryDefinitionBuilder()
                .AppendQueryText("select * from c")
                .WithParameterIfAsync("@param", async () => await Task.FromResult("value"), async () => false);
        }
    }
}
