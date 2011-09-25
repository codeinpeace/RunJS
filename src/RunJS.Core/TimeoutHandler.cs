using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jurassic.Library;
using System.Threading;
using Jurassic;

namespace RunJS.Core
{
    class TimeoutHandler : ObjectInstance, IDisposable
    {
        private static Dictionary<ScriptEngine, TimeoutHandler> timeoutHandlers = new Dictionary<ScriptEngine, TimeoutHandler>();

        private int nextId = 0;
        private volatile HashSet<int> timeouts;
        private volatile HashSet<int> intervals;
        private ScriptRunner scriptRunner;

        public TimeoutHandler(ScriptRunner scriptRunner)
            : base(scriptRunner.Engine)
        {
            timeouts = new HashSet<int>();
            intervals = new HashSet<int>();
            this.scriptRunner = scriptRunner;
            timeoutHandlers.Add(Engine, this);
        }

        [JSFunction(Name = "setTimeout", Flags = JSFunctionFlags.HasEngineParameter)]
        public static int SetTimeout(ScriptEngine engine, FunctionInstance function, double delay)
        {
            var self = timeoutHandlers[engine];

            int id;
            lock (self)
            {
                do
                {
                    id = self.nextId++;
                } while (self.timeouts.Contains(id) || self.intervals.Contains(id));

                self.timeouts.Add(id);
            }

            new Thread((ThreadStart)delegate
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(delay));

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
            })
            {
                Name = "JsTimeout #" + id
            }.Start();
            return id;
        }

        [JSFunction(Name = "setTimeout", Flags = JSFunctionFlags.HasEngineParameter)]
        public static int SetTimeout(ScriptEngine engine, string function, double delay, params object[] args)
        {
            var self = timeoutHandlers[engine];

            int id;
            lock (self)
            {
                do
                {
                    id = self.nextId++;
                } while (self.timeouts.Contains(id) || self.intervals.Contains(id));

                self.timeouts.Add(id);
            }

            new Thread((ThreadStart)delegate
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(delay));

                lock (self)
                {
                    if (!self.timeouts.Contains(id))
                        return;
                    self.timeouts.Remove(id);
                }


                self.scriptRunner.BeginInvoke(delegate
                {
                    var fn = self.Engine.Evaluate(function) as FunctionInstance;
                    if (fn != null)
                        fn.Call(self.Engine.Global, args);
                });
            })
            {
                Name = "JsTimeout #" + id
            }.Start();
            return id;
        }

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

            new Thread((ThreadStart)delegate
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(delay));

                    lock (self)
                    {
                        if (!self.intervals.Contains(id))
                            return;
                    }

                    self.scriptRunner.BeginInvoke(delegate
                    {
                        function.Call(self.Engine.Global);
                    });
                }
            })
            {
                Name = "JsInterval #" + id
            }.Start();
            return id;
        }

        [JSFunction(Name = "setInterval", Flags = JSFunctionFlags.HasEngineParameter)]
        public static int SetInterval(ScriptEngine engine, string function, double delay, params object[] args)
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

            new Thread((ThreadStart)delegate
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(delay));

                    lock (self)
                    {
                        if (!self.intervals.Contains(id))
                            return;
                    }

                    self.scriptRunner.BeginInvoke(delegate
                    {
                        var fn = self.Engine.Evaluate(function) as FunctionInstance;
                        if (fn != null)
                            fn.Call(self.Engine.Global, args);
                    });
                }
            })
            {
                Name = "JsInterval #" + id
            }.Start();
            return id;
        }

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
            timeoutHandlers.Remove(Engine);
        }
    }
}
