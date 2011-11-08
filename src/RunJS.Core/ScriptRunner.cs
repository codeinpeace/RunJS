using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    /// <summary>
    /// ScriptRunner is the main class of the RunJS.Core library.
    /// It manages events and execution of javascript.
    /// </summary>
    public class ScriptRunner : IDisposable
    {
        private TimeoutHandler timeoutHandler;
        private AddInManager addInManager;

        private ScriptEngine engine;
        private Thread jsThread;
        private volatile Queue<Action<ScriptRunner>> executionQueue = new Queue<Action<ScriptRunner>>();
        private readonly object queueLock = new object();
        private readonly AutoResetEvent queueWait = new AutoResetEvent(false);
        private readonly ManualResetEvent exitWait = new ManualResetEvent(true);
        private bool running = false;

        private readonly Dictionary<Type, ClrFunction> typeStorage = new Dictionary<Type, ClrFunction>();

        /// <summary>
        /// Gets the script-engine.
        /// </summary>
        public ScriptEngine Engine
        {
            get { return engine; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ScriptRunner"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running
        {
            get { return running; }
        }

        /// <summary>
        /// Create a new ScriptRunner
        /// </summary>
        public ScriptRunner()
        {

        }

        /// <summary>
        /// Start the ScriptRunner
        /// </summary>
        public void Run()
        {
            if (jsThread == null)
            {
                lock (this)
                {
                    if (jsThread == null)
                    {
                        jsThread = new Thread(JsRunner);
                        jsThread.Name = "JsRunner";
                        jsThread.IsBackground = true;
                        jsThread.Start();
                        ManualResetEvent wait = new ManualResetEvent(false);
                        BeginInvoke(r => wait.Set());
                        wait.WaitOne();
                    }
                }
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (jsThread == null)
            {
                lock (this)
                {
                    if (jsThread == null)
                        return;
                }
            }
            Invoke((runner) =>
            {
                running = false;
                runner.timeoutHandler.Dispose();
                runner.addInManager.Dispose();
            });
        }

        /// <summary>
        /// Begins invokation of the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void BeginInvoke(Action<ScriptRunner> action)
        {
            lock (queueLock)
                executionQueue.Enqueue(action);
            queueWait.Set();
        }

        /// <summary>
        /// Invokes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        public void Invoke(Action<ScriptRunner> action, int millisecondsTimeout)
        {
            ManualResetEvent evt = new ManualResetEvent(false);
            BeginInvoke((engine) =>
            {
                action(engine);
                evt.Set();
            });
            evt.WaitOne(millisecondsTimeout);
        }

        /// <summary>
        /// Invokes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void Invoke(Action<ScriptRunner> action)
        {
            Invoke(action, -1);
        }

        /// <summary>
        /// Invokes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="timeout">The timeout.</param>
        public void Invoke(Action<ScriptRunner> action, TimeSpan timeout)
        {
            Invoke(action, Convert.ToInt32(timeout.TotalMilliseconds));
        }

        /// <summary>
        /// Executes the specified javascript.
        /// </summary>
        /// <param name="javascript">The javascript.</param>
        /// <returns>The result of the execution, or undefined if there was none.</returns>
        public object Execute(string javascript)
        {
            object ret = null;
            JavaScriptException ex = null;
            Invoke((runner) =>
            {
                try
                {
                    ret = runner.Engine.Evaluate(javascript);
                }
                catch (JavaScriptException e)
                {
                    ex = e;
                }
            });
            if (ex != null)
                throw ex;
            return ret;
        }

        /// <summary>
        /// Executes the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The result of the execution, or undefined if there was none.</returns>
        public object Execute(FunctionInstance function, params object[] parameters)
        {
            object ret = null;
            Invoke((runner) =>
            {
                ret = function.CallLateBound(runner.Engine.Global, parameters);
            });
            return ret;
        }

        /// <summary>
        /// Constructs a T with the specified parameters.
        /// </summary>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The constructed object.</returns>
        public ObjectInstance Construct<T>(params object[] parameters) where T : ObjectInstance
        {
            if (running && typeStorage.Count == 0)
            {
                ManualResetEvent wait = new ManualResetEvent(false);
                BeginInvoke(runner => wait.Set());
                wait.WaitOne();
            }
            Type t = typeof(T);
            if (!typeStorage.ContainsKey(t))
                throw new KeyNotFoundException("Type " + t.ToString() + " not found.");
            return typeStorage[typeof(T)].ConstructLateBound(parameters);
        }

        #region Private members
        private void JsRunner()
        {
            running = true;
            exitWait.Reset();
            engine = new ScriptEngine();

            typeStorage.Clear();
            typeStorage.Add(typeof(JsEventObject), new JsEventObjectConstructor(this));

            timeoutHandler = new TimeoutHandler(this);
            addInManager = new AddInManager(this);

            foreach (var fn in timeoutHandler.Properties.Where(p => p.Value is FunctionInstance)
                .Where(p => ((FunctionInstance)p.Value).Name.Contains("Timeout") || ((FunctionInstance)p.Value).Name.Contains("Interval")))
            {
                engine.Global[fn.Name] = fn.Value;
            }

            foreach (var fn in addInManager.Properties.Where(p => p.Value is FunctionInstance)
                .Where(p => ((FunctionInstance)p.Value).Name == "require"))
            {
                engine.Global[fn.Name] = fn.Value;
            }

            engine.SetGlobalFunction("close", new Action(() => running = false));
            engine.Global.SetPropertyValue("Worker", new JsWorkerConstructor(this), false);

            InitializeEngine();

            foreach (var file in Directory.GetFiles(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Scripts"),
                    "*.js", SearchOption.TopDirectoryOnly))
                engine.Execute(new FileScriptSource(file));

            while (running)
            {
                queueWait.WaitOne();

                Queue<Action<ScriptRunner>> q = new Queue<Action<ScriptRunner>>();
                lock (queueLock)
                    while (executionQueue.Count > 0)
                        q.Enqueue(executionQueue.Dequeue());

                while (q.Count > 0)
                    q.Dequeue().Invoke(this);
            }
            lock (this)
                jsThread = null;
            exitWait.Set();
        }

        /// <summary>
        /// Initializes the engine.
        /// </summary>
        protected virtual void InitializeEngine() { }
        #endregion

        #region IDisposable
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
        #endregion

        /// <summary>
        /// Waits for close.
        /// </summary>
        public void WaitForClose()
        {
            exitWait.WaitOne();
        }
    }
}
