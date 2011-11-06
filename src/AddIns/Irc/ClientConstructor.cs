using Jurassic.Library;
using RunJS.Core;

namespace RunJS.AddIn.Irc
{
    [JsAddIn]
    public class ClientConstructor : ClrFunction
    {
        private ScriptRunner runner;

        public ClientConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "IrcClient", runner.Engine.Object.InstancePrototype)
        {

        }
    }
}
