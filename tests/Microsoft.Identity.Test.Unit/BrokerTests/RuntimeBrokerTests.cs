// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Broker;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    [TestCategory("Broker")]
    public class RuntimeBrokerTests : TestBase
    {
        private CoreUIParent _coreUIParent;
        private ICoreLogger _logger;
        private RuntimeBroker _wamBroker;
        private SynchronizationContext _synchronizationContext;

        private MsalTokenResponse _msalTokenResponse = TokenCacheHelper.CreateMsalTokenResponse();

        [TestInitialize]
        public void Init()
        {
            _synchronizationContext = new DedicatedThreadSynchronizationContext();

            _coreUIParent = new CoreUIParent() { SynchronizationContext = _synchronizationContext };
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            _logger = Substitute.For<ICoreLogger>();

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

            pcaBuilder = pcaBuilder.WithBroker2();
            Assert.IsTrue(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public void NoWamOnADFS()
        {
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithAdfsAuthority(TestConstants.ADFSAuthority);

            pcaBuilder = pcaBuilder.WithBroker2();

            Assert.IsFalse(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public void HandleInstallUrl_Throws()
        {
            AssertException.Throws<NotImplementedException>(() => _wamBroker.HandleInstallUrl("http://app"));
        }

        [TestMethod]
        public async Task WamSilentAuthAsync()
        {
            string[] scopes = new[]
                {
                    "https://management.core.windows.net//.default"
                };

            var pcaBuilder = PublicClientApplicationBuilder
               .Create("04f0c124-f2bc-4f59-8241-bf6df9866bbd")
               .WithAuthority(TestConstants.AuthorityOrganizationsTenant);

            pcaBuilder = pcaBuilder.WithBroker2();
            var pca = pcaBuilder.Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Need user interaction to continue"));
            }

        }

        [TestMethod]
        public async Task WamSilentAuthWithLabAppAsync()
        {
            string[] scopes = new[]
                {
                    "user.read"
                };

            var pcaBuilder = PublicClientApplicationBuilder
               .Create("4b0db8c2-9f26-4417-8bde-3f0e3656f8e0")
               .WithAuthority(TestConstants.AuthorityOrganizationsTenant);

            pcaBuilder = pcaBuilder.WithBroker2();
            var pca = pcaBuilder.Build();

            // Act
            try
            {
                var result = await pca.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
            catch (MsalUiRequiredException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Need user interaction to continue"));
            }

        }
    }    
}

#endif
