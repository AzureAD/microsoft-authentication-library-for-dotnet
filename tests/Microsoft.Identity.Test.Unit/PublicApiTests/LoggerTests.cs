// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.IdentityModel.Abstractions;
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

            return new IdentityLoggerAdapter(new TestIdentityLogger(LoggerHelper.GetEventLogLevel(logLevel)), Guid.Empty, null, null, enablePiiLogging);
        }

        [TestMethod()]
        public void IdentityLoggerConstructorComponentTest()
        {
            ILoggerAdapter logger = new IdentityLoggerAdapter(null, Guid.Empty, "", "", false);
            Assert.AreEqual(string.Empty, logger.ClientName);
            Assert.AreEqual(string.Empty, logger.ClientVersion);
            logger = new IdentityLoggerAdapter(null, Guid.Empty, "comp1", "", false);
            Assert.AreEqual("comp1", logger.ClientName);
            logger = new IdentityLoggerAdapter(null, Guid.Empty, "comp1", "version1", false);
            Assert.AreEqual("comp1", logger.ClientName);
            Assert.AreEqual("version1", logger.ClientVersion);
        }

        [TestMethod()]
        public void LegacyLoggerConstructorComponentTest()
        {
            ILoggerAdapter logger = new LegacyIdentityLoggerAdapter(Guid.Empty, "", "", LogLevel.Always, false, false, null);
            Assert.AreEqual(string.Empty, logger.ClientName);
            Assert.AreEqual(string.Empty, logger.ClientVersion);
            logger = new LegacyIdentityLoggerAdapter(Guid.Empty, "comp1", null, LogLevel.Always, false, false, null);
            Assert.AreEqual("comp1", logger.ClientName);
            logger = new LegacyIdentityLoggerAdapter(Guid.Empty, "comp1", "version1", LogLevel.Always, false, false, null);
            Assert.AreEqual("comp1", logger.ClientName);
            Assert.AreEqual("version1", logger.ClientVersion);
        }

        [TestMethod()]
        [DataRow(LogLevel.Always, true)]
        [DataRow(LogLevel.Error, true)]
        [DataRow(LogLevel.Warning, true)]
        [DataRow(LogLevel.Info, true)]
        [DataRow(LogLevel.Verbose, true)]
        public void CallbackLoggerTest(LogLevel level, bool UseLegaccyLogger)
        {
            ILoggerAdapter logger = CreateLogger(level, false, UseLegaccyLogger);
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
        [DataRow(LogLevel.Always, true)]
        [DataRow(LogLevel.Error, true)]
        [DataRow(LogLevel.Warning, true)]
        [DataRow(LogLevel.Info, true)]
        [DataRow(LogLevel.Verbose, true)]
        public void CallbackTestLoggersPii(LogLevel level, bool UseLegaccyLogger)
        {
            ILoggerAdapter logger = CreateLogger(level, true, UseLegaccyLogger);
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

            var IdentityLogger = CreateLogger(LogLevel.Warning);
            Assert.IsFalse(IdentityLogger.IsLoggingEnabled(LogLevel.Info));
            Assert.IsTrue(IdentityLogger.IsLoggingEnabled(LogLevel.Always));
            Assert.IsTrue(IdentityLogger.IsLoggingEnabled(LogLevel.Error));
            Assert.IsTrue(IdentityLogger.IsLoggingEnabled(LogLevel.Warning));
            Assert.IsFalse(IdentityLogger.IsLoggingEnabled(LogLevel.Verbose));
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

        [TestMethod]
        public async Task IdentityLoggerOverridesLegacyLoggerTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                TestIdentityLogger testLogger = new TestIdentityLogger();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret("secret")
                    .WithExperimentalFeatures()
                    .WithLogging(testLogger, false)
                    .WithLogging((level, message, containsPii) => { Assert.Fail("MSAL should not use the logging callback"); })
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                var result = await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "some-code")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsTrue(testLogger.StringBuilder.ToString().Contains("AcquireTokenByAuthorizationCode"));
            }
        }

        [TestMethod]
        public void IdentityLoggeExperimental()
        {
            using (var httpManager = new MockHttpManager())
            {
                TestIdentityLogger testLogger = new TestIdentityLogger();

                var e = AssertException.Throws<MsalClientException>(() => ConfidentialClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithClientSecret("secret")
                   .WithLogging(testLogger, false)
                   .Build());

                Assert.AreEqual(MsalError.ExperimentalFeature, e.ErrorCode);
            }
        }
    }
}
