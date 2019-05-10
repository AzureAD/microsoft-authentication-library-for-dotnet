// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
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
        [TestCategory("LoggerTests")]
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
        [TestCategory("LoggerTests")]
        public void CallbackTestErrorTest()
        {
            MsalLogger logger = CreateLogger(LogLevel.Error);
            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error("test message");
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.Error), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.Error), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestWarning()
        {
            MsalLogger logger = CreateLogger(LogLevel.Warning);
            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).
                Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestInfo()
        {
            MsalLogger logger = CreateLogger(LogLevel.Info);

            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestVerbose()
        {
            MsalLogger logger = CreateLogger(LogLevel.Verbose);

            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestErrorPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Error, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestWarningPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Warning, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestInfoPii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Info, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestVerbosePii()
        {
            MsalLogger logger = CreateLogger(LogLevel.Verbose, true);

            var counter = 0;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(4, counter);
        }
    }
}
