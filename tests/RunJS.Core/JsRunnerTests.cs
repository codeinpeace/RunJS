using System;
using System.Threading;
using System.Threading.Tasks;
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

        [Test]
        public void PromisesContinues()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    set = true;
                    wait.Set();
                }));
                scriptRunner.Engine.SetGlobalValue("test", Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                }).AsPromise(scriptRunner));
            });
            scriptRunner.Execute("test.continueWith(fin);");
            wait.WaitOne(200);
            set.Should().Be.True();
        }

        [Test]
        public void PromisesReturnsValue()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            bool set = false;
            string value = null;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action<string>(val =>
                {
                    value = val;
                    set = true;
                    wait.Set();
                }));
                scriptRunner.Engine.SetGlobalValue("test", Task.Factory.StartNew<string>(() => "test_value").AsPromise(runner, str => scriptRunner.Engine.String.Construct(str)));
            });
            scriptRunner.Execute("test.continueWith(fin);");
            wait.WaitOne(200);
            set.Should().Be.True();
            value.Should().Equal("test_value");
        }

        [Test]
        public void EventObjectWorks()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            EventsTestsObject eventTest = new EventsTestsObject(scriptRunner);
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    set = true;
                    wait.Set();
                }));
                scriptRunner.Engine.SetGlobalValue("test", eventTest);
            });
            scriptRunner.Execute("test.listen('test', fin);");
            eventTest.Test();
            wait.WaitOne(100);
            set.Should().Be.True();
        }

        [Test]
        public void ConstructedEventObjectWorks()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            EventsTest2Constructor constructor = new EventsTest2Constructor(scriptRunner);
            EventsTest2 eventTest = null;
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    set = true;
                    wait.Set();
                }));
                eventTest = (EventsTest2)constructor.ConstructLateBound();
                scriptRunner.Engine.SetGlobalValue("test", eventTest);
            });
            scriptRunner.Execute("test.listen('test', fin);");
            eventTest.Test();
            wait.WaitOne(100);
            set.Should().Be.True();
        }

        [Test]
        public void EventObjectFiresWithArguments()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            EventsTestsObject eventTest = new EventsTestsObject(scriptRunner);
            string value = null;
            bool set = false;
            scriptRunner.BeginInvoke((runner) =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action<string>(val =>
                {
                    value = val;
                    set = true;
                    wait.Set();
                }));
                scriptRunner.Engine.SetGlobalValue("test", eventTest);
            });
            scriptRunner.Execute("test.listen('test', fin);");
            eventTest.Test("test_value");
            wait.WaitOne(100);
            set.Should().Be.True();
            value.Should().Equal("test_value");
        }

        [Test]
        public void EventsAreSettableAsObject()
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            int count = 0;
            EventsTestsObject eventTest = new EventsTestsObject(scriptRunner);
            scriptRunner.BeginInvoke(runner =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action(() =>
                {
                    if (++count > 1)
                        wait.Set();
                }));
                scriptRunner.Engine.SetGlobalValue("test", eventTest);
            });
            scriptRunner.Execute("test.listen({test:fin,test2:fin});");
            eventTest.Test();
            eventTest.Test2();
            wait.WaitOne(100);
            count.Should().Equal(2);
        }

        [Test]
        public void JsDefinedPromiseSourcesWorks()
        {
            int value = 0;
            scriptRunner.BeginInvoke(runner =>
            {
                scriptRunner.Engine.SetGlobalFunction("fin", new Action<int>(num =>
                {
                    value = num;
                }));
            });
            scriptRunner.Execute("var p = new PromiseSource();");
            scriptRunner.Execute("p.getPromise().continueWith(function(n) { return n + 1; }).continueWith(function(n) { return n + 1; }).continueWith(fin);");
            scriptRunner.Execute("p.finalize(1);");
            value.Should().Equal(3);
        }
    }
}
