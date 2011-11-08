using System;
using Jurassic;
using RunJS.Core;

namespace RunJS
{
    class Program
    {
        static void Main(string[] args)
        {
            ScriptRunner scriptRunner = new ScriptRunner();
            scriptRunner.Run();

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
}
