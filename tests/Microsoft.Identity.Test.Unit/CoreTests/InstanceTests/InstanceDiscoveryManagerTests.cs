// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class InstanceDiscoveryManagerTests : TestBase
    {
        private const string TestAuthority = "https://some_env.com/tid/";
        private INetworkMetadataProvider _networkMetadataProvider;
        private IKnownMetadataProvider _knownMetadataProvider;
        private INetworkCacheMetadataProvider _networkCacheMetadataProvider;
        private IUserMetadataProvider _userMetadataProvider;

        private InstanceDiscoveryMetadataEntry _expectedResult;
        private MockHttpAndServiceBundle _harness;
        private RequestContext _testRequestContext;
        private InstanceDiscoveryManager _discoveryManager;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            _networkMetadataProvider = Substitute.For<INetworkMetadataProvider>();
            _knownMetadataProvider = Substitute.For<IKnownMetadataProvider>();
            _networkCacheMetadataProvider = Substitute.For<INetworkCacheMetadataProvider>();
            _userMetadataProvider = Substitute.For<IUserMetadataProvider>();

            InitializeTestObjects();
        }

        private void InitializeTestObjects(bool isInstanceDiscoveryEnabled = true)
        {
            _expectedResult = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "some_env.com", "some_env2.com" },
                PreferredCache = "env",
                PreferredNetwork = "env"
            };

            _harness = base.CreateTestHarness(isInstanceDiscoveryEnabled: isInstanceDiscoveryEnabled);

            _testRequestContext = new RequestContext(_harness.ServiceBundle, Guid.NewGuid());
            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                false,
                null,
                null,
                _knownMetadataProvider,
                _networkCacheMetadataProvider,
                _networkMetadataProvider);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            _harness?.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        public async Task B2C_GetMetadataAsync()
        {
            await ValidateSelfEntryAsync(new Uri(TestConstants.B2CAuthority))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ADFS_GetMetadataAsync()
        {
            await ValidateSelfEntryAsync(new Uri(TestConstants.ADFSAuthority))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NetworkCacheProvider_IsUsedFirst_Async()
        {
            // Arrange
            INetworkMetadataProvider networkMetadataProvider = new NetworkMetadataProvider(
                Substitute.For<IHttpManager>(),
                _networkCacheMetadataProvider);

            _networkCacheMetadataProvider.GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>()).Returns(_expectedResult);

            _discoveryManager = new InstanceDiscoveryManager(
              _harness.HttpManager,
              false,
              null,
              null,
              _knownMetadataProvider,
              _networkCacheMetadataProvider,
              networkMetadataProvider);

            // Act
            InstanceDiscoveryMetadataEntry actualResult1 = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                new[] { "env1", "env2" },
                _testRequestContext)
                .ConfigureAwait(false);
            _networkCacheMetadataProvider.Received(1).GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>());

            InstanceDiscoveryMetadataEntry actualResult2 = await _discoveryManager.GetMetadataEntryAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                _testRequestContext)
                .ConfigureAwait(false);
            _networkCacheMetadataProvider.Received(2).GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>());
            _networkCacheMetadataProvider.AddMetadata(null, null);

            // Assert
            Assert.AreSame(_expectedResult, actualResult1, "The static provider should be queried first");
            Assert.AreSame(_expectedResult, actualResult2, "The static provider should be queried first");
        }

        [TestMethod]
        public async Task InstanceDiscoveryDisabled_Async()
        {
            // Arrange
            InitializeTestObjects(false);
            INetworkMetadataProvider networkMetadataProvider = new NetworkMetadataProvider(
                Substitute.For<IHttpManager>(),
                _networkCacheMetadataProvider);

            _networkCacheMetadataProvider.GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>()).Returns(_expectedResult);

            _discoveryManager = new InstanceDiscoveryManager(
              _harness.HttpManager,
              false,
              null,
              null,
              _knownMetadataProvider,
              _networkCacheMetadataProvider,
              networkMetadataProvider);

            // Act
            InstanceDiscoveryMetadataEntry actualResult1 = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                new[] { "env1", "env2" },
                _testRequestContext)
                .ConfigureAwait(false);

            //Ensures that no network call is made for instance discovery
            _networkCacheMetadataProvider.Received(0).GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>());

            InstanceDiscoveryMetadataEntry actualResult2 = await _discoveryManager.GetMetadataEntryAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                _testRequestContext)
                .ConfigureAwait(false);

            //Ensures that no network call is made for instance discovery
            _networkCacheMetadataProvider.Received(0).GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>());
            _networkCacheMetadataProvider.AddMetadata(null, null);

            // Assert
            Assert.AreEqual("some_env.com", actualResult1.Aliases.Single());
            Assert.AreEqual("some_env.com", actualResult1.PreferredCache);
            Assert.AreEqual("some_env.com", actualResult1.PreferredNetwork);

            Assert.AreEqual("some_env.com", actualResult2.Aliases.Single());
            Assert.AreEqual("some_env.com", actualResult2.PreferredCache);
            Assert.AreEqual("some_env.com", actualResult2.PreferredNetwork);

            //Ensure cached result is not returned
            Assert.AreNotSame(_expectedResult, actualResult1);
            Assert.AreNotSame(_expectedResult, actualResult2);
        }

        [TestMethod]
        public async Task KnownMetadataProviderIsCheckedSecondAsync()
        {
            // Arrange
            var otherEnvs = new[] { "env1", "env2" };

            // No response from the static provider
            _networkCacheMetadataProvider.GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>()).Returns((InstanceDiscoveryMetadataEntry)null);

            _knownMetadataProvider.GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>()).Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _networkCacheMetadataProvider.Received(1).GetMetadata("some_env.com", Arg.Any<ILoggerAdapter>());
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>());
        }

        [TestMethod]
        public async Task AuthorityValidationFailure_IsRethrown_Async()
        {
            // Arrange
            var validationException = new MsalServiceException(MsalError.InvalidInstance, "authority validation failed");
            _networkCacheMetadataProvider = new NetworkCacheMetadataProvider();

            // network fails with invalid_instance exception
            _networkMetadataProvider
                .When(x => x.GetMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(_ => throw validationException);

            // Act
            var actualException = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
            _discoveryManager.GetMetadataEntryAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                _testRequestContext))
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(validationException, actualException);
            _knownMetadataProvider.DidNotReceiveWithAnyArgs().GetMetadata(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<ILoggerAdapter>());
        }

        [TestMethod]
        public async Task NetworkProviderFailures_AreIgnored_Async()
        {
            // Arrange
            _networkCacheMetadataProvider = new NetworkCacheMetadataProvider();
            _knownMetadataProvider.GetMetadata("some_env.com", Enumerable.Empty<string>(), Arg.Any<ILoggerAdapter>()).Returns(_expectedResult);

            // network fails with something other than invalid_instance exception
            _networkMetadataProvider
                .When(x => x.GetMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(_ => throw new MsalServiceException("endpoint_busy", "some exception message"));

            // Act
            var actualResult = await _discoveryManager.GetMetadataEntryAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", Enumerable.Empty<string>(), Arg.Any<ILoggerAdapter>());
        }

        [TestMethod]
        public async Task NetworkProviderFailures_WithNoKnownMetadata_ContinuesWithAuthority_Async()
        {
            // Arrange
            _networkCacheMetadataProvider = new NetworkCacheMetadataProvider();

            // no known metadata 
            _knownMetadataProvider.GetMetadata(null, null, Arg.Any<ILoggerAdapter>()).ReturnsForAnyArgs((InstanceDiscoveryMetadataEntry)null);

            // network fails with something other than invalid_instance exception
            _networkMetadataProvider
                .When(x => x.GetMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(_ => throw new MsalServiceException("endpoint_busy", "some exception message"));

            // Act
            var actualResult = await _discoveryManager.GetMetadataEntryAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", Enumerable.Empty<string>(), Arg.Any<ILoggerAdapter>());
            ValidateSingleEntryMetadata(new Uri("https://some_env.com/tid"), actualResult);
        }

        [TestMethod]
        public async Task NetworkProviderIsCalledLastAsync()
        {
            // Arrange
            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                false,
                null,
                null,
                _knownMetadataProvider,
                _networkCacheMetadataProvider,
                _networkMetadataProvider);

            var otherEnvs = new[] { "env1", "env2" };
            var authorityUri = new Uri(TestAuthority);

            // No response from the static and known provider
            _knownMetadataProvider
                .GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>())
                .Returns((InstanceDiscoveryMetadataEntry)null);

            _networkMetadataProvider
                .GetMetadataAsync(authorityUri, _testRequestContext)
                .Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>());
            await _networkMetadataProvider.Received(1).GetMetadataAsync(authorityUri, _testRequestContext).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UserProvider_TakesPrecedence_OverNetworkProvider_Async()
        {
            // Arrange
            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                false,
                _userMetadataProvider,
                null,
                _knownMetadataProvider,
                _networkCacheMetadataProvider,
                _networkMetadataProvider);

            var otherEnvs = new[] { "env1", "env2" };
            var authorityUri = new Uri(TestAuthority);

            // No response from the static and known provider
            _knownMetadataProvider
                .GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>())
                .Returns((InstanceDiscoveryMetadataEntry)null);

            _networkMetadataProvider
                .GetMetadataAsync(authorityUri, _testRequestContext)
                .Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>());
            await _networkMetadataProvider.Received(1).GetMetadataAsync(authorityUri, _testRequestContext).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CustomDiscoveryEndpoint_Async()
        {
            // Arrange
            Uri customDiscoveryEndpoint = new Uri("http://some.discovery.endpoint");

            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                false,
                null,
                customDiscoveryEndpoint,
                _knownMetadataProvider,
                _networkCacheMetadataProvider,
                _networkMetadataProvider);

            var otherEnvs = new[] { "env1", "env2" };
            var authorityUri = new Uri(TestAuthority);

            // No response from the static and known provider
            _knownMetadataProvider
                .GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>())
                .Returns((InstanceDiscoveryMetadataEntry)null);

            _networkMetadataProvider
                .GetMetadataAsync(authorityUri, _testRequestContext)
                .Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs, Arg.Any<ILoggerAdapter>());
            await _networkMetadataProvider.Received(1).GetMetadataAsync(authorityUri, _testRequestContext).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ValidateAuthorityFalse_SkipsNetworkCall_Async()
        {
            // Arrange
            var validationException = new MsalServiceException(MsalError.InvalidInstance, "authority validation failed");

            // Inject authority in service bundle
            var httpManager = new MockHttpManager();
            var appConfig = new ApplicationConfiguration(MsalClientType.ConfidentialClient)
            {
                HttpManager = httpManager,
                Authority = Authority.CreateAuthority(TestAuthority, false)
            };

            var serviceBundle = ServiceBundle.Create(appConfig);

            RequestContext requestContext = new RequestContext(serviceBundle, Guid.NewGuid());

            // network fails with invalid_instance exception
            _networkMetadataProvider
                .When(x => x.GetMetadataAsync(Arg.Any<Uri>(), requestContext))
                .Do(_ => throw validationException);

            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryAsync(
                AuthorityInfo.FromAuthorityUri("https://some_env.com/tid", true),
                requestContext).ConfigureAwait(false);

            // Since the validateAuthority is set to false, proceed without alias. 
            ValidateSingleEntryMetadata(new Uri(TestAuthority), actualResult);
        }

        private async Task ValidateSelfEntryAsync(Uri authority, RequestContext requestContext = null)
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                InstanceDiscoveryMetadataEntry entry = await harness.ServiceBundle.InstanceDiscoveryManager
                    .GetMetadataEntryAsync(
                        AuthorityInfo.FromAuthorityUri(authority.AbsoluteUri, true),
                        requestContext ?? new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .ConfigureAwait(false);

                InstanceDiscoveryMetadataEntry entry2 = await harness.ServiceBundle.InstanceDiscoveryManager
                    .GetMetadataEntryTryAvoidNetworkAsync(
                        AuthorityInfo.FromAuthorityUri(authority.AbsoluteUri, true),
                        new[] { "some_env" },
                        requestContext ?? new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .ConfigureAwait(false);

                ValidateSingleEntryMetadata(authority, entry);
                ValidateSingleEntryMetadata(authority, entry2);
            }
        }

        private static void ValidateSingleEntryMetadata(Uri authority, InstanceDiscoveryMetadataEntry entry)
        {
            Assert.AreEqual(authority.Host, entry.PreferredCache);
            Assert.AreEqual(authority.Host, entry.PreferredNetwork);
            Assert.AreEqual(authority.Host, entry.Aliases.Single());
        }
    }
}
