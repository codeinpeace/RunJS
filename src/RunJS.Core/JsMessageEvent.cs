using Jurassic;
using Jurassic.Library;

namespace RunJS.Core
{
    class JsMessageEvent : ObjectInstance
    {
        private JsPort jsPort;
        private string message;

        public JsMessageEvent(ScriptEngine engine, JsPort jsPort, string message)
            : base(engine.Object.Prototype)
        {
            // TODO: Complete member initialization
            this.jsPort = jsPort;
            this.message = message;

            PopulateFunctions();
        }

        [JSProperty(Name = "data")]
        public string Data
        {
            get { return message; }
        }

        [JSProperty(Name = "port")]
        public JsPort[] Ports
        {
            get { return new JsPort[] { jsPort }; }
        }
    }
}
