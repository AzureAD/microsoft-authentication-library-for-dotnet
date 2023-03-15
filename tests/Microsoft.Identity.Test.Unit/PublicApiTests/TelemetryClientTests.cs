// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TelemetryClientTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;
        private ConfidentialClientApplication _cca;
        private TelemetryClient _telemetryClient;

        [TestInitialize]
        public override void TestInitialize()
        {
            _telemetryClient = new TelemetryClient(TestConstants.ClientId);
            base.TestInitialize();
        }

        [TestCleanup] 
        public override void TestCleanup()
        {
            base.TestCleanup();
        }

        [TestMethod]
        public void TelemetryClientExperimental()
        {
            var e = AssertException.Throws<MsalClientException>(() => ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret("secret")
                .WithTelemetryClient(_telemetryClient)
                .Build());

            Assert.AreEqual(MsalError.ExperimentalFeature, e.ErrorCode);
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

                // Acquire token interactively
                var result = await _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result, 
                    eventDetails, 
                    TokenSource.IdentityProvider, 
                    CacheRefreshReason.NoCachedAccessToken, 
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant);

                // Acquire token silently
                var account = (await _cca.GetAccountsAsync().ConfigureAwait(false)).Single();
                result = await _cca.AcquireTokenSilent(TestConstants.s_scope, account)
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result);

                eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result, 
                    eventDetails, 
                    TokenSource.Cache, 
                    CacheRefreshReason.NotApplicable,
                    AssertionType.Secret,
                    TestConstants.AuthorityUtidTenant);
            }
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        public async Task AcquireTokenCertificateTelemetryTestAsync(int assertionType)
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
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    (AssertionType)assertionType,
                    TestConstants.AuthorityUtidTenant);
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

                var scope = "https://management.azure.com";
                Environment.SetEnvironmentVariable("MSI_ENDPOINT", endpoint);

                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(_harness.HttpManager)
                    .WithExperimentalFeatures()
                    .WithTelemetryClient(_telemetryClient)
                    .Build();

                _harness.HttpManager.AddManagedIdentityMockHandler(
                    endpoint,
                    resource,
                    MockHelpers.GetMsiSuccessfulResponse(),
                    ManagedIdentitySourceType.CloudShell);

                var result = await cca.AcquireTokenForClient(new string[] { scope })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                AssertLoggedTelemetry(
                    result,
                    eventDetails,
                    TokenSource.IdentityProvider,
                    CacheRefreshReason.NoCachedAccessToken,
                    AssertionType.MSI,
                    TestConstants.AuthorityCommonTenant);
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

                MsalServiceException ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.IsNotNull(ex.ErrorCode);

                MsalTelemetryEventDetails eventDetails = _telemetryClient.TestTelemetryEventDetails;
                Assert.AreEqual(ex.ErrorCode, eventDetails.Properties[TelemetryConstants.ErrorCode]);
                Assert.IsFalse((bool?)eventDetails.Properties[TelemetryConstants.Succeeded]);
            }
        }

        private void AssertLoggedTelemetry(
                        AuthenticationResult authenticationResult, 
                        MsalTelemetryEventDetails eventDetails, 
                        TokenSource tokenSource, 
                        CacheRefreshReason cacheRefreshReason,
                        AssertionType assertionType,
                        string endpoint,
                        TokenType? tokenType = TokenType.Bearer,
                        CacheTypeUsed? cacheTypeUsed = null)
        {
            Assert.IsNotNull(eventDetails);
            Assert.AreEqual(Convert.ToInt64(cacheRefreshReason), eventDetails.Properties[TelemetryConstants.CacheInfoTelemetry]);
            Assert.AreEqual(Convert.ToInt64(tokenSource), eventDetails.Properties[TelemetryConstants.TokenSource]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs, eventDetails.Properties[TelemetryConstants.Duration]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationInHttpInMs, eventDetails.Properties[TelemetryConstants.DurationInHttp]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationInCacheInMs, eventDetails.Properties[TelemetryConstants.DurationInCache]);
            Assert.AreEqual(authenticationResult.AuthenticationResultMetadata.DurationTotalInMs, eventDetails.Properties[TelemetryConstants.Duration]);
            Assert.AreEqual(Convert.ToInt64(assertionType), eventDetails.Properties[TelemetryConstants.AssertionType]);
            Assert.AreEqual(Convert.ToInt64(tokenType), eventDetails.Properties[TelemetryConstants.TokenType]);
            Assert.AreEqual(endpoint, eventDetails.Properties[TelemetryConstants.Endpoint]);

            if (eventDetails.Properties.ContainsKey(TelemetryConstants.CacheUsed))
            {
                Assert.AreEqual(cacheTypeUsed, eventDetails.Properties[TelemetryConstants.CacheUsed]);
            }
        }

        private void CreateApplication(AssertionType assertionType = AssertionType.Secret)
        {
            var certificate = new X509Certificate2(
                                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                                    TestConstants.DefaultPassword);
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
                case AssertionType.CertificateWithoutSNI:
                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithCertificate(certificate)
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                    break;
                case AssertionType.CertificateWithSNI:
                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithCertificate(certificate, true)
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
                case AssertionType.MSI:
                    _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAppTokenProvider((AppTokenProviderParameters parameters) => { return Task.FromResult(GetAppTokenProviderResult()); })
                        .WithHttpManager(_harness.HttpManager)
                        .WithExperimentalFeatures()
                        .WithTelemetryClient(_telemetryClient)
                        .BuildConcrete();
                    break;
            }


            TokenCacheHelper.PopulateCache(_cca.UserTokenCacheInternal.Accessor);
        }
    }

    internal class TelemetryClient : ITelemetryClient
    {
        public MsalTelemetryEventDetails TestTelemetryEventDetails { get; set; }
        
        public TelemetryClient(string clientId)
        {
            ClientId = clientId;
        }

        public string ClientId { get; set; }

        public void Initialize()
        {

        }

        public bool IsEnabled()
        {
            return true;
        }

        public bool IsEnabled(string eventName)
        {
            return TelemetryConstants.AcquireTokenEventName.Equals(eventName);
        }

        public void TrackEvent(TelemetryEventDetails eventDetails)
        {
            TestTelemetryEventDetails = (MsalTelemetryEventDetails) eventDetails;
        }

        public void TrackEvent(string eventName, IDictionary<string, string> stringProperties = null, IDictionary<string, long> longProperties = null, IDictionary<string, bool> boolProperties = null, IDictionary<string, DateTime> dateTimeProperties = null, IDictionary<string, double> doubleProperties = null, IDictionary<string, Guid> guidProperties = null)
        {
            throw new NotImplementedException();
        }
    }
}
