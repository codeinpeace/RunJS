using System;
using System.Threading;
using Jurassic;
using Jurassic.Library;
using NUnit.Framework;
using Should.Fluent;

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

        [Test]
        public void TimeoutWorks()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    wait.Set();
                    set = true;
                }));
            });
            scriptRunner.Execute("setTimeout(fin, 100);");
            wait.WaitOne(200);
            set.Should().Be.True();
        }

        [Test]
        public void ClearingTimeoutWorks()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    wait.Set();
                    set = true;
                }));
            });
            int id = (int)scriptRunner.Execute("setTimeout(fin, 100);");
            scriptRunner.Execute("clearTimeout(" + id + ");");
            wait.WaitOne(200);
            set.Should().Be.False();
        }

        [Test]
        public void IntervalWorks()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            bool set = false;
            int count = 0;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    if (++count > 3)
                    {
                        wait.Set();
                        set = true;
                    }
                }));
            });
            scriptRunner.Execute("setInterval(fin, 20);");
            wait.WaitOne(150);
            set.Should().Be.True();
        }

        [Test]
        public void ClearingIntervalWorks()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            bool set = false;
            int count = 0;
            int id = 0;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    if (count == 1)
                    {
                        scriptRunner.Engine.Evaluate("clearInterval(" + id + ")");
                    }
                    if (++count > 3)
                    {
                        wait.Set();
                        set = true;
                    }
                }));
            });
            id = (int)scriptRunner.Execute("setInterval(fin, 20);");
            wait.WaitOne(150);
            set.Should().Be.False();
        }

        [Test]
        public void WebworkersExists()
        {
            scriptRunner.Execute("Worker").Should().Not.Be.OfType<Undefined>();
        }

        [Test, ExpectedException(typeof(JavaScriptException))]
        public void WorkersWithInvalidFilesThrows()
        {
            scriptRunner.Execute("new Worker('some_inexisting_file')");
        }

        [Test]
        public void CanCreateWorker()
        {
            scriptRunner.Execute("new Worker('Tests/worker1.js')").Should().Not.Be.OfType<Undefined>();
        }

        [Test]
        public void WorkersDoWork()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            int result = 0;
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action<int>(res =>
                {
                    result = res;
                    set = true;
                    wait.Set();
                }));
                scriptRunner.Engine.SetGlobalFunction("log", new Action<object>(obj => Console.WriteLine(obj.ToString())));
            });
            scriptRunner.Execute("var newFin = function(evt) { fin(evt.data.toString().toInt()); }");
            scriptRunner.Execute("var w = new Worker('Tests/worker1.js'); w.onmessage = newFin; w.postMessage(5);");
            wait.WaitOne(200);
            set.Should().Be.True();
            result.Should().Equal(10);
        }
    }
}
