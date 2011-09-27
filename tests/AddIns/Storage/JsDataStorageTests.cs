using NUnit.Framework;
using RunJS.Core;
using Should.Fluent;

namespace RunJS.AddIn.Storage.Tests
{
    [TestFixture]
    public class JsDataStorageTests
    {
        ScriptRunner scriptRunner;
        StorageInstance storage;

        [SetUp]
        public void TestStart()
        {
            scriptRunner = new ScriptRunner();
            scriptRunner.Run();
            scriptRunner.Execute("var Storage = require('storage');");
            storage = scriptRunner.Execute("new Storage(Storage.inMemory)") as StorageInstance;
            if (storage == null)
                Assert.Ignore("Storage not found.");
            scriptRunner.Invoke((r) => r.Engine.SetGlobalValue("s", storage));
        }

        [TearDown]
        public void TestEnd()
        {
            storage.Drop();
            scriptRunner.Dispose();
        }

        [Test]
        public void CanCreateNamedProxy()
        {
            StorageInstance.StorageContainerInstance namedProxy = scriptRunner.Execute("s.getStorage('test')") as StorageInstance.StorageContainerInstance;
            namedProxy.Should().Not.Be.Null();
        }

        [Test]
        public void CanStoreData()
        {
            scriptRunner.Execute("var sp = s.getStorage('test');");
            scriptRunner.Execute("sp.put(null, {test:'test'})").Should().Be.OfType<string>();
            scriptRunner.Execute("sp.put(null, {test:'test2'})").Should().Be.OfType<string>();
            scriptRunner.Execute("sp.put('per', {test:'test3',name:'per'})").Should().Be.OfType<string>();
        }

        [Test]
        public void CanRetrieveById()
        {
            scriptRunner.Execute("var sp = s.getStorage('test');");
            scriptRunner.Execute("sp.put('pal', {name:'palt'})").Should().Equal("pal");
            scriptRunner.Execute("sp.get('pal').name").Should().Equal("palt");
        }

        [Test]
        public void CanRetrieveByQuery()
        {
            //if (!AppDomain.CurrentDomain.IsHomogenous)
            //    Assert.Ignore("Current AppDomain is not homogenous, thus the tests cannot be run.");
            scriptRunner.Execute("var sp = s.getStorage('test');");

            scriptRunner.Execute("sp.put(null, {name:'palt',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt2',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt3',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt4',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt5',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt6',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt7',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt8',age:10})");

            scriptRunner.Execute("sp.put(null, {name:'palt',age:11})");
            scriptRunner.Execute("sp.put(null, {name:'palt',age:12})");
            scriptRunner.Execute("sp.put(null, {name:'palt',age:13})");
            scriptRunner.Execute("sp.put(null, {name:'palt',age:14})");

            scriptRunner.Execute("sp.query({age:10}).length").Should().Equal(8);
            scriptRunner.Execute("sp.query({name:'palt'}).length").Should().Equal(5);
            scriptRunner.Execute("sp.query({name:'palt',age:10}).length").Should().Equal(1);
            scriptRunner.Execute("sp.query({name:'palt'},{age:10}).length").Should().Equal(12);
        }
    }
}
