using System;
using System.Collections.Generic;
using System.Linq;
using Jurassic;
using RunJS.Core;

namespace RunJS
{
    class Program
    {
        private static List<ScriptSource> scriptSources = new List<ScriptSource>();

        static void Main(string[] args)
        {
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
