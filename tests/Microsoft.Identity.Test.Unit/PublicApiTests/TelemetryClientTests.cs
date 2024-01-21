// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.Identity.Test.Unit.TelemetryTests;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\valid_cert.pfx")]
    public class TelemetryClientTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;
        private ConfidentialClientApplication _cca;
        private TestTelemetryClient _telemetryClient;

        [TestInitialize]
        public override void TestInitialize()
        {
            _telemetryClient = new TestTelemetryClient(TestConstants.ClientId);
            base.TestInitialize();
        }

        [TestCleanup] 
        public override void TestCleanup()
        {
            base.TestCleanup();
        }

        [TestMethod]
        public void TelemetryClientListNull()
        {
            var e = AssertException.Throws<ArgumentNullException>(() => ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithClientSecret("secret")
                .WithTelemetryClient(null)
                .Build());

            Assert.AreEqual("telemetryClients", e.ParamName);
        }

        [TestMethod]
        public void TelemetryClientNullClientInList()
        {
            var e = AssertException.Throws<ArgumentNullException>(() => ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithClientSecret("secret")
                .WithTelemetryClient(_telemetryClient, null)
                .Build());

            Assert.AreEqual("telemetryClient", e.ParamName);
        }

        [TestMethod]
        public void TelemetryClientNoArg()
        {
            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithClientSecret("secret")
                .WithTelemetryClient()
                .Build();

            Assert.IsNotNull(cca);
        }

        [TestMethod]
        public async Task AcquireTokenSuccessfulTelemetryTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                CreateApplication();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Acquire token for client with scope
                var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.None,
                    TestConstants.s_scope.AsSingleString(),
                    null);

                // Acquire token silently
                var account = (await _cca.GetAccountsAsync().ConfigureAwait(false)).Single();
                result = await _cca.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.Cache,
                    CacheRefreshReason.NotApplicable,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.L1Cache,
                    TestConstants.s_scope.AsSingleString(),
                    null);

                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Acquire token for client with resource
                result = await _cca.AcquireTokenForClient(new[] { TestConstants.DefaultGraphScope })
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.None,
                    null,
                    "https://graph.microsoft.com");
            }
        }

        [TestMethod]
        [DataRow(new[] { "https://graph.microsoft.com/.default" }, "https://graph.microsoft.com", ".default")]
        [DataRow(new[] { "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/Mail.Read" }, "https://graph.microsoft.com", "User.Read Mail.Read")]
        [DataRow(new[] { "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c/access_as_user" }, "api://23c64cd8-21e4-41dd-9756-ab9e2c23f58c", "access_as_user")]
        [DataRow(new[] { "User.Read", "Mail.Read" }, null, "User.Read Mail.Read")]
        [DataRow(new[] { "https://sharepoint.com/scope" }, "https://sharepoint.com", "scope")]
        [DataRow(new[] { "offline_access", "openid", "profile" }, null, "offline_access openid profile")]
        [DataRow(new[] { "https://graph.microsoft.com/.default", "User.Read" }, "https://graph.microsoft.com", ".default User.Read")]
        public async Task AcquireTokenSuccessfulTelemetryTestForScopesAsync(IEnumerable<string> input, string expectedResource, string expectedScope)
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                
                CreateApplication();
                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Acquire token for client with scope
                var result = await _cca.AcquireTokenForClient(input)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result, 
                    eventDetails, 
                    TokenSource.IdentityProvider, 
                    CacheRefreshReason.NoCachedAccessToken, 
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.None,
                    expectedScope,
                    expectedResource);
            }
        }

        [TestMethod]
        [DataRow(AssertionType.Secret)]
        [DataRow(AssertionType.CertificateWithSni)]
        [DataRow(AssertionType.CertificateWithoutSni)]
        [DataRow(AssertionType.ClientAssertion)]
        [DataRow(AssertionType.ManagedIdentity)]
        public async Task AcquireTokenAssertionTypeTelemetryTestAsync(int assertionType)
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                CreateApplication((AssertionType)assertionType);
                if (assertionType != 5)
                {
                    _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                }

                var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    (AssertionType)assertionType,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.None, 
                    TestConstants.s_scope.AsSingleString(), 
                    null);
            }
        }

        [TestMethod]
        public async Task AcquireTokenCacheTelemetryTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                //Create app
                CacheLevel cacheLevel = CacheLevel.L1Cache;
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();
                CreateApplication();

                _harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                //Configure cache
                _cca.AppTokenCache.SetBeforeAccess((args) =>
                {
                    args.TelemetryData.CacheLevel = cacheLevel;
                });

                _cca.AppTokenCache.SetAfterAccess((args) =>
                {
                    args.TelemetryData.CacheLevel = cacheLevel;
                });

                //Acquire Token
                var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;

                //Validate telemetry
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.None, 
                    TestConstants.s_scope.AsSingleString(), 
                    null);

                //Update cache type
                cacheLevel = CacheLevel.L1Cache;

                //Acquire Token
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;

                //Validate telemetry
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.Cache,
                    CacheRefreshReason.NotApplicable,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.L1Cache,
                    TestConstants.s_scope.AsSingleString(),
                    null);

                //Update cache type again
                cacheLevel = CacheLevel.L2Cache;

                //Acquire Token
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;

                //Validate telemetry
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.Cache,
                    CacheRefreshReason.NotApplicable,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.L2Cache,
                    TestConstants.s_scope.AsSingleString(),
                    null);

                //Simulate the cache not providing a value
                cacheLevel = CacheLevel.None;

                //Acquire Token
                result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;

                //Validate telemetry
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.Cache,
                    CacheRefreshReason.NotApplicable,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant,
                    TokenType.Bearer,
                    CacheLevel.Unknown,
                    TestConstants.s_scope.AsSingleString(),
                    null);
            }
        }

        [TestMethod]
        public async Task AcquireTokenWithMSITelemetryTestAsync()
        {
            using (new EnvVariableContext())
            using (_harness = CreateTestHarness())
            {
                string endpoint = "http://localhost:40342/metadata/identity/oauth2/token";
                string resource = "https://management.azure.com";

                Environment.SetEnvironmentVariable("MSI_ENDPOINT", endpoint);

                var mia = ManagedIdentityApplicationBuilder
                    .Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures()
                    .WithHttpManager(_harness.HttpManager)
                    .WithTelemetryClient(_telemetryClient)
                    .Build();

                _harness.HttpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySource.CloudShell);

                var result = await mia.AcquireTokenForManagedIdentity(resource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    AssertionType.ManagedIdentity,
                    "https://login.microsoftonline.com/managed_identity/",
                    TokenType.Bearer,
                    CacheLevel.None,
                    null,
                    resource);
            }
        }

        [TestMethod]
        public async Task AcquireTokenUnSuccessfulTelemetryTestAsync()
        {
            using (_harness = CreateTestHarness())
            {
                _harness.HttpManager.AddInstanceDiscoveryMockHandler();

                CreateApplication();
                _harness.HttpManager.AddTokenResponse(TokenResponseType.InvalidClient);

                //Test for MsalServiceException
                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.IsNotNull(ex.ErrorCode);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                Assert.AreEqual(ex.ErrorCode, eventDetails.Properties[TelemetryConstants.ErrorCode]);
                Assert.AreEqual(ex.Message, eventDetails.Properties[TelemetryConstants.ErrorMessage]);
                Assert.AreEqual(ex.ErrorCodes.AsSingleString(), eventDetails.Properties[TelemetryConstants.StsErrorCode]);
                Assert.IsFalse((bool?)eventDetails.Properties[TelemetryConstants.Succeeded]);

                //Test for MsalClientException
                MsalClientException exClient = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => _cca.AcquireTokenForClient(null) // null scope -> client exception
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                Assert.IsNotNull(exClient);
                Assert.IsNotNull(exClient.ErrorCode);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;
                Assert.AreEqual(exClient.ErrorCode, eventDetails.Properties[TelemetryConstants.ErrorCode]);
                Assert.AreEqual(exClient.Message, eventDetails.Properties[TelemetryConstants.ErrorMessage]);
                Assert.IsFalse((bool?)eventDetails.Properties[TelemetryConstants.Succeeded]);
            }
        }

        [TestMethod]
        public async Task AcquireTokenGenericErrorTelemetryTestAsync()
        {
            IMsalHttpClientFactory factoryThatThrows = Substitute.For<IMsalHttpClientFactory>();
            factoryThatThrows.When(x => x.GetHttpClient()).Do(_ => { throw new SocketException(0); });

            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpClientFactory(factoryThatThrows)
                .WithExperimentalFeatures()
                .WithTelemetryClient(_telemetryClient)
                .BuildConcrete();

            MsalClientException exClient = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => cca.AcquireTokenForClient(null)
                .WithTenantId(TestConstants.Utid)
                .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

            Assert.IsNotNull(exClient);
            Assert.IsNotNull(exClient.ErrorCode);

            var eventDetails = _telemetryClient.TestTelemetryEventDetails;
            Assert.AreEqual(exClient.ErrorCode, eventDetails.Properties[TelemetryConstants.ErrorCode]);
            Assert.AreEqual(exClient.Message, eventDetails.Properties[TelemetryConstants.ErrorMessage]);
            Assert.IsFalse((bool?)eventDetails.Properties[TelemetryConstants.Succeeded]);
        }

        private void AssertLoggedTelemetry(
                        AuthenticationResult authenticationResult,
                        MsalTelemetryEventDetails eventDetails,
                        TokenSource tokenSource,
                        CacheRefreshReason cacheRefreshReason,
                        AssertionType assertionType,
                        string endpoint,
                        TokenType? tokenType,
                        CacheLevel cacheLevel,
                        string scopes,
                        string resource)
        {
            Assert.IsNotNull(eventDetails);
            Assert.AreEqual(Convert.ToInt64(cacheRefreshReason), eventDetails.Properties[TelemetryConstants.CacheInfoTelemetry]);
            Assert.AreEqual(Convert.ToInt64(tokenSource), eventDetails.Properties[TelemetryConstants.TokenSource]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs, eventDetails.Properties[TelemetryConstants.Duration]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs, eventDetails.Properties[TelemetryConstants.DurationInHttp]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs, eventDetails.Properties[TelemetryConstants.DurationInCache]);
            Assert.AreEqual(Convert.ToInt64(assertionType), eventDetails.Properties[TelemetryConstants.AssertionType]);
            Assert.AreEqual(Convert.ToInt64(tokenType), eventDetails.Properties[TelemetryConstants.TokenType]);
            Assert.AreEqual(endpoint, eventDetails.Properties[TelemetryConstants.Endpoint]);
            Assert.AreEqual(Convert.ToInt64(cacheLevel), eventDetails.Properties[TelemetryConstants.CacheLevel]);
            Assert.AreEqual(cacheLevel, authenticationResult.AuthenticationResultMetadata.CacheLevel);

            if (!string.IsNullOrWhiteSpace(scopes))
            {
                Assert.AreEqual(scopes, eventDetails.Properties[TelemetryConstants.Scopes]);
            }

            if (!string.IsNullOrWhiteSpace(resource))
            {
                Assert.AreEqual(resource, eventDetails.Properties[TelemetryConstants.Resource]);
            }

        }

        private void CreateApplication(AssertionType assertionType = AssertionType.Secret)
        {
            switch (assertionType)
            {
                case AssertionType.Secret:
                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithClientSecret(TestConstants.ClientSecret)
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                        break;
                case AssertionType.CertificateWithoutSni:
                    var certificate1 = CertHelper.GetOrCreateTestCert();

                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithCertificate(certificate1)
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                    break;
                case AssertionType.CertificateWithSni:
                    var certificate2 = CertHelper.GetOrCreateTestCert();

                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithCertificate(certificate2, true)
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                    break;
                case AssertionType.ClientAssertion:
                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithClientAssertion(TestConstants.DefaultClientAssertion)
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                    break;
                case AssertionType.ManagedIdentity:
                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAppTokenProvider((AppTokenProviderParameters _) => { return Task.FromResult(GetAppTokenProviderResult()); })
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                    break;
            }

            TokenCacheHelper.PopulateCache(_cca.UserTokenCacheInternal.Accessor);
        }

        private AppTokenProviderResult GetAppTokenProviderResult(string differentScopesForAt = "", long? refreshIn = 1000)
        {
            var token = new AppTokenProviderResult();
            token.AccessToken = TestConstants.DefaultAccessToken + differentScopesForAt; //Used to indicate that there is a new access token for a different set of scopes
            token.ExpiresInSeconds = 3600;
            token.RefreshInSeconds = refreshIn;

            return token;
        }
    }
}
