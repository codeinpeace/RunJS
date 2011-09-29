using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using RunJS.Core;
using Should.Fluent;

namespace RunJS.AddIn.Utils.Tests
{
    [TestFixture]
    public class LoggerTests
    {
        private List<LogEventInfo> logs;
        private LoggingConfiguration oldConfig;
        private ScriptRunner scriptRunner;
        private class TestLoggerTarget : Target
        {
            private List<LogEventInfo> logs = new List<LogEventInfo>();
            protected override void Write(LogEventInfo logEvent)
            {
                logs.Add(logEvent);
            }

            public List<LogEventInfo> Logs
            {
                get { return logs; }
            }
        }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var target = new TestLoggerTarget();
            logs = target.Logs;

            var config = new LoggingConfiguration();
            config.AddTarget("fake", target);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, target));
            oldConfig = LogManager.Configuration;
            LogManager.Configuration = config;
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            LogManager.Configuration = oldConfig;
        }

        [SetUp]
        public void SetUp()
        {
            scriptRunner = new ScriptRunner();
            scriptRunner.Run();
            scriptRunner.Execute("this.Logger = require('utils', 'logger');");
            logs.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            scriptRunner.Dispose();
        }

        [Test]
        public void CreatingALoggerCreatesALog()
        {
            scriptRunner.Execute("var logger = new Logger('test');");
            logs.Count.Should().Equal(1);
        }

        [Test]
        public void LogLevelsTest()
        {
            scriptRunner.Execute("var logger = new Logger('test');");
            scriptRunner.Execute("logger.trace('test');");
            scriptRunner.Execute("logger.debug('test');");
            scriptRunner.Execute("logger.info('test');");
            scriptRunner.Execute("logger.warn('test');");
            scriptRunner.Execute("logger.error('test');");
            scriptRunner.Execute("logger.fatal('test');");

            logs.Count.Should().Equal(7);
            logs[1].Level.Should().Equal(LogLevel.Trace);
            logs[2].Level.Should().Equal(LogLevel.Debug);
            logs[3].Level.Should().Equal(LogLevel.Info);
            logs[4].Level.Should().Equal(LogLevel.Warn);
            logs[5].Level.Should().Equal(LogLevel.Error);
            logs[6].Level.Should().Equal(LogLevel.Fatal);
        }

        [Test]
        public void LoggerNameTests()
        {
            scriptRunner.Execute("var logger1 = new Logger('test');");
            scriptRunner.Execute("var logger2 = new Logger('Test');");
            scriptRunner.Execute("var logger3 = new Logger('knut');");
            scriptRunner.Execute("var logger4 = new Logger('Paal');");
            scriptRunner.Execute("var logger5 = new Logger('olE');");
            logs.Clear();

            scriptRunner.Execute("logger1.trace('test');");
            scriptRunner.Execute("logger2.trace('test');");
            scriptRunner.Execute("logger3.trace('test');");
            scriptRunner.Execute("logger4.trace('test');");
            scriptRunner.Execute("logger5.trace('test');");

            logs.Count.Should().Equal(5);
            logs[0].LoggerName.Should().Equal("JSLogger.Test");
            logs[1].LoggerName.Should().Equal("JSLogger.Test");
            logs[2].LoggerName.Should().Equal("JSLogger.Knut");
            logs[3].LoggerName.Should().Equal("JSLogger.Paal");
            logs[4].LoggerName.Should().Equal("JSLogger.OlE");
        }

        [Test]
        public void LogMessageTests()
        {
            scriptRunner.Execute("var logger = new Logger('test');");
            scriptRunner.Execute("logger.trace('test1');");
            scriptRunner.Execute("logger.trace('test2');");
            scriptRunner.Execute("logger.trace('test3');");
            scriptRunner.Execute("logger.trace('test4');");
            scriptRunner.Execute("logger.trace('test5');");

            logs.Count.Should().Equal(6);
            for (int i = 1; i < 6; i++)
                logs[i].Message.Should().Equal("test" + i);
        }

        [Test]
        public void LogParameters()
        {
            scriptRunner.Execute("var logger = new Logger('test');");
            scriptRunner.Execute("logger.trace('test');");
            scriptRunner.Execute("logger.trace('test', 1);");
            scriptRunner.Execute("logger.trace('test', 1, 2, 3);");
            scriptRunner.Execute("logger.trace('test', {age:5});");

            logs.Count.Should().Equal(5);

            logs[1].Parameters.Length.Should().Equal(0);

            logs[2].Parameters.Length.Should().Equal(1);
            logs[2].Parameters[0].Should().Equal("1");

            logs[3].Parameters.Length.Should().Equal(3);
            logs[3].Parameters[0].Should().Equal("1");
            logs[3].Parameters[1].Should().Equal("2");
            logs[3].Parameters[2].Should().Equal("3");

            logs[4].Parameters.Length.Should().Equal(1);
            logs[4].Parameters[0].Should().Equal("{\"age\":5}");
        }
    }
}
