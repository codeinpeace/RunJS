﻿using NUnit.Framework;
using RunJS.Core;
using Should.Fluent;

namespace RunJS.AddIn.Storage.Tests
{
    [TestFixture]
    public class JsStorageTests
    {
        ScriptRunner scriptRunner;
        StorageInstance storage;

        [SetUp]
        public void TestStart()
        {
            scriptRunner = new ScriptRunner();
            scriptRunner.Run();
        }

        [TearDown]
        public void TestEnd()
        {
            if (storage != null)
                storage.Drop();
            scriptRunner.Dispose();
        }

        [Test]
        public void StorageModuleExists()
        {
            scriptRunner.Execute("require('storage')").Should().Be.OfType<StorageConstructor>();
        }

        [Test]
        public void CanCreateStorage()
        {
            scriptRunner.Execute("this.Storage = require('storage');");
            storage = scriptRunner.Execute("new Storage()") as StorageInstance;
            storage.Should().Not.Be.Null();
        }

        [Test]
        public void CanCreateNamedStorage()
        {
            scriptRunner.Execute("this.Storage = require('storage');");
            storage = scriptRunner.Execute("new Storage('egrss')") as StorageInstance;
            storage.Should().Not.Be.Null();
        }

        [Test]
        public void CanCreateAbsolutePositionedStorage()
        {
            scriptRunner.Execute("this.Storage = require('storage');");
            storage = scriptRunner.Execute("new Storage('egrss', true)") as StorageInstance;
            storage.Should().Not.Be.Null();
        }
    }
}
