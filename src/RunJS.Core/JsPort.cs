using System.Collections.Generic;
using Jurassic.Library;

namespace RunJS.Core
{
    class JsPort : ObjectInstance
    {
        private const string CONNECT = "$$connect$$";
        private const string DISCONNECT = "$$disconnect$$";

        private JsPort receiver;
        private bool running;
        private FunctionInstance messageCallback;
        private HashSet<FunctionInstance> messageListeners;
        protected ScriptRunner runner;
        public JsPort(ScriptRunner runner)
            : base(runner.Engine.Object.Prototype)
        {
            this.runner = runner;
            Initialize();
            PopulateFunctions();
        }

        protected void Initialize()
        {
            this.messageListeners = new HashSet<FunctionInstance>();
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

        internal JsPort Receiver
        {
            get { return receiver; }
            set { receiver = value; }
        }

        [JSFunction(Name = "start")]
        public void Start()
        {
            if (running)
                return;
            running = true;
            receiver.PostReceiveMessage(CONNECT);
        }

        [JSFunction(Name = "stop")]
        public void Stop()
        {
            if (!running)
                return;
            running = false;
            receiver.PostReceiveMessage(DISCONNECT);
        }

        [JSFunction(Name = "postMessage")]
        public void PostMessage(string message)
        {
            //Console.WriteLine("PostMessage-call with message \"" + message + "\", running: " + running);

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
            //Console.WriteLine("Received " + message);
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
            //Console.WriteLine("Created message event with message \"" + message + "\" to be transmitted to " + (messageCallback == null ? "" : "callback (" + messageCallback.ToStringJS() + ") and ") + messageListeners.Count + " listeners");
            if (messageCallback != null)
                messageCallback.CallLateBound(Engine.Global, evt);
            foreach (var mc in messageListeners)
                mc.CallLateBound(Engine.Global, evt);
        }

        protected override string InternalClassName
        {
            get
            {
                return "JsPort";
            }
        }
    }
}
