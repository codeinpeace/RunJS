using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    class JsWorkerConstructor : ClrFunction
    {
        private ScriptRunner runner;
        private List<JsWorker> workers = new List<JsWorker>();
        //private static Dictionary<string, JsWorker> sharedWorkers;
        public JsWorkerConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "Worker", runner.Engine.Object.InstancePrototype)
        {
            this.runner = runner;
        }

        [JSConstructorFunction]
        public JsPort Construct(string file)
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
                    var port = new JsPort(runner);
                    var m = new ManualResetEvent(false);
                    worker.BeginInvoke((w) =>
                    {
                        port.Receiver = worker.LocalPort;
                        worker.LocalPort.Receiver = port;

                        worker.Engine.SetGlobalValue("__port", worker.LocalPort);
                        worker.Engine.Execute("Object.defineProperty(this, 'onmessage', { get: function() { return __port.onmessage; }, set: function(value) { __port.onmessage = value; } });");
                        worker.Engine.Execute("this.addEventListener = __port.addEventListener.bind(__port);");
                        worker.Engine.Execute("this.postMessage = __port.postMessage.bind(__port);");
                        m.Set();
                    });
                    worker.Run();
                    m.WaitOne();
                    worker.BeginInvoke((w) => worker.StartWork());
                    return port;
                }
            }

            throw new JavaScriptException(Engine, "Error", "The file specified was not found in either of these locations:\n\t" + String.Join("\n\t", searchPaths));
        }
    }

    class JsWorker : ScriptRunner
    {
        string file;
        JsPort localPort;
        public JsWorker(string file)
        {
            this.file = file;
        }

        protected override void InitializeEngine()
        {
            base.InitializeEngine();

            this.localPort = new JsPort(this);
            Engine.Global["terminate"] = Engine.Global["close"];
            Engine.Global.Delete("close", false);
        }

        public void StartWork()
        {
            Engine.Execute(new FileScriptSource(file));
        }

        public JsPort LocalPort
        {
            get { return localPort; }
        }
    }
}
