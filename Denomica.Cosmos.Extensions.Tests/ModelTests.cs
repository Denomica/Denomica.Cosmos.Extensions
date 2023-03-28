using Denomica.Cosmos.Extensions.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
        public void TestModel04()
        {
            var m = new TestDocument();
            Assert.AreEqual(nameof(TestDocument), m.Partition);
        }

        [TestMethod]
        public void TestModel05()
        {
            var m = new TestDocument2 { Foo = "bar" };
            Assert.AreEqual($"{nameof(TestDocument2)}/{m.Foo}", m.Partition);
        }

        [TestMethod]
        public void TestModel06()
        {
            var m = new TestDocument3 { Foo = "bar" };
            Assert.AreEqual(m.Foo, m.Partition);
        }

        [TestMethod]
        public void TestModel07()
        {
            var m = new TestDocument4 { Foo = "bar" };
            Assert.AreEqual(m.Foo, m.Partition);
        }

        [TestMethod]
        public void TestModel08()
        {
            var m = new TestDocument5 { Timestamp = new DateTime(2023, 3, 9) };
            Assert.AreEqual("20230309", m.Partition);
        }

        [TestMethod]
        public void TestModel09()
        {
            var m1 = new TestDocument6 { Index = 4 };
            var m2 = new TestDocument6 { Index = 45 };
            var m3 = new TestDocument6 { Index = 183 };


            Assert.AreEqual("04", m1.Partition);
            Assert.AreEqual("45", m2.Partition);
            Assert.AreEqual("183", m3.Partition);
        }

        [TestMethod]
        public void TestModel10()
        {
            var m = new TestDocument7 { D1 = 5.3, D2 = 12.5 };
            Assert.AreEqual("5.3/12,5", m.Partition);
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

    public class TestDocument : SyntheticPartitionKeyDocument
    {

        [PartitionKeyProperty(0)]
        public override string Type { get => base.Type; set => base.Type = value; }
    }

    public class TestDocument2 : TestDocument
    {
        [PartitionKeyProperty(1)]
        public string Foo
        {
            get { return this.GetProperty<string>(nameof(Foo)); }
            set { this.SetProperty(nameof(Foo), value); }
        }
    }

    public class TestDocument3 : TestDocument
    {
        [PartitionKeyProperty(1)]
        public string Foo
        {
            get { return this.GetProperty<string>(nameof(Foo)); }
            set { this.SetProperty(nameof(Foo), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }

    public class TestDocument4 : TestDocument
    {
        [PartitionKeyProperty(1)]
        public string Foo
        {
            get { return this.GetProperty<string>(nameof(Foo)); }
            set { this.SetProperty(nameof(Foo), value); }
        }

        public override string Type { get => base.Type; set => base.Type = value; }
    }

    public class TestDocument5 : TestDocument
    {
        [PartitionKeyProperty(0, formatString: "yyyyMMdd")]
        public DateTime Timestamp
        {
            get { return this.GetProperty<DateTime>(nameof(Timestamp)); }
            set { this.SetProperty(nameof(Timestamp), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }

    public class TestDocument6 : TestDocument
    {
        [PartitionKeyProperty(0, formatString: "D2")]
        public int Index
        {
            get { return this.GetProperty<int>(nameof(Index)); }
            set { this.SetProperty(nameof(Index), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }

    public class TestDocument7 : TestDocument
    {
        [PartitionKeyProperty(0, formatString: "F1", culture: "en-US")]
        public double D1
        {
            get { return this.GetProperty<double>(nameof(D1)); }
            set { this.SetProperty(nameof(D1), value); }
        }

        [PartitionKeyProperty(1, formatString: "F1", culture: "fi-FI")]
        public double D2
        {
            get { return this.GetProperty<double>(nameof(D2)); }
            set { this.SetProperty(nameof(D2), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
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
