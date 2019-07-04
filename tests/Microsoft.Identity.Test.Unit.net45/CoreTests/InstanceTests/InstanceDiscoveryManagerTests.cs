// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
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
        private IStaticMetadataProvider _staticMetadataProvider;
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
            _staticMetadataProvider = Substitute.For<IStaticMetadataProvider>();
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
                _knownMetadataProvider,
                _staticMetadataProvider,
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
        public async Task StaticProviderIsUsedFirst_Async()
        {
            // Arrange
            _staticMetadataProvider.GetMetadata("some_env.com").Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult1 = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                Authority,
                new[] { "env1", "env2" },
                _testRequestContext)
                .ConfigureAwait(false);
            _staticMetadataProvider.Received(1).GetMetadata("some_env.com");

            InstanceDiscoveryMetadataEntry actualResult2 = await _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext)
                .ConfigureAwait(false);
            _staticMetadataProvider.Received(2).GetMetadata("some_env.com");
            _staticMetadataProvider.AddMetadata(null, null);

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
            _staticMetadataProvider.GetMetadata("some_env.com").Returns((InstanceDiscoveryMetadataEntry)null);

            _knownMetadataProvider.GetMetadata("some_env.com", otherEnvs).Returns(_expectedResult);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                "https://some_env.com/tid",
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _staticMetadataProvider.Received(1).GetMetadata("some_env.com");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs);
        }

        [TestMethod]
        public async Task AuthorityValidationFailure_IsRethrown_Async()
        {
            // Arrange
            var validationException = new MsalServiceException(MsalError.InvalidInstance, "authority validation failed"); 
            _staticMetadataProvider = new StaticMetadataProvider();

            // network fails with invalid_instance exception
            _networkMetadataProvider
                .When(x => x.FetchAllDiscoveryMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
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
            _staticMetadataProvider = new StaticMetadataProvider();
            _knownMetadataProvider.GetMetadata("some_env.com", Enumerable.Empty<string>()).Returns(_expectedResult);

            // network fails with something other than invalid_instance exception
            _networkMetadataProvider
                .When(x => x.FetchAllDiscoveryMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(x => throw new MsalServiceException("endpoint_busy", "some exception message"));


            // Act
            var actualResult = await _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", Enumerable.Empty<string>());
        }

        [TestMethod]
        public async Task NetworkProviderFailures_WithNoKnownMetadata_ContinuesWithAuthority_Async()
        {
            // Arrange
            _staticMetadataProvider = new StaticMetadataProvider();

            // no known metadata 
            _knownMetadataProvider.GetMetadata(null, null).ReturnsForAnyArgs((InstanceDiscoveryMetadataEntry)null);

            // network fails with something other than invalid_instance exception
            _networkMetadataProvider
                .When(x => x.FetchAllDiscoveryMetadataAsync(Arg.Any<Uri>(), _testRequestContext))
                .Do(x => throw new MsalServiceException("endpoint_busy", "some exception message"));

            // Act
            var actualResult = await _discoveryManager.GetMetadataEntryAsync(
                "https://some_env.com/tid",
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", Enumerable.Empty<string>());
            ValidateSingleEntryMetadata(new Uri("https://some_env.com/tid"), actualResult);
        }

        [TestMethod]
        public async Task NetworkProviderIsCalledLastAsync()
        {
            // Arrange
            _staticMetadataProvider = new StaticMetadataProvider();

            _discoveryManager = new InstanceDiscoveryManager(
                _harness.HttpManager,
                _harness.ServiceBundle.TelemetryManager,
                false,
                _knownMetadataProvider,
                _staticMetadataProvider,
                _networkMetadataProvider);

            var otherEnvs = new[] { "env1", "env2" };
            InstanceDiscoveryResponse discoveryResponse = new InstanceDiscoveryResponse
            {
                Metadata = new[] { _expectedResult }
            };
            var authorityUri = new Uri(Authority);

            // No response from the static and known provider
            _knownMetadataProvider
                .GetMetadata("some_env.com", otherEnvs)
                .Returns((InstanceDiscoveryMetadataEntry)null);
            _networkMetadataProvider
                .FetchAllDiscoveryMetadataAsync(authorityUri, _testRequestContext)
                .Returns(discoveryResponse);

            // Act
            InstanceDiscoveryMetadataEntry actualResult = await _discoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                "https://some_env.com/tid",
                otherEnvs,
                _testRequestContext)
                .ConfigureAwait(false);

            // Assert
            Assert.AreSame(_expectedResult, actualResult, "The known metadata provider should be queried second");
            _knownMetadataProvider.Received(1).GetMetadata("some_env.com", otherEnvs);
            _ = _networkMetadataProvider.Received(1).FetchAllDiscoveryMetadataAsync(authorityUri, _testRequestContext);
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
