using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    class AddInManager : ObjectInstance, IDisposable
    {
        internal const string DEFAULT = "_default";
        private static readonly Dictionary<ScriptEngine, AddInManager> addInManagers = new Dictionary<ScriptEngine, AddInManager>();

        private ScriptRunner runner;
        private Dictionary<Tuple<string, string>, ClrFunction> addins;
        public AddInManager(ScriptRunner runner)
            : base(runner.Engine)
        {
            this.runner = runner;
            this.addins = new Dictionary<Tuple<string, string>, ClrFunction>();
            lock (addInManagers)
                addInManagers.Add(Engine, this);

            PopulateFunctions();
        }


        [JSFunction(Name = "require", Flags = JSFunctionFlags.HasEngineParameter)]
        public static object Require(ScriptEngine engine, string assembly, string name)
        {
            var self = addInManagers[engine];

            var tuple = new Tuple<string, string>(assembly, name);
            if (self.addins.ContainsKey(tuple))
                return self.addins[tuple];

            var asmName = "RunJS.AddIn." + assembly + ".dll";
            string[] searchPaths = new string[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RunJS", "AddIns", asmName),
                Path.Combine(Environment.CurrentDirectory, asmName),
                Path.Combine(Environment.CurrentDirectory, "AddIns", asmName),
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AddIns", asmName)
            };
            foreach (var sp in searchPaths)
            {
                if (File.Exists(sp))
                {
                    Assembly asm = Assembly.LoadFrom(sp);
                    foreach (var tt in asm.GetTypes().Where(t => t.IsSubclassOf(typeof(ClrFunction))).Select(t => new
                    {
                        attr = t.GetCustomAttributes(typeof(JsAddInAttribute), false).Cast<JsAddInAttribute>().SingleOrDefault(),
                        ctor = t.GetConstructor(Type.GetTypeArray(new object[] { self.runner }))
                    }).Where(ta => ta.attr != null && ta.ctor != null))
                    {
                        string n = tt.attr.Name;
                        ClrFunction f = (ClrFunction)tt.ctor.Invoke(new object[] { self.runner });
                        self.addins.Add(new Tuple<string, string>(assembly, n), f);
                    }
                    break;
                }
            }

            if (self.addins.ContainsKey(tuple))
                return self.addins[tuple];
            else
                return Undefined.Value;
        }

        [JSFunction(Name = "require", Flags = JSFunctionFlags.HasEngineParameter)]
        public static object Require(ScriptEngine engine, string assembly)
        {
            return Require(engine, assembly, DEFAULT);
        }

        public void Dispose()
        {
            lock (addInManagers)
                addInManagers.Remove(Engine);
        }
    }
}
