// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TelemetryTests : TestBase
    {
        private const string ClientId = "a1b3c3d4";

        private IServiceBundle _serviceBundle;
        private IPlatformProxy _platformProxy;
        private ILoggerAdapter _logger;
        private ICryptographyManager _crypto;

        [TestInitialize]
        public void Initialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, clientId: ClientId);
            _logger = _serviceBundle.ApplicationLogger;
            _platformProxy = _serviceBundle.PlatformProxy;
            _crypto = _platformProxy.CryptographyManager;
        }

        private const string TenantId = "1234";
        private const string UserId = "5678";

#if NETFRAMEWORK
        [TestMethod]
        public async Task DoNotCallPlatformProxyAsync()
        {
            // Arrange
            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var mockProxy = new MockProxy();
                var app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                               .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                               .WithHttpManager(harness.HttpManager)
                               .WithPlatformProxy(mockProxy)
                               .WithRedirectUri("http://localhost")
                               .BuildConcrete();

                app.ServiceBundle.ConfigureMockWebUI();

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                // Act
                await app
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert - NoDeviceIdProxy would fail if InternalGetDeviceId is called
                Assert.IsFalse(mockProxy.InternalDeviceIdWasCalled);
            }
        }

        internal class MockProxy : Microsoft.Identity.Client.Platforms.netdesktop.NetDesktopPlatformProxy
        {

            public MockProxy() : base(new NullLogger())
            {
            }
            public bool InternalDeviceIdWasCalled { get; private set; }

            protected override string InternalGetDeviceId()
            {
                InternalDeviceIdWasCalled = true;
                return null;
            }
        }
#endif
    }
}
