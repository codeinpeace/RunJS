using System;
using System.Linq;
using Jurassic;
using Jurassic.Library;
using NLog;
using RunJS.Core;

namespace RunJS.AddIn.Utils
{
    /// <summary>
    /// A logger-constructor used in javascript to construct loggers.
    /// </summary>
    [JsAddIn("logger")]
    public class LoggerConstructor : ClrFunction
    {
        private ScriptRunner runner;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerConstructor"/> class.
        /// </summary>
        /// <param name="runner">The script runner.</param>
        public LoggerConstructor(ScriptRunner runner)
            : base(runner.Engine.Function.Prototype, "Logger", runner.Engine.Object.InstancePrototype)
        {
            LogManager.Configuration = runner.LogConfiguration;
            LogManager.ReconfigExistingLoggers();
            this.runner = runner;
        }

        //     JAVASCRIPT INTERNAL FUNCTIONS
        //_________________________________________________________________________________________

        /// <summary>
        /// Constructs the specified logger.
        /// </summary>
        /// <param name="logName">Name of the log.</param>
        /// <returns>A new LoggerInstance.</returns>
        [JSConstructorFunction]
        public LoggerInstance Construct(string logName)
        {
            if (logName == "null" || logName == "default")
                logName = null;
            if (String.IsNullOrWhiteSpace(logName))
                throw new JavaScriptException(Engine, "Error", "logName can't be empty");
            return new LoggerInstance(this.InstancePrototype, this.runner, logName);
        }
    }

    /// <summary>
    /// A logger-instance used by javascript.
    /// </summary>
    public class LoggerInstance : ObjectInstance
    {
        private static Logger classLogger = LogManager.GetCurrentClassLogger();

        private ScriptRunner scriptRunner;
        private string logName;
        private Logger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerInstance"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <param name="scriptRunner">The script runner.</param>
        /// <param name="logName">Name of the log.</param>
        public LoggerInstance(ObjectInstance prototype, ScriptRunner scriptRunner, string logName)
            : base(prototype)
        {
            this.scriptRunner = scriptRunner;
            logName = logName[0].ToString().ToUpper() + logName.Substring(1);
            this.logName = logName;
            this.logger = LogManager.GetLogger("JSLogger." + logName);

            classLogger.Trace("Logger {0} initialized.", logName);
            PopulateFunctions();
        }

        /// <summary>
        /// Traces the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        [JSFunction(Name = "trace")]
        public void Trace(string message, params object[] args)
        {
            logger.Trace(message, args.JsonEntype(Engine));
        }

        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        [JSFunction(Name = "debug")]
        public void Debug(string message, params object[] args)
        {
            logger.Debug(message, args.JsonEntype(Engine));
        }

        /// <summary>
        /// Infoes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        [JSFunction(Name = "info")]
        public void Info(string message, params object[] args)
        {
            logger.Info(message, args.JsonEntype(Engine));
        }

        /// <summary>
        /// Warns the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        [JSFunction(Name = "warn")]
        public void Warn(string message, params object[] args)
        {
            logger.Warn(message, args.JsonEntype(Engine));
        }

        /// <summary>
        /// Errors the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        [JSFunction(Name = "error")]
        public void Error(string message, params object[] args)
        {
            logger.Error(message, args.JsonEntype(Engine));
        }

        /// <summary>
        /// Fatals the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The args.</param>
        [JSFunction(Name = "fatal")]
        public void Fatal(string message, params object[] args)
        {
            logger.Fatal(message, args.JsonEntype(Engine));
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
                return "Logger";
            }
        }
    }

    static class LoggerExtensions
    {
        public static object[] JsonEntype(this object[] array, ScriptEngine engine)
        {
            return array.Select(itm =>
            {
                if (itm is ObjectInstance)
                {
                    try
                    {
                        return JSONObject.Stringify(engine, itm).Trim();
                    }
                    catch
                    { }
                }
                return itm.ToString();
            }).ToArray();
        }
    }
}
