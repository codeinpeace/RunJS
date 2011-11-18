using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jurassic;
using Jurassic.Library;
using NLog;
using RunJS.Core;

namespace RunJS
{
    class Program
    {
        private static List<ScriptSource> scriptSources = new List<ScriptSource>();
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "NLog.config")))
            {
                LogManager.ThrowExceptions = true;
                LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(Path.Combine(Environment.CurrentDirectory, "NLog.config"));
                LogManager.ReconfigExistingLoggers();
                logger.Info("Loaded log-config from current directory");
            }

            ScriptRunner scriptRunner = new ScriptRunner();
            scriptRunner.Run();

            scriptRunner.BeginInvoke(r =>
            {
                foreach (var s in scriptSources)
                    scriptRunner.Engine.Execute(s);

                foreach (var arg in args.Where(a => a[0] != '-'))
                    scriptRunner.Engine.ExecuteFile(arg);
            });

            if (args.Contains("--no-prompt"))
            {
                if (args.Any(arg => arg.StartsWith("--exec:")))
                {
                    foreach (var arg in args.Where(a => a.StartsWith("--exec:")).Select(a => a.Substring("--exec:".Length)))
                        scriptRunner.BeginInvoke(r => r.Execute(scriptRunner.Engine.Global[arg] as FunctionInstance));
                }
                scriptRunner.WaitForClose();
            }
            else
            {
                while (scriptRunner.Running)
                {
                    Console.Write(":> ");
                    string cmd = Console.ReadLine();

                    string result;
                    try
                    {
                        result = scriptRunner.Execute(cmd).ToString();
                    }
                    catch (JavaScriptException e)
                    {
                        result = e.ToString();
                    }

                    Console.WriteLine(result);
                    Console.WriteLine();
                }
            }
        }

        public static void AddScriptSource(string name, string script)
        {
            scriptSources.Add(new StringScriptSource(script, name));
        }
    }
}
