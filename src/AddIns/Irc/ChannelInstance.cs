using dotRant;
using Jurassic.Library;
using RunJS.Core;

namespace RunJS.AddIn.Irc
{
    /// <summary>
    /// Represents a IRC channel for JS.
    /// </summary>
    public class ChannelInstance : ObjectInstance
    {
        ScriptRunner runner;
        IrcChannel channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelInstance"/> class.
        /// </summary>
        /// <param name="runner">The runner.</param>
        /// <param name="channel">The channel.</param>
        public ChannelInstance(ScriptRunner runner, IrcChannel channel)
            : base(runner.Engine)
        {
            this.runner = runner;
            this.channel = channel;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        [JSProperty(Name = "name", IsConfigurable = false, IsEnumerable = false)]
        public string Name
        {
            get { return channel.Name; }
        }

        /// <summary>
        /// Gets or sets the topic.
        /// </summary>
        /// <value>
        /// The topic.
        /// </value>
        [JSProperty(Name = "topic", IsConfigurable = false, IsEnumerable = false)]
        public string Topic
        {
            get { return channel.Topic; }
            set { channel.Topic = value; }
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
                return "IrcChannel";
            }
        }
    }
}
