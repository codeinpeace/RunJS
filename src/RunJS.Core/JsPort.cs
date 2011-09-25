using System.Collections.Generic;
using Jurassic.Library;

namespace RunJS.Core
{
    class JsPort : ObjectInstance
    {
        private const string CONNECT = "$$connect$$";
        private const string DISCONNECT = "$$disconnect$$";

        protected JsPort receiver;
        bool running;
        private FunctionInstance messageCallback;
        private HashSet<FunctionInstance> messageListeners;
        private ScriptRunner runner;
        public JsPort(ScriptRunner runner, JsPort receiver = null)
            : base(runner.Engine.Object.Prototype)
        {
            this.runner = runner;
            this.receiver = receiver;
            this.messageListeners = new HashSet<FunctionInstance>();
            PopulateFunctions();
        }

        [JSProperty(Name = "onmessage")]
        public FunctionInstance OnMessageCallback
        {
            get
            {
                return messageCallback;
            }
            set
            {
                messageCallback = value;
                if (value != null)
                    Start();
                else
                    Stop();
            }
        }

        [JSFunction(Name = "start")]
        public void Start()
        {
            running = true;
            receiver.PostReceiveMessage(CONNECT);
        }

        [JSFunction(Name = "stop")]
        public void Stop()
        {
            running = false;
            receiver.PostReceiveMessage(DISCONNECT);
        }

        [JSFunction(Name = "postMessage")]
        public void PostMessage(string message)
        {
            if (!running)
                return;

            receiver.PostReceiveMessage(message);
        }

        [JSFunction(Name = "addEventListener")]
        public void AddEventListener(string name, FunctionInstance callback, bool boubles)
        {
            if (name == "message")
                messageListeners.Add(callback);
        }

        [JSFunction(Name = "removeEventListener")]
        public void RemoveEventListener(string name, FunctionInstance callback, bool boubles)
        {
            if (name == "message" && messageListeners.Contains(callback))
                messageListeners.Remove(callback);
        }

        private void PostReceiveMessage(string message)
        {
            runner.BeginInvoke((r) =>
            {
                ReceiveMessage(message);
            });
        }

        public void ReceiveMessage(string message)
        {
            if (message == CONNECT)
            {
                running = true;
                return;
            }
            else if (message == DISCONNECT)
            {
                running = false;
                return;
            }

            if (!running)
                return;

            var evt = new JsMessageEvent(Engine, this, message);
            if (messageCallback != null)
                messageCallback.CallLateBound(Engine.Global, evt);
            foreach (var mc in messageListeners)
                mc.CallLateBound(Engine.Global, evt);
        }
    }
}
