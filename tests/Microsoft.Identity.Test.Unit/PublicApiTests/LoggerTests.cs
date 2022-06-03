// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class LoggerTests
    {
        private LogCallback _callback;

        [TestInitialize]
        public void TestInit()
        {
            TestCommon.ResetInternalStaticCaches();

            _callback = Substitute.For<LogCallback>();
        }

        private ILoggerAdapter CreateLogger(LogLevel logLevel = LogLevel.Verbose, bool enablePiiLogging = false, bool legacyLogger = false)
        {
            if (legacyLogger)
            {
                return new LegacyIdentityLoggerAdapter(Guid.Empty, null, null, logLevel, enablePiiLogging, true, _callback);
            }

            return new IdentityLoggerAdapter(new TestIdentityLogger(), Guid.Empty, null, null, enablePiiLogging);
        }

        [TestMethod()]
        public void ConstructorComponentTest()
        {
            ILoggerAdapter logger = new IdentityLoggerAdapter(null, Guid.Empty, null, null, false);
            Assert.AreEqual(string.Empty, logger.ClientName);
            Assert.AreEqual(string.Empty, logger.ClientVersion);
            logger = new IdentityLoggerAdapter(null, Guid.Empty, "comp1", null, false);
            Assert.AreEqual(" (comp1)", logger.ClientName);
            logger = new IdentityLoggerAdapter(null, Guid.Empty, "comp1", "version1", false);
            Assert.AreEqual(" (comp1)", logger.ClientName);
            Assert.AreEqual("version1", logger.ClientVersion);
        }

        [TestMethod()]
        [DataRow(LogLevel.Always)]
        [DataRow(LogLevel.Error)]
        [DataRow(LogLevel.Warning)]
        [DataRow(LogLevel.Info)]
        [DataRow(LogLevel.Verbose)]
        public void CallbackLoggerTest(LogLevel level)
        {
            ILoggerAdapter logger = CreateLogger(level, false, true);
            var counter = 0;
            var validationCounter = 1;
            var levelToValidate = LogLevel.Always;
            Action incrementCounter = () => {

                if (level > levelToValidate)
                {
                    validationCounter++;
                }

                levelToValidate++;
            };

            _callback.When(x => x(LogLevel.Always, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Always(TestConstants.TestMessage);
            Assert.AreEqual(validationCounter, counter);
            _callback.Received().Invoke(Arg.Is((LogLevel)validationCounter - 2), Arg.Any<string>(), Arg.Is(false));

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error(TestConstants.TestMessage);
            Assert.AreEqual(validationCounter, counter);
            _callback.Received().Invoke(Arg.Is((LogLevel)validationCounter - 2), Arg.Any<string>(), Arg.Is(false));

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning(TestConstants.TestMessage);
            Assert.AreEqual(validationCounter, counter);
            _callback.Received().Invoke(Arg.Is((LogLevel)validationCounter - 2), Arg.Any<string>(), Arg.Is(false));

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info(TestConstants.TestMessage);
            Assert.AreEqual(validationCounter, counter);

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose(TestConstants.TestMessage);
            Assert.AreEqual(validationCounter, counter);
        }

        [TestMethod()]
        [DataRow(LogLevel.Always)]
        [DataRow(LogLevel.Error)]
        [DataRow(LogLevel.Warning)]
        [DataRow(LogLevel.Info)]
        [DataRow(LogLevel.Verbose)]
        public void CallbackTestLoggersPii(LogLevel level)
        {
            ILoggerAdapter logger = CreateLogger(level, true);
            var counter = 0;
            var validationCounter = 1;
            var levelToValidate = LogLevel.Always;
            Action incrementCounter = () => {

                if (level > levelToValidate)
                {
                    validationCounter++;
                }

                levelToValidate++;
            };

            _callback.When(x => x(LogLevel.Always, Arg.Any<string>(), true)).Do(x => counter++);
            logger.AlwaysPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(validationCounter, counter);

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(validationCounter, counter);

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(validationCounter, counter);

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(validationCounter, counter);

            incrementCounter.Invoke();

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(validationCounter, counter);
        }

        [TestMethod]
        public void IsEnabled()
        {
            var infoLoggerWithCallback = new LegacyIdentityLoggerAdapter(Guid.Empty, null, null, LogLevel.Info, true, true, _callback);
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Info));
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Always));
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Error));
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Warning));
            Assert.IsFalse(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Verbose));

            var loggerNoCallback = new LegacyIdentityLoggerAdapter(Guid.Empty, null, null, LogLevel.Warning, true, true, null);
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.Info));
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.Always));
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.Error));
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.Warning));
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.Verbose));

        }

        [TestMethod]
        [Description("LogCallback is public API. If its signature needs to change, it is a breaking change.")]
        public void PublicApi()
        {
            LogCallback callback = (lvl, msg, isPii) =>
            {
#pragma warning disable CS0183 // let it be for the test 
                Assert.IsTrue(lvl is LogLevel);
                Assert.IsTrue(msg is string);
                Assert.IsTrue(isPii is bool);
#pragma warning restore CS0183 //
            };
        }
    }
}
