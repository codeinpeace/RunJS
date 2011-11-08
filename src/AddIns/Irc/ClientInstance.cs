using Jurassic.Library;
using RunJS.Core;

namespace RunJS.AddIn.Irc
{
    public class ClientInstance : ObjectInstance
    {
        private ScriptRunner runner;

        public ClientInstance(ObjectInstance prototype, ScriptRunner runner)
            : base(prototype)
        {
            this.runner = runner;
            PopulateFunctions();
        }
    }
}
