// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DoNotParallelize]
    public class ReducedLoggingTests : TestBase
    {
        private string _originalEnvironmentValue;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _originalEnvironmentValue = Environment.GetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable);
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, null);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, _originalEnvironmentValue);
            base.TestCleanup();
        }

        [TestMethod]
        [DataRow(null, false, false)]
        [DataRow("", false, false)]
        [DataRow("false", false, false)]
        [DataRow("FALSE", false, false)]
        [DataRow("0", false, false)]
        [DataRow("true", true, false)]
        [DataRow("TrUe", true, false)]
        [DataRow("1", true, false)]
        [DataRow("invalid-value", false, true)]
        [DataRow(" true ", false, true)]
        public void EnvironmentSettingDefaultsToFalse(string value, bool expectedEnabled, bool expectedWarning)
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, value);
            var entries = new List<LogEntry>();
            var builder = CreateBuilder(false, LogLevel.Info, false, entries);

            // Act
            var app = builder.BuildConcrete();
            var context = new RequestContext(app.ServiceBundle, Guid.NewGuid(), null);

            // Assert
            Assert.AreEqual(expectedEnabled, app.ServiceBundle.ApplicationLogger.IsReducedLoggingEnabled);
            Assert.AreEqual(expectedEnabled, context.Logger.IsReducedLoggingEnabled);
            var warnings = entries.Where(e => e.Message.Contains("Invalid MSAL_REDUCED_LOGGING")).ToList();
            Assert.HasCount(expectedWarning ? 1 : 0, warnings);
            if (expectedWarning)
            {
                Assert.AreEqual(EventLogLevel.Warning, warnings[0].EventLogLevel);
                Assert.DoesNotContain(value, warnings[0].Message);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EnvironmentSettingIsCapturedPerApplicationEvenWhenBuilderIsReused(bool callback)
        {
            // Arrange
            var entries = new List<LogEntry>();
            var builder = CreateBuilder(callback, LogLevel.Info, false, entries);
            var originalApp = builder.BuildConcrete();

            // Act
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, "true");
            var reducedApp = builder.BuildConcrete();
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, "false");
            var originalContext = new RequestContext(originalApp.ServiceBundle, Guid.NewGuid(), null);
            var reducedContext = new RequestContext(reducedApp.ServiceBundle, Guid.NewGuid(), null);

            // Assert
            Assert.IsFalse(originalApp.ServiceBundle.ApplicationLogger.IsReducedLoggingEnabled);
            Assert.IsFalse(originalContext.Logger.IsReducedLoggingEnabled);
            Assert.IsTrue(reducedApp.ServiceBundle.ApplicationLogger.IsReducedLoggingEnabled);
            Assert.IsTrue(reducedContext.Logger.IsReducedLoggingEnabled);
        }

        [TestMethod]
        [DataRow(false, false, LogLevel.Info)]
        [DataRow(true, false, LogLevel.Info)]
        [DataRow(false, true, LogLevel.Info)]
        [DataRow(true, true, LogLevel.Info)]
        [DataRow(false, true, LogLevel.Verbose)]
        [DataRow(true, true, LogLevel.Verbose)]
        [DataRow(false, false, LogLevel.Warning)]
        [DataRow(true, true, LogLevel.Warning)]
        public void DiagnosticFormattingIsSkippedAtTheEffectiveLevel(bool callback, bool reduced, LogLevel threshold)
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, reduced.ToString());
            var entries = new List<LogEntry>();
            var logger = CreateBuilder(callback, threshold, false, entries).BuildConcrete().ServiceBundle.ApplicationLogger;
            entries.Clear();
            int producerCalls = 0;
            int scopeEnumerations = 0;
            var parameters = new AcquireTokenInteractiveParameters
            {
                ExtraScopesToConsent = new[] { "scope" }.Select(scope =>
                {
                    scopeEnumerations++;
                    return scope;
                })
            };

            // Act
            logger.InfoOrVerbose(() =>
            {
                producerCalls++;
                return "routine diagnostic";
            });
            parameters.LogParameters(logger);

            // Assert
            LogLevel effectiveLevel = reduced ? LogLevel.Verbose : LogLevel.Info;
            bool expectedEnabled = threshold >= effectiveLevel;
            Assert.AreEqual(expectedEnabled, logger.IsInfoOrVerboseEnabled());
            Assert.AreEqual(expectedEnabled ? 1 : 0, producerCalls);
            Assert.AreEqual(expectedEnabled ? 1 : 0, scopeEnumerations);
            Assert.HasCount(expectedEnabled ? 2 : 0, entries);
            Assert.IsTrue(entries.All(e => e.EventLogLevel == LoggerHelper.GetEventLogLevel(effectiveLevel)));
        }

        [TestMethod]
        [DataRow(false, false, LogLevel.Info)]
        [DataRow(true, false, LogLevel.Info)]
        [DataRow(false, true, LogLevel.Info)]
        [DataRow(true, true, LogLevel.Info)]
        [DataRow(false, true, LogLevel.Verbose)]
        [DataRow(true, true, LogLevel.Verbose)]
        public void ParameterDumpsUseTheEffectiveLevelAcrossFlows(bool callback, bool reduced, LogLevel threshold)
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, reduced.ToString());
            var entries = new List<LogEntry>();
            var logger = CreateBuilder(callback, threshold, false, entries).BuildConcrete().ServiceBundle.ApplicationLogger;
            var cases = new (IAcquireTokenParameters Parameters, int Count)[]
            {
                (new AcquireTokenForClientParameters(), 1),
                (new AcquireTokenForManagedIdentityParameters(), 1),
                (new AcquireTokenOnBehalfOfParameters(), 1),
                (new AcquireTokenByUserFederatedIdentityCredentialParameters(), 1),
                (new AcquireTokenSilentParameters(), 4),
                (new AcquireTokenInteractiveParameters(), 1)
            };

            foreach (var testCase in cases)
            {
                entries.Clear();

                // Act
                testCase.Parameters.LogParameters(logger);

                // Assert
                LogLevel effectiveLevel = reduced ? LogLevel.Verbose : LogLevel.Info;
                Assert.HasCount(threshold >= effectiveLevel ? testCase.Count : 0, entries, testCase.Parameters.GetType().Name);
                Assert.IsTrue(entries.All(e => e.EventLogLevel == LoggerHelper.GetEventLogLevel(effectiveLevel)));
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void ReducedLoggingPreservesPiiSelectionAndOtherLevels(bool callback, bool pii)
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, "true");
            var entries = new List<LogEntry>();
            var logger = CreateBuilder(callback, LogLevel.Verbose, pii, entries).BuildConcrete().ServiceBundle.ApplicationLogger;
            entries.Clear();

            // Act
            logger.InfoOrVerbosePii("private diagnostic", "scrubbed diagnostic");
            logger.InfoOrVerbosePii(string.Empty, "fallback diagnostic");
            logger.Info("retained summary");
            logger.Warning("retained warning");
            logger.Error("retained error");
            logger.Always("retained always");

            // Assert
            Assert.HasCount(6, entries);
            Assert.AreEqual(EventLogLevel.Verbose, entries[0].EventLogLevel);
            Assert.Contains(pii ? "private diagnostic" : "scrubbed diagnostic", entries[0].Message);
            Assert.DoesNotContain(pii ? "scrubbed diagnostic" : "private diagnostic", entries[0].Message);
            Assert.Contains("fallback diagnostic", entries[1].Message);
            CollectionAssert.AreEqual(
                new[] { EventLogLevel.Verbose, EventLogLevel.Verbose, EventLogLevel.Informational, EventLogLevel.Warning, EventLogLevel.Error, EventLogLevel.LogAlways },
                entries.Select(e => e.EventLogLevel).ToArray());
        }

        [TestMethod]
        [DataRow(false, false, LogLevel.Info)]
        [DataRow(true, false, LogLevel.Info)]
        [DataRow(false, true, LogLevel.Info)]
        [DataRow(true, true, LogLevel.Info)]
        [DataRow(false, true, LogLevel.Verbose)]
        [DataRow(true, true, LogLevel.Verbose)]
        public async Task CacheHitRetainsSummariesAndDemotesSixRoutineEventsAsync(bool callback, bool reduced, LogLevel threshold)
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, reduced.ToString());
            var entries = new List<LogEntry>();
            using (var httpManager = new MockHttpManager())
            {
                var app = CreateBuilder(callback, threshold, false, entries).WithHttpManager(httpManager).Build();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                });
                var firstResult = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
                entries.Clear();

                // Act
                var cachedResult = await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(firstResult.AccessToken, cachedResult.AccessToken);
                Assert.AreEqual(firstResult.ExpiresOn, cachedResult.ExpiresOn);
                Assert.HasCount(reduced ? 2 : 8, entries.Where(e => e.EventLogLevel == EventLogLevel.Informational).ToList());
                Assert.HasCount(4, entries.Where(e => e.EventLogLevel == EventLogLevel.LogAlways).ToList());
                string[] routineMessages =
                {
                    "with assembly version",
                    "AcquireTokenForClientParameters",
                    "=== Request Data ===",
                    "Not using a regional authority",
                    "Access token is not expired",
                    "Token Acquisition finished successfully"
                };
                foreach (string message in routineMessages)
                {
                    var matching = entries.Where(e => e.Message.Contains(message)).ToList();
                    Assert.HasCount(reduced && threshold == LogLevel.Info ? 0 : 1, matching, message);
                    if (matching.Count != 0)
                    {
                        Assert.AreEqual(reduced ? EventLogLevel.Verbose : EventLogLevel.Informational, matching[0].EventLogLevel, message);
                    }
                }

                Assert.IsTrue(entries.Any(e => e.EventLogLevel == EventLogLevel.Informational && e.Message.Contains("started:")));
                Assert.IsTrue(entries.Any(e => e.EventLogLevel == EventLogLevel.Informational && e.Message.Contains("source: Cache")));
            }
        }

        [TestMethod]
        public void NoLoggerStillSkipsDiagnosticProducer()
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, "true");
            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId).WithClientSecret("secret").BuildConcrete();
            int producerCalls = 0;

            // Act
            app.ServiceBundle.ApplicationLogger.InfoOrVerbose(() =>
            {
                producerCalls++;
                return "unused diagnostic";
            });

            // Assert
            Assert.AreEqual(0, producerCalls);
            Assert.AreSame(LoggerHelper.NullLogger, app.ServiceBundle.ApplicationLogger);
        }

        [TestMethod]
        public async Task DemotedSuccessHeadingDoesNotRequireInfoToBeEnabledAsync()
        {
            // Arrange
            Environment.SetEnvironmentVariable(LoggerHelper.ReducedLoggingEnvironmentVariable, "true");
            var entries = new List<LogEntry>();
            var identityLogger = Substitute.For<IIdentityLogger>();
            identityLogger.IsEnabled(Arg.Any<EventLogLevel>())
                .Returns(call => call.Arg<EventLogLevel>() == EventLogLevel.Verbose);
            identityLogger.When(l => l.Log(Arg.Any<LogEntry>())).Do(call => entries.Add(call.Arg<LogEntry>()));
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithClientSecret("secret")
                    .WithLogging(identityLogger)
                    .WithHttpManager(httpManager)
                    .Build();
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
                });

                // Act
                await app.AcquireTokenForClient(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

                // Assert
                Assert.IsTrue(entries.All(e => e.EventLogLevel == EventLogLevel.Verbose));
                Assert.HasCount(1, entries.Where(e => e.Message.Contains("Token Acquisition finished successfully")).ToList());
                Assert.HasCount(1, entries.Where(e => e.Message.Contains("=== Request Data ===")).ToList());
            }
        }

        private static ConfidentialClientApplicationBuilder CreateBuilder(bool callback, LogLevel threshold, bool pii, List<LogEntry> entries)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityTestTenant)
                .WithClientSecret("secret");
            if (callback)
            {
                return builder.WithLogging(
                    (level, message, containsPii) => entries.Add(new LogEntry { EventLogLevel = LoggerHelper.GetEventLogLevel(level), Message = message }),
                    threshold,
                    pii);
            }

            var identityLogger = Substitute.For<IIdentityLogger>();
            identityLogger.IsEnabled(Arg.Any<EventLogLevel>())
                .Returns(call => call.Arg<EventLogLevel>() <= LoggerHelper.GetEventLogLevel(threshold));
            identityLogger.When(l => l.Log(Arg.Any<LogEntry>())).Do(call => entries.Add(call.Arg<LogEntry>()));
            return builder.WithLogging(identityLogger, pii);
        }
    }
}
