using System.Net;
using System.Threading;
using Jurassic.Library;
using RunJS.Core;

namespace RunJS.AddIn.Utils
{
    /// <summary>
    /// A loader-constructor, used to create Loader-instances in javascript.
    /// </summary>
    [JsAddIn("loader")]
    public class LoaderConstructor : ClrFunction
    {
        private ScriptRunner runner;
        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderConstructor"/> class.
        /// </summary>
        /// <param name="runner">The runner.</param>
        public LoaderConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "Loader", runner.Engine.Object.InstancePrototype)
        {
            this.runner = runner;
        }

        //     JAVASCRIPT INTERNAL FUNCTIONS
        //_________________________________________________________________________________________

        /// <summary>
        /// Constructs a javascript Loader instance.
        /// </summary>
        /// <returns>A new javascript Loader instance.</returns>
        [JSConstructorFunction]
        public LoaderInstance Construct()
        {
            return new LoaderInstance(this.InstancePrototype, this.runner);
        }
    }

    /// <summary>
    /// A LoaderInstance used from javascript to download stuff from the internet.
    /// </summary>
    public class LoaderInstance : ObjectInstance
    {
        private ScriptRunner scriptRunner;
        private WebClient webClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoaderInstance"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="scriptRunner">The script runner.</param>
        public LoaderInstance(ObjectInstance prototype, ScriptRunner scriptRunner)
            : base(prototype)
        {
            this.scriptRunner = scriptRunner;
            this.webClient = new WebClient();
        }

        /// <summary>
        /// Gets the name of the internal class.
        /// </summary>
        /// <value>
        /// The name of the internal class.
        /// </value>
        protected override string InternalClassName
        {
            get
            {
                return "Loader";
            }
        }

        /// <summary>
        /// Loads the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="callback">The callback.</param>
        [JSFunction(Name = "load")]
        public void Load(string url, FunctionInstance callback)
        {
            new Thread((ThreadStart)delegate
            {
                var data = webClient.DownloadString(url);
                scriptRunner.Execute(callback);
            })
            {
                Name = "JsLoader"
            }.Start();
        }
    }
}
