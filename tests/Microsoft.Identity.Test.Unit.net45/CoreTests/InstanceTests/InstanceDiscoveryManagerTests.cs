// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class InstanceDiscoveryManagerTests : TestBase
    {
        private const string Authority = "https://some_env.com/tid";
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

            _expectedResult = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "some_env.com", "some_env2.com" },
                PreferredCache = "env",
                PreferredNetwork = "env"
            };

            _harness = base.CreateTestHarness();
            _testRequestContext = new RequestContext(_harness.ServiceBundle, Guid.NewGuid());
            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                _harness.ServiceBundle.TelemetryManager,
                false,
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
            await ValidateSelfEntryAsync(new Uri(MsalTestConstants.B2CAuthority))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ADFS_GetMetadataAsync()
        {
            await ValidateSelfEntryAsync(new Uri(MsalTestConstants.ADFSAuthority))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NetworkCacheProvider_IsUsedFirst_Async()
        {
            // Arrange
            INetworkMetadataProvider networkMetadataProvider = new NetworkMetadataProvider(
                Substitute.For<IHttpManager>(), Substitute.For<ITelemetryManager>(), _networkCacheMetadataProvider);

            _networkCacheMetadataProvider.GetMetadata("some_env.com", Arg.Any<ICoreLogger>()).Returns(_expectedResult);

            _discoveryManager = new InstanceDiscoveryManager(
              _harness.HttpManager,
              _harness.ServiceBundle.TelemetryManager,
              false,
              null,
              _knownMetadataProvider,
              _networkCacheMetadataProvider,
              networkMetadataProvider);

            // Act
            InstanceDiscoveryMetadataEntry actualResult1 = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                Authority,
                new[] { "env1", "env2" },
                _testRequestContext)
                .ConfigureAwait(false);
            _networkCacheMetadataProvider.Received(1).GetMetadata("some_env.com", Arg.Any<ICoreLogger>());

            InstanceDiscoveryMetadataEntry actualResult2 = await _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext)
                .ConfigureAwait(false);
            _networkCacheMetadataProvider.Received(2).GetMetadata("some_env.com", Arg.Any<ICoreLogger>());
            _networkCacheMetadataProvider.AddMetadata(null, null);

            // Assert
            Assert.AreSame(_expectedResult, actualResult1, "The static provider should be queried first");
            Assert.AreSame(_expectedResult, actualResult2, "The static provider should be queried first");
        }

        [TestMethod]
        public async Task KnownMetadataProviderIsCheckedSecondAsync()
        {
            // Arrange
            var otherEnvs = new[] { "env1", "env2" };

            // No response from the static provider
            _networkCacheMetadataProvider.GetMetadata("some_env.com", Arg.Any<ICoreLogger>()).Returns((InstanceDiscoveryMetadataEntry)null);

            _knownMetadataProvider.GetMetadata("some_env.com", otherEnvs, Arg.Any<ICoreLogger>()).Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                "https://some_env.com/tid",
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _networkCacheMetadataProvider.Received(1).GetMetadata("some_env.com", Arg.Any<ICoreLogger>());
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs, Arg.Any<ICoreLogger>());
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
                .Do(x => throw validationException);
          

            // Act
            var actualException = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
            _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext))
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(validationException, actualException);
            _knownMetadataProvider.DidNotReceiveWithAnyArgs();
        }

        [TestMethod]
        public async Task NetworkProviderFailures_AreIgnored_Async()
        {
            // Arrange
            _networkCacheMetadataProvider = new NetworkCacheMetadataProvider();
            _knownMetadataProvider.GetMetadata("some_env.com", Enumerable.Empty<string>(), Arg.Any<ICoreLogger>()).Returns(_expectedResult);

            // network fails with something other than invalid_instance exception
            _networkMetadataProvider
                .When(x => x.GetMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(x => throw new MsalServiceException("endpoint_busy", "some exception message"));


            // Act
            var actualResult = await _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", Enumerable.Empty<string>(), Arg.Any<ICoreLogger>());
        }

        [TestMethod]
        public async Task NetworkProviderFailures_WithNoKnownMetadata_ContinuesWithAuthority_Async()
        {
            // Arrange
            _networkCacheMetadataProvider = new NetworkCacheMetadataProvider();

            // no known metadata 
            _knownMetadataProvider.GetMetadata(null, null, Arg.Any<ICoreLogger>()).ReturnsForAnyArgs((InstanceDiscoveryMetadataEntry)null);

            // network fails with something other than invalid_instance exception
            _networkMetadataProvider
                .When(x => x.GetMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(x => throw new MsalServiceException("endpoint_busy", "some exception message"));

            // Act
            var actualResult = await _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", Enumerable.Empty<string>(), Arg.Any<ICoreLogger>());
            ValidateSingleEntryMetadata(new Uri("https://some_env.com/tid"), actualResult);
        }

        [TestMethod]
        public async Task NetworkProviderIsCalledLastAsync()
        {
            // Arrange
            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                _harness.ServiceBundle.TelemetryManager,
                false,
                null,
                _knownMetadataProvider,
                _networkCacheMetadataProvider,
                _networkMetadataProvider);

            var otherEnvs = new[] { "env1", "env2" };
            var authorityUri = new Uri(Authority);

            // No response from the static and known provider
            _knownMetadataProvider
                .GetMetadata("some_env.com", otherEnvs, Arg.Any<ICoreLogger>())
                .Returns((InstanceDiscoveryMetadataEntry)null);

            _networkMetadataProvider
                .GetMetadataAsync(authorityUri, _testRequestContext)
                .Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                "https://some_env.com/tid",
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs, Arg.Any<ICoreLogger>());
            await _networkMetadataProvider.Received(1).GetMetadataAsync(authorityUri, _testRequestContext).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UserProvider_TakesPrecedence_OverNetworkProvider_Async()
        {
            // Arrange
            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                _harness.ServiceBundle.TelemetryManager,
                false,
                _userMetadataProvider,
                _knownMetadataProvider,
                _networkCacheMetadataProvider,
                _networkMetadataProvider);

            var otherEnvs = new[] { "env1", "env2" };
            _userMetadataProvider.GetMetadataOrThrow("some_env.com", Arg.Any<ICoreLogger>()).Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                "https://some_env.com/tid",
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            InstanceDiscoveryMetadataEntry actualResult2 = await _discoveryManager.GetMetadataEntryAsync(
               "https://some_env.com/tid",
               _testRequestContext)
               .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The user metadata provider should be queried second");
            Assert.AreSame(_expectedResult, actualResult2, "The user metadata provider should be queried second");
            _userMetadataProvider.Received(2).GetMetadataOrThrow("some_env.com", Arg.Any<ICoreLogger>());
            _knownMetadataProvider.DidNotReceiveWithAnyArgs().GetMetadata(null, null, null);
            await _networkMetadataProvider.DidNotReceiveWithAnyArgs().GetMetadataAsync(
                Arg.Any<Uri>(),
                Arg.Any<RequestContext>()).ConfigureAwait(false);
        }

        private async Task ValidateSelfEntryAsync(Uri authority)
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                InstanceDiscoveryMetadataEntry entry = await harness.ServiceBundle.InstanceDiscoveryManager
                    .GetMetadataEntryAsync(
                        authority.AbsoluteUri,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .ConfigureAwait(false);

                InstanceDiscoveryMetadataEntry entry2 = await harness.ServiceBundle.InstanceDiscoveryManager
                    .GetMetadataEntryTryAvoidNetworkAsync(
                        authority.AbsoluteUri,
                        new[] { "some_env" },
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
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
