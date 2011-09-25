using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Should.Fluent;
using Jurassic.Library;
using Jurassic;

namespace RunJS.Core
{
    [TestFixture]
    public class JsRunnerTests
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
        public void RunnerCanExecuteJavascript()
        {
            scriptRunner.Execute("2 + 5").Should().Equal(7);
            scriptRunner.Execute("(function() { return 2 + 8; })()").Should().Equal(10);
            scriptRunner.Execute("(function() { return {number:5}; })()").Should().Be.OfType<ObjectInstance>(); // bug in jurassic prevents me from doing Execute("{}")
            scriptRunner.Execute("[1,2,3]").Should().Be.OfType<ArrayInstance>();
        }

        [Test]
        public void MooToolsIsLoaded()
        {
            scriptRunner.Execute("MooTools").Should().Not.Be.OfType<Undefined>();
            scriptRunner.Execute("Class").Should().Not.Be.OfType<Undefined>();
        }
        
        [Test]
        public void ScriptCanTerminateRunner()
        {
            scriptRunner.Execute("close();");
            scriptRunner.Running.Should().Be.False();
        }

        [Test]
        public void AddInManagerExists()
        {
            scriptRunner.Execute("require").Should().Be.AssignableFrom<FunctionInstance>();
        }

        [Test]
        public void AddInManagerReturnsUndefinedOnInvalidArguments()
        {
            scriptRunner.Execute("require('per_og_paal')").Should().Be.OfType<Undefined>();
            scriptRunner.Execute("require('per_og_paal', 'nr1')").Should().Be.OfType<Undefined>();
        }

        [Test]
        public void AddInManagerReturnsFunctionOnValidArguments()
        {
            scriptRunner.Execute("require('utils', 'loader')").Should().Be.AssignableFrom<ClrFunction>();
        }
    }
}
