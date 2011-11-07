using System.Collections.Generic;
using Jurassic.Library;

namespace RunJS.Core
{
    /// <summary>
    /// JsConstructor for EventObjects.
    /// </summary>
    public class JsEventObjectConstructor : ClrFunction
    {
        private readonly ScriptRunner runner;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsEventObjectConstructor"/> class.
        /// </summary>
        /// <param name="runner">The runner.</param>
        public JsEventObjectConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.InstancePrototype, "EventObject", new JsEventObject(runner.Engine.Object.InstancePrototype, runner))
        {
            this.runner = runner;
        }

        /// <summary>
        /// Constructs a new JsEventObject instance.
        /// </summary>
        /// <returns>The newly constructed JsEventObject</returns>
        [JSConstructorFunction]
        public JsEventObject Construct()
        {
            return new JsEventObject(this.InstancePrototype, runner);
        }
    }

    /// <summary>
    /// Class used to handle events in JS.
    /// </summary>
    public class JsEventObject : ObjectInstance
    {
        private readonly ScriptRunner runner;
        private readonly Dictionary<string, List<FunctionInstance>> events;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsEventObject"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="runner">The runner.</param>
        public JsEventObject(ObjectInstance prototype, ScriptRunner runner)
            : base(prototype)
        {
            this.runner = runner;
            this.events = new Dictionary<string, List<FunctionInstance>>();

            PopulateFunctions(typeof(JsEventObject));
        }

        /// <summary>
        /// Listens the specified event name.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="fn">The fn.</param>
        [JSFunction(Name = "listen")]
        public void Listen(string eventName, FunctionInstance fn)
        {
            if (!events.ContainsKey(eventName))
                events.Add(eventName, new List<FunctionInstance>());
            events[eventName].Add(fn);
        }

        /// <summary>
        /// Listens the specified events to listen.
        /// </summary>
        /// <param name="eventsToListen">The events to listen.</param>
        [JSFunction(Name = "listen")]
        public void Listen(ObjectInstance eventsToListen)
        {
            foreach (var prop in eventsToListen.Properties)
                if (prop.IsEnumerable && prop.Value is FunctionInstance)
                    Listen(prop.Name, (FunctionInstance)prop.Value);
        }

        /// <summary>
        /// Ignores the specified event name.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="fn">The fn.</param>
        [JSFunction(Name = "ignore")]
        public void Ignore(string eventName, FunctionInstance fn)
        {
            if (events.ContainsKey(eventName))
                events[eventName].Remove(fn);
        }

        /// <summary>
        /// Ignores the specified event name.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        [JSFunction(Name = "ignore")]
        public void Ignore(string eventName)
        {
            if (events.ContainsKey(eventName))
                events[eventName].Clear();
        }

        /// <summary>
        /// Fires the specified event name.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="args">The args.</param>
        protected void Fire(string eventName, params object[] args)
        {
            if (events.ContainsKey(eventName))
                foreach (var fn in events[eventName])
                    runner.Execute(fn, args);
        }
    }
}
