using System;
using dotRant;
using Jurassic;
using Jurassic.Library;
using NLog;
using RunJS.Core;

namespace RunJS.AddIn.Irc
{
    /// <summary>
    /// Represents a IRC client in JS
    /// </summary>
    public class ClientInstance : JsEventObject
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IrcClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientInstance"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="runner">The runner.</param>
        public ClientInstance(ObjectInstance prototype, ScriptRunner runner)
            : base(prototype, runner)
        {
            client = new IrcClient();
            client.Message += new EventHandler<IrcMessageEventArgs>(client_Message);
            client.ChannelJoin += new EventHandler<IrcChannelEventArgs>(client_ChannelJoin);
            client.Disconnect += new EventHandler<EventArgs>(client_Disconnect);
            client.SslValidate += new EventHandler<SslValidateEventArgs>(client_SslValidate);
            PopulateFunctions();
        }

        void client_SslValidate(object sender, SslValidateEventArgs e)
        {
            e.Accept = true; // Won't dispatch to javascript.
            logger.Info("Ssl Validate called by client");
        }

        void client_Disconnect(object sender, EventArgs e)
        {
            Fire("disconnect");
        }

        void client_ChannelJoin(object sender, IrcChannelEventArgs e)
        {
            Fire("channel.join", new ChannelInstance(ScriptRunner, e.Channel));
        }

        void client_Message(object sender, IrcMessageEventArgs e)
        {
            Fire("message", e.Type.ToString(), e.Sender.Name, e.Recipent.Name, e.Message);
        }

        /// <summary>
        /// Gets or sets the nick.
        /// </summary>
        /// <value>
        /// The nick.
        /// </value>
        [JSProperty(Name = "nick", IsEnumerable = false, IsConfigurable = false)]
        public string Nick
        {
            get { return client.Nick; }
            set { client.Nick = value; }
        }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        [JSProperty(Name = "fullName", IsEnumerable = false, IsConfigurable = false)]
        public string FullName
        {
            get { return client.FullName; }
            set { client.FullName = value; }
        }

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        [JSProperty(Name = "encoding", IsEnumerable = false, IsConfigurable = false)]
        public string Encoding
        {
            get { return client.Encoding.EncodingName; }
            set
            {
                try
                {
                    client.Encoding = System.Text.Encoding.GetEncoding(value);
                }
                catch (Exception e)
                {
                    throw new JavaScriptException(Engine, "Error", e.Message);
                }
            }
        }

        /// <summary>
        /// Connects the specified hostname.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="secure">if set to <c>true</c> [secure].</param>
        /// <returns>A promise representing the connect opperation.</returns>
        [JSFunction(Name = "connect")]
        public JsPromise Connect(string hostname, int port, bool secure)
        {
            try
            {
                return client.ConnectAsync(hostname, port, secure).AsPromise(ScriptRunner);
            }
            catch (Exception e)
            {
                throw new JavaScriptException(Engine, "Error", e.Message);
            }
        }

        /// <summary>
        /// Joins a channel.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        /// <returns>A promise representing the join opperation.</returns>
        [JSFunction(Name = "joinChannel")]
        public JsPromise JoinChannel(string channelName)
        {
            try
            {
                return client.Channels.JoinAsync(channelName).AsPromise(ScriptRunner, channel => new ChannelInstance(ScriptRunner, channel));
            }
            catch (Exception e)
            {
                throw new JavaScriptException(Engine, "Error", e.Message);
            }
        }

        private class JsIrcReceiver : IIrcRecipent
        {
            string name;

            public JsIrcReceiver(string name)
            {
                this.name = name;
            }

            public string Name
            {
                get { return name; }
            }
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="receiver">The receiver.</param>
        /// <param name="message">The message.</param>
        [JSFunction(Name = "send")]
        public void Send(string receiver, string message)
        {
            var sendTo = new JsIrcReceiver(receiver);
            try
            {
                client.SendMessage(sendTo, message);
            }
            catch (Exception e)
            {
                throw new JavaScriptException(Engine, "Error", e.Message);
            }
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
                return "IrcClient";
            }
        }
    }
}
