using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    class TimeoutHandler : ObjectInstance, IDisposable
    {
        private static Dictionary<ScriptEngine, TimeoutHandler> timeoutHandlers = new Dictionary<ScriptEngine, TimeoutHandler>();

        private int nextId = 0;
        private volatile HashSet<int> timeouts;
        private volatile HashSet<int> intervals;
        private ScriptRunner scriptRunner;
        private CancellationTokenSource cancellationTokenSource;

        public TimeoutHandler(ScriptRunner scriptRunner)
            : base(scriptRunner.Engine)
        {
            timeouts = new HashSet<int>();
            intervals = new HashSet<int>();
            this.scriptRunner = scriptRunner;
            timeoutHandlers.Add(Engine, this);

            PopulateFunctions();
            cancellationTokenSource = new CancellationTokenSource();
        }

        ~TimeoutHandler()
        {
            Dispose();
        }

        [JSFunction(Name = "setTimeout", Flags = JSFunctionFlags.HasEngineParameter)]
        public static int SetTimeout(ScriptEngine engine, FunctionInstance function, double delay)
        {
            var self = timeoutHandlers[engine];
            var finnishTime = DateTime.Now.AddMilliseconds(delay);

            int id;
            lock (self)
            {
                do
                {
                    id = self.nextId++;
                } while (self.timeouts.Contains(id) || self.intervals.Contains(id));

                self.timeouts.Add(id);
            }

            Task.Factory.StartNew(() =>
            {

                self.cancellationTokenSource.Token.ThrowIfCancellationRequested();

                TimeSpan timeout = finnishTime - DateTime.Now;
                if (timeout < TimeSpan.Zero)
                    timeout = TimeSpan.Zero;

                self.cancellationTokenSource.Token.WaitHandle.WaitOne(timeout);

                self.cancellationTokenSource.Token.ThrowIfCancellationRequested();

                lock (self)
                {
                    if (!self.timeouts.Contains(id))
                        return;
                    self.timeouts.Remove(id);
                }

                self.scriptRunner.BeginInvoke(delegate
                {
                    function.Call(self.Engine.Global);
                });

            }, self.cancellationTokenSource.Token);

            return id;
        }

        //[JSFunction(Name = "setTimeout", Flags = JSFunctionFlags.HasEngineParameter)]
        //public static int SetTimeout(ScriptEngine engine, string function, double delay, params object[] args)
        //{
        //    var self = timeoutHandlers[engine];

        //    int id;
        //    lock (self)
        //    {
        //        do
        //        {
        //            id = self.nextId++;
        //        } while (self.timeouts.Contains(id) || self.intervals.Contains(id));

        //        self.timeouts.Add(id);
        //    }

        //    new Thread((ThreadStart)delegate
        //    {
        //        Thread.Sleep(TimeSpan.FromMilliseconds(delay));

        //        lock (self)
        //        {
        //            if (!self.timeouts.Contains(id))
        //                return;
        //            self.timeouts.Remove(id);
        //        }


        //        self.scriptRunner.BeginInvoke(delegate
        //        {
        //            var fn = self.Engine.Evaluate(function) as FunctionInstance;
        //            if (fn != null)
        //                fn.Call(self.Engine.Global, args);
        //        });
        //    })
        //    {
        //        Name = "JsTimeout #" + id
        //    }.Start();
        //    return id;
        //}

        [JSFunction(Name = "clearTimeout", Flags = JSFunctionFlags.HasEngineParameter)]
        public static void ClearTimeout(ScriptEngine engine, int id)
        {
            var self = timeoutHandlers[engine];

            lock (self)
                if (self.timeouts.Contains(id))
                    self.timeouts.Remove(id);
        }

        [JSFunction(Name = "setInterval", Flags = JSFunctionFlags.HasEngineParameter)]
        public static int SetInterval(ScriptEngine engine, FunctionInstance function, double delay)
        {
            var self = timeoutHandlers[engine];

            int id;
            lock (self)
            {
                do
                {
                    id = self.nextId++;
                } while (self.timeouts.Contains(id) || self.intervals.Contains(id));

                self.intervals.Add(id);
            }

            self.SetInterval(id, function, delay, DateTime.Now);
            return id;
        }

        private void SetInterval(int id, FunctionInstance function, double delay, DateTime previous)
        {
            var self = this;
            var finnishTime = previous.AddMilliseconds(delay);

            Task.Factory.StartNew(() =>
            {

                self.cancellationTokenSource.Token.ThrowIfCancellationRequested();

                TimeSpan timeout = finnishTime - DateTime.Now;
                if (timeout < TimeSpan.Zero)
                    timeout = TimeSpan.Zero;

                self.cancellationTokenSource.Token.WaitHandle.WaitOne(timeout);

                self.cancellationTokenSource.Token.ThrowIfCancellationRequested();

                lock (self)
                {
                    if (!self.intervals.Contains(id))
                        return;
                }

                self.scriptRunner.BeginInvoke(delegate
                {
                    function.Call(self.Engine.Global);
                });

                SetInterval(id, function, delay, finnishTime);

            }, self.cancellationTokenSource.Token);
        }

        //[JSFunction(Name = "setInterval", Flags = JSFunctionFlags.HasEngineParameter)]
        //public static int SetInterval(ScriptEngine engine, string function, double delay, params object[] args)
        //{
        //    var self = timeoutHandlers[engine];

        //    int id;
        //    lock (self)
        //    {
        //        do
        //        {
        //            id = self.nextId++;
        //        } while (self.timeouts.Contains(id) || self.intervals.Contains(id));

        //        self.intervals.Add(id);
        //    }

        //    new Thread((ThreadStart)delegate
        //    {
        //        while (true)
        //        {
        //            Thread.Sleep(TimeSpan.FromMilliseconds(delay));

        //            lock (self)
        //            {
        //                if (!self.intervals.Contains(id))
        //                    return;
        //            }

        //            self.scriptRunner.BeginInvoke(delegate
        //            {
        //                var fn = self.Engine.Evaluate(function) as FunctionInstance;
        //                if (fn != null)
        //                    fn.Call(self.Engine.Global, args);
        //            });
        //        }
        //    })
        //    {
        //        Name = "JsInterval #" + id
        //    }.Start();
        //    return id;
        //}

        [JSFunction(Name = "clearInterval", Flags = JSFunctionFlags.HasEngineParameter)]
        public static void ClearInterval(ScriptEngine engine, int id)
        {
            var self = timeoutHandlers[engine];

            lock (self)
                if (self.intervals.Contains(id))
                    self.intervals.Remove(id);
        }

        public void Dispose()
        {
            timeouts.Clear();
            intervals.Clear();
            try { cancellationTokenSource.Cancel(true); }
            catch { }
            timeoutHandlers.Remove(Engine);
        }
    }
}
