using System;
using System.IO;
using System.Reflection;
using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    class JsWorkerConstructor : ClrFunction
    {
        private ScriptRunner runner;
        public JsWorkerConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "Worker", runner.Engine.Object.InstancePrototype)
        {
            this.runner = runner;
        }

        [JSConstructorFunction]
        public JsWorkerWithPort Construct(string file)
        {
            var searchPaths = new string[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RunJS", "Scripts", file),
                Path.Combine(Environment.CurrentDirectory, file),
                Path.Combine(Environment.CurrentDirectory, "Scripts", file),
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Scripts", file)
            };
            foreach (var s in searchPaths)
            {
                if (File.Exists(s))
                {
                    var worker = new JsWorker(s);
                    return new JsWorkerWithPort(runner, worker);
                }
            }

            throw new JavaScriptException(Engine, "Error", "The file specified was not found in either of these locations:\n\t" + String.Join("\n\t", searchPaths));
        }
    }

    class JsWorker : ScriptRunner
    {
        string file;
        public JsWorker(string file)
        {
            this.file = file;
        }

        protected override void InitializeEngine()
        {
            base.InitializeEngine();

            Engine.Global["terminate"] = Engine.Global["close"];
            Engine.Global.Delete("close", false);
        }

        public void StartWork()
        {
            Engine.Execute(new FileScriptSource(file));
        }
    }

    class JsWorkerWithPort : JsPort
    {
        private JsWorker worker;

        public JsWorkerWithPort(ScriptRunner runner, JsWorker worker)
            : base(runner)
        {
            this.worker = worker;

            worker.Run();

            worker.BeginInvoke((w) =>
            {
                this.receiver = new JsPort(worker, this);
                w.Engine.SetGlobalValue("__port", receiver);
                w.Engine.Execute("Object.defineProperty(this, 'onconnect', { get: function() { return __port.onconnect; }, set: function(value) { __port.onconnect = value; } });");
                w.Engine.Global.SetPropertyValue("addEventListener", ((FunctionInstance)receiver.GetPropertyValue("addEventListener")).Bind(receiver), false);
                w.Engine.Global.SetPropertyValue("postMessage", ((FunctionInstance)receiver.GetPropertyValue("postMessage")).Bind(receiver), false);
                worker.StartWork();
            });

        }

        public override string ToString()
        {
            return "Worker object;\n\t" + String.Join(", ", ObjectConstructor.GetOwnPropertyNames(this).ElementValues);
        }

    }
}
