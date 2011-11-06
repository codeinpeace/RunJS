using System;
using System.Threading;
using System.Threading.Tasks;
using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    /// <summary>
    /// Represents a Task in JS
    /// </summary>
    public class JsPromise : ObjectInstance
    {
        private Task<object> task;
        private ScriptRunner runner;
        /// <summary>
        /// Initializes a new instance of the <see cref="JsPromise"/> class.
        /// </summary>
        /// <param name="runner">The runner.</param>
        /// <param name="task">The task.</param>
        public JsPromise(ScriptRunner runner, Task<object> task)
            : base(runner.Engine.Object.Prototype)
        {
            this.task = task;
            this.runner = runner;
            PopulateFunctions();
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        [JSProperty(Name = "result")]
        public object Result
        {
            get { return task.Status == TaskStatus.RanToCompletion ? task.Result : null; }
        }

        /// <summary>
        /// Adds a continuation function.
        /// </summary>
        /// <param name="fn">The function.</param>
        /// <returns>The resulting JsPromise.</returns>
        [JSFunction(Name = "continueWith")]
        public JsPromise ContinueWith(object fn)
        {
            if (fn == null)
                throw new JavaScriptException(Engine, "Error", "Function can't be null or undefined.");
            if (!(fn is FunctionInstance))
                throw new JavaScriptException(Engine, "Error", "Function must be a function (was " + fn.GetType().Name + ")");

            var function = fn as FunctionInstance;

            return task.ContinueWith(t =>
            {
                object result = null;
                ManualResetEvent m = new ManualResetEvent(false);
                runner.BeginInvoke(r =>
                {
                    result = function.CallLateBound(Engine.Global, t.Result);
                    m.Set();
                });
                m.WaitOne();
                return result;
            }).AsPromise(runner);
        }
    }

    /// <summary>
    /// Extensions for JsPromise
    /// </summary>
    public static class JsPromiseExtensions
    {
        /// <summary>
        /// Converts a Task to a JsPromise.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="runner">The runner.</param>
        /// <returns>The resulting JsPromise.</returns>
        public static JsPromise AsPromise(this Task<ObjectInstance> task, ScriptRunner runner)
        {
            return new JsPromise(runner, task.ContinueWith(t => (object)t.Result));
        }

        /// <summary>
        /// Converts a Task to a JsPromise.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task">The task.</param>
        /// <param name="runner">The runner.</param>
        /// <param name="converter">The converter.</param>
        /// <returns>The resulting JsPromise.</returns>
        public static JsPromise AsPromise<T>(this Task<T> task, ScriptRunner runner, Func<T, ObjectInstance> converter)
        {
            return new JsPromise(runner, task.ContinueWith(t => (object)converter(t.Result)));
        }

        /// <summary>
        /// Converts a Task to a JsPromise.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="runner">The runner.</param>
        /// <returns>The resulting JsPromise.</returns>
        public static JsPromise AsPromise(this Task task, ScriptRunner runner)
        {
            return new JsPromise(runner, task.ContinueWith(t => (object)Undefined.Value));
        }
    }
}
