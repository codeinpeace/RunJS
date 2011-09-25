using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jurassic;
using System.Threading;
using System.IO;
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
        private bool running = false;

        /// <summary>
        /// Gets the script-engine.
        /// </summary>
        internal protected ScriptEngine Engine
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
            running = false;
            Invoke((runner) =>
            {
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
            Invoke((runner) =>
            {
                ret = runner.Engine.Evaluate(javascript);
            });
            return ret;
        }

        /// <summary>
        /// Executes the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The result of the execution, or undefined if there was none.</returns>
        public object Execute(FunctionInstance function)
        {
            object ret = null;
            Invoke((runner) =>
            {
                ret = function.CallLateBound(runner.Engine.Global);
            });
            return ret;
        }

        #region Private members
        private void JsRunner()
        {
            running = true;
            engine = new ScriptEngine();

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

            foreach (var file in Directory.GetFiles("Scripts", "*.js", SearchOption.TopDirectoryOnly))
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
        }
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
    }
}
