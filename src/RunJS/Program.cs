using System;
using RunJS.Core;

namespace RunJS
{
    class Program
    {
        static void Main(string[] args)
        {
            ScriptRunner scriptRunner = new ScriptRunner();
            scriptRunner.Run();

            Console.WriteLine("ready");
            Console.ReadLine();
            scriptRunner.Execute("var Storage = require('storage');");
            scriptRunner.Execute("var s = new Storage();");

            scriptRunner.Execute("var sp = s.getStorage('test');");

            scriptRunner.Execute("sp.put(null, {name:'palt',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt2',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt3',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt4',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt5',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt6',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt7',age:10})");
            scriptRunner.Execute("sp.put(null, {name:'palt8',age:10})");

            scriptRunner.Execute("sp.put(null, {name:'palt',age:11})");
            scriptRunner.Execute("sp.put(null, {name:'palt',age:12})");
            scriptRunner.Execute("sp.put(null, {name:'palt',age:13})");
            scriptRunner.Execute("sp.put(null, {name:'palt',age:14})");

            scriptRunner.Execute("sp.query({age:10}).length");
            scriptRunner.Execute("sp.query({name:'palt'}).length");
            scriptRunner.Execute("sp.query({name:'palt',age:10}).length");
            scriptRunner.Execute("sp.query({name:'palt'},{age:10}).length");

            Console.ReadLine();
        }
    }
}
