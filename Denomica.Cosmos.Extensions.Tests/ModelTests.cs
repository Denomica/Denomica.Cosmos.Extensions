using Denomica.Cosmos.Extensions.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions.Tests
{
    [TestClass]
    public class ModelTests
    {

        [TestMethod]
        public void TestModel01()
        {
            var ts = DateTimeOffset.Now;
            var model = new SyntheticPartitionKeyDocument();
            Assert.IsTrue(model.Created > ts);
            Assert.IsTrue(model.Modified > ts);
        }

        [TestMethod]
        public void TestModel02()
        {
            var p1 = new Person { FirstName = "John", LastName = "Doe" };
            var p2 = new Person { FirstName = "Jane", LastName = "Doe" };

            Assert.AreEqual(p1.Partition, p2.Partition);
            Assert.AreEqual("Person|Doe", p1.Partition);
        }

        [TestMethod]
        public void TestModel03()
        {
            var m = new SyntheticPartitionKeyDocument();
            Assert.AreEqual(nameof(SyntheticPartitionKeyDocument), m.Partition);
        }


        [TestMethod]
        public void TestModelEvents01()
        {
            var model = new SyntheticPartitionKeyDocument();
            DateTimeOffset created = default;
            model.PropertyValueChanged += (s, e) =>
            {
                created = (DateTimeOffset)e.NewValue;
            };

            var created2 = model.Created;
            Assert.AreEqual(created, created2);
        }


    }


    public class Person : SyntheticPartitionKeyDocument
    {

        public string FirstName
        {
            get { return this.GetProperty<string>(nameof(FirstName)); }
            set { this.SetProperty(nameof(FirstName), value); }
        }

        [PartitionKeyProperty(1)]
        public string LastName
        {
            get { return this.GetProperty<string>(nameof(LastName)); }
            set { this.SetProperty(nameof(LastName), value); }
        }

        [PartitionKeyProperty(0)]
        public override string Type { get => base.Type; set => base.Type = value; }

        protected override string PartitionKeyPropertySeparator => "|";
    }
}
