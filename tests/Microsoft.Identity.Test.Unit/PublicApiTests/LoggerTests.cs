// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
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

        private MsalLogger CreateLogger(LogLevel logLevel = LogLevel.Verbose, bool enablePiiLogging = false)
        {
            return new MsalLogger(Guid.Empty, null, null, logLevel, enablePiiLogging, true, _callback);
        }

        [TestMethod()]
        public void ConstructorComponentTest()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null, null, LogLevel.Verbose, false, true, null);
            Assert.AreEqual(string.Empty, logger.ClientName);
            Assert.AreEqual(string.Empty, logger.ClientVersion);
            Assert.AreEqual(string.Empty, logger.ClientInformation);
            logger = new MsalLogger(Guid.Empty, "comp1", null, LogLevel.Verbose, false, true, null);
            Assert.AreEqual(" (comp1)", logger.ClientInformation);
            logger = new MsalLogger(Guid.Empty, "comp1", "version1", LogLevel.Verbose, false, true, null);
            Assert.AreEqual(" (comp1: version1)", logger.ClientInformation);
        }

        [TestMethod()]
        public void CallbackTestHealthMetricTest()
        {
            MsalLogger logger = CreateLogger(LogLevel.HealthMetric);
            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), false)).Do(x => counter++);
            logger.HealthMetric(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.HealthMetric), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.HealthMetric), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.HealthMetric), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        public void CallbackTestErrorTest()
        {
            MsalLogger logger = CreateLogger(LogLevel.Error);
            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), false)).Do(x => counter++);
            logger.HealthMetric(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.HealthMetric), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error(TestConstants.TestMessage);
            Assert.AreEqual(2, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.Error), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning(TestConstants.TestMessage);
            Assert.AreEqual(2, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.Error), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info(TestConstants.TestMessage);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose(TestConstants.TestMessage);
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        public void CallbackTestWarning()
        {
            MsalLogger logger = CreateLogger(LogLevel.Warning);
            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), false)).Do(x => counter++);
            logger.HealthMetric(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning(TestConstants.TestMessage);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info(TestConstants.TestMessage);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose(TestConstants.TestMessage);
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        public void CallbackTestInfo()
        {
            MsalLogger logger = CreateLogger(LogLevel.Info);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), false)).Do(x => counter++);
            logger.HealthMetric(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning(TestConstants.TestMessage);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info(TestConstants.TestMessage);
            Assert.AreEqual(4, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose(TestConstants.TestMessage);
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        public void CallbackTestVerbose()
        {
            MsalLogger logger = CreateLogger(LogLevel.Verbose);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), false)).Do(x => counter++);
            logger.HealthMetric(TestConstants.TestMessage);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning(TestConstants.TestMessage);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info(TestConstants.TestMessage);
            Assert.AreEqual(4, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose(TestConstants.TestMessage);
            Assert.AreEqual(5, counter);
        }

        [TestMethod()]
        public void CallbackTestHealthMetricPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.HealthMetric, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), true)).Do(x => counter++);
            logger.HealthMetricPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        public void CallbackTestErrorPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Error, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), true)).Do(x => counter++);
            logger.HealthMetricPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        public void CallbackTestWarningPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Warning, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), true)).Do(x => counter++);
            logger.HealthMetricPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        public void CallbackTestInfoPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Info, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), true)).Do(x => counter++);
            logger.HealthMetricPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(4, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        public void CallbackTestVerbosePii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Verbose, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.HealthMetric, Arg.Any<string>(), true)).Do(x => counter++);
            logger.HealthMetricPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException(TestConstants.TestMessage));
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(4, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii(TestConstants.TestMessage, string.Empty);
            Assert.AreEqual(5, counter);
        }

        [TestMethod]
        public void IsEnabled()
        {
            var infoLoggerWithCallback = new MsalLogger(Guid.Empty, null, null, LogLevel.Info, true, true, _callback);
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Info));
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.HealthMetric));
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Error));
            Assert.IsTrue(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Warning));
            Assert.IsFalse(infoLoggerWithCallback.IsLoggingEnabled(LogLevel.Verbose));

            var loggerNoCallback = new MsalLogger(Guid.Empty, null, null, LogLevel.Warning, true, true, null);
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.Info));
            Assert.IsFalse(loggerNoCallback.IsLoggingEnabled(LogLevel.HealthMetric));
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
