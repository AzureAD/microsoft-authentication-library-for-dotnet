// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET6_WIN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Platforms.Features.RuntimeBroker;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Account = Microsoft.Identity.Client.Account;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    [TestCategory(TestCategories.Broker)]
    public class RuntimeBrokerTests : TestBase
    {
        private CoreUIParent _coreUIParent;
        private ILoggerAdapter _logger;
        private RuntimeBroker _wamBroker;
        private SynchronizationContext _synchronizationContext;

        private MsalTokenResponse _msalTokenResponse = TokenCacheHelper.CreateMsalTokenResponse();

        private IServiceBundle _serviceBundle;

        [TestInitialize]
        public void Init()
        {
            _synchronizationContext = new DedicatedThreadSynchronizationContext();

            _coreUIParent = new CoreUIParent() { SynchronizationContext = _synchronizationContext };
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration(MsalClientType.PublicClient);
            applicationConfiguration.BrokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
            _logger = Substitute.For<ILoggerAdapter>();
            _logger.PiiLoggingEnabled.Returns(true);

            _serviceBundle = TestCommon.CreateDefaultServiceBundle();

            _wamBroker = new RuntimeBroker(_coreUIParent, applicationConfiguration, _logger);
        }

        [TestMethod]
        public void WamOnWin10()
        {
            if (!DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                Assert.Inconclusive("Needs to run on win10 or equivalent");
            }
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithAuthority(TestConstants.AuthorityTenant);

            pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

            Assert.IsTrue(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public void NoWamOnADFS()
        {
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithAdfsAuthority(TestConstants.ADFSAuthority);

            pcaBuilder = pcaBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

            Assert.IsFalse(pcaBuilder.IsBrokerAvailable());
        }

        [TestMethod]
        public async Task ThrowOnNoHandleAsync()
        {
            using (var handle = base.CreateTestHarness())
            {
                handle.HttpManager.AddInstanceDiscoveryMockHandler();
                var pcaBuilder = PublicClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithHttpManager(handle.HttpManager)
                   .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

                var pca = pcaBuilder.Build();

                // no window handle - throw
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => pca.AcquireTokenInteractive(new[] { "" }).ExecuteAsync()).ConfigureAwait(false);

                Assert.AreEqual("window_handle_required", ex.ErrorCode);
            }
           
        }
       
        [TestMethod]
        public void HandleInstallUrl_Throws()
        {
            AssertException.Throws<NotImplementedException>(() => _wamBroker.HandleInstallUrl("http://app"));
        }

        [TestMethod]
        public async Task ATS_CallsLog_When_CalledAsync()
        {
            // Arrange
            var appTokenCache = new TokenCache(_serviceBundle, isApplicationTokenCache: false);
            var requestContext = new RequestContext(_serviceBundle, Guid.NewGuid());
            var tenantAuthority = AuthorityInfo.FromAadAuthority(AzureCloudInstance.AzurePublic, tenant: TestConstants.AadTenantId, validateAuthority: false);
            var acquireTokenCommonParameters = new AcquireTokenCommonParameters
            {
                ApiId = ApiEvent.ApiIds.AcquireTokenSilent,
                AuthorityOverride = tenantAuthority                
            };

            var authRequestParams = new AuthenticationRequestParameters(
                _serviceBundle,
                appTokenCache,
                acquireTokenCommonParameters,
                requestContext,
                Authority.CreateAuthority(tenantAuthority));
            authRequestParams.Scope.Add("User.Read");

            AcquireTokenSilentParameters silentParams = new AcquireTokenSilentParameters();
            silentParams.Account = PublicClientApplication.OperatingSystemAccount;
            authRequestParams.Account = PublicClientApplication.OperatingSystemAccount;

            // Act
            try
            {
                _ = await _wamBroker.AcquireTokenSilentAsync(authRequestParams, silentParams).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                // Assert
                _logger.Received().Log(Arg.Any<Client.LogLevel>(), Arg.Any<string>(), Arg.Any<string>());
            }
        }

        [DataTestMethod]
        [DataRow(Client.NativeInterop.LogLevel.Trace, Client.LogLevel.Verbose)]
        [DataRow(Client.NativeInterop.LogLevel.Debug, Client.LogLevel.Verbose)]
        [DataRow(Client.NativeInterop.LogLevel.Info, Client.LogLevel.Info)]
        [DataRow(Client.NativeInterop.LogLevel.Warning, Client.LogLevel.Warning)]
        [DataRow(Client.NativeInterop.LogLevel.Error, Client.LogLevel.Error)]
        [DataRow(Client.NativeInterop.LogLevel.Fatal, Client.LogLevel.Error)]
        public void LogEventRaised_MapsEvents_Correctly_When_No_Pii(Client.NativeInterop.LogLevel nativeLogLevel, Client.LogLevel msalLogLevel)
        {
            const string logMessage = "This is test";
            _logger.IsLoggingEnabled(msalLogLevel).Returns(true);
            _logger.PiiLoggingEnabled.Returns(false);

            Type wamBrokerType = _wamBroker.GetType();
            MethodInfo fireLogMethod = wamBrokerType.GetMethod("LogEventRaised", BindingFlags.NonPublic | BindingFlags.Instance);
            fireLogMethod.Invoke(_wamBroker, new object[] { null, new LogEventArgs(nativeLogLevel, logMessage) });

            // Assert
            _logger.Received().Log(msalLogLevel, string.Empty, logMessage);
        }

        [DataTestMethod]
        [DataRow(Client.NativeInterop.LogLevel.Trace, Client.LogLevel.Verbose)]
        [DataRow(Client.NativeInterop.LogLevel.Debug, Client.LogLevel.Verbose)]
        [DataRow(Client.NativeInterop.LogLevel.Info, Client.LogLevel.Info)]
        [DataRow(Client.NativeInterop.LogLevel.Warning, Client.LogLevel.Warning)]
        [DataRow(Client.NativeInterop.LogLevel.Error, Client.LogLevel.Error)]
        [DataRow(Client.NativeInterop.LogLevel.Fatal, Client.LogLevel.Error)]
        public void LogEventRaised_DoesNotLog_When_NotForLevel(Client.NativeInterop.LogLevel nativeLogLevel, Client.LogLevel msalLogLevel)
        {
            const string logMessage = "This is test";
            _logger.IsLoggingEnabled(msalLogLevel).Returns(false);
            _logger.PiiLoggingEnabled.Returns(true);

            Type wamBrokerType = _wamBroker.GetType();
            MethodInfo fireLogMethod = wamBrokerType.GetMethod("LogEventRaised", BindingFlags.NonPublic | BindingFlags.Instance);
            fireLogMethod.Invoke(_wamBroker, new object[] { null, new LogEventArgs(nativeLogLevel, logMessage) });

            // Assert
            _logger.DidNotReceiveWithAnyArgs().Log(Arg.Any<Client.LogLevel>(), Arg.Any<string>(), Arg.Any<String>());
        }
    }
}
#endif
