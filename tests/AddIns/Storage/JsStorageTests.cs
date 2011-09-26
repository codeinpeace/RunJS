using NUnit.Framework;
using RunJS.Core;
using Should.Fluent;

namespace RunJS.AddIn.Storage.Tests
{
    [TestFixture]
    public class JsStorageTests
    {
        ScriptRunner scriptRunner;

        [SetUp]
        public void TestStart()
        {
            scriptRunner = new ScriptRunner();
            scriptRunner.Run();
        }

        [TearDown]
        public void TestEnd()
        {
            scriptRunner.Dispose();
        }

        [Test]
        public void StorageModuleExists()
        {
            scriptRunner.Execute("require('storage')").Should().Be.OfType<StorageConstructor>();
        }
    }
}
