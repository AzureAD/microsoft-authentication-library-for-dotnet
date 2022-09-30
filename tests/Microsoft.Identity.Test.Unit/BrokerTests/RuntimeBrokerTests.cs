// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE && !NET6_0
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
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.Runtime.InteropServices;
using System.Linq;

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

        [TestInitialize]
        public void Init()
        {
            _synchronizationContext = new DedicatedThreadSynchronizationContext();

            _coreUIParent = new CoreUIParent() { SynchronizationContext = _synchronizationContext };
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            _logger = Substitute.For<ILoggerAdapter>();

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

            pcaBuilder = pcaBuilder.WithBrokerPreview();
            Assert.IsTrue(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public void NoWamOnADFS()
        {
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithAdfsAuthority(TestConstants.ADFSAuthority);

            pcaBuilder = pcaBuilder.WithBrokerPreview();

            Assert.IsFalse(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public async Task ThrowOnNoHandleAsync()
        {
            var pca = PublicClientApplicationBuilder
               .Create(TestConstants.ClientId)
               .WithBrokerPreview()
               .Build();

            // no window handle - throw
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => pca.AcquireTokenInteractive(new[] { "" }).ExecuteAsync()).ConfigureAwait(false);

            Assert.AreEqual("window_handle_required", ex.ErrorCode);
           
        }
       
        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("openid")]
        [DataRow("profile")]
        [DataRow("offline_access")]
        [DataRow("openid offline_access")]
        [DataRow("profile offline_access")]        
        [DataRow("profile offline_access openid")]        
        public async Task ThrowOnNoScopesAsync(string scopes)
        {
            var scopeArray = new List<string>();
            if (scopes != null)
            {
                scopeArray = scopes.Split(" ").ToList();
            }

            var pca = PublicClientApplicationBuilder
               .Create(TestConstants.ClientId)
               .WithBrokerPreview()
               .Build();

            // empty scopes
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => pca
                .AcquireTokenInteractive(scopeArray)
                .WithParentActivityOrWindow(new IntPtr(123456))
                .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.WamScopesRequired, ex.ErrorCode);

            // empty scopes
            var ex2 = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => pca
                .AcquireTokenSilent(scopeArray, new Account("123.123", "user", "env"))                
                .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.WamScopesRequired, ex2.ErrorCode);

        }

        [TestMethod]
        public void HandleInstallUrl_Throws()
        {
            AssertException.Throws<NotImplementedException>(() => _wamBroker.HandleInstallUrl("http://app"));
        }
    }
}

#endif
