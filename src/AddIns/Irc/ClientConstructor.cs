using Jurassic.Library;
using RunJS.Core;

namespace RunJS.AddIn.Irc
{
    /// <summary>
    /// A JsAddIn for creating IRC clients.
    /// </summary>
    [JsAddIn]
    public class ClientConstructor : ClrFunction
    {
        private ScriptRunner runner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConstructor"/> class.
        /// </summary>
        /// <param name="runner">The runner.</param>
        public ClientConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "IrcClient", runner.Construct<JsEventObject>())
        {
            this.runner = runner;

            PopulateFunctions();
        }

        //     JAVASCRIPT INTERNAL FUNCTIONS
        //_________________________________________________________________________________________

        /// <summary>
        /// Constructs a IrcClient instance.
        /// </summary>
        /// <returns>A new IrcClient.</returns>
        [JSConstructorFunction]
        public ClientInstance Construct()
        {
            return new ClientInstance(InstancePrototype, runner);
        }
    }
}
