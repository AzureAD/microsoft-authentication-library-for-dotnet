// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.Integration.NetCore.HeadlessTests
{
    [TestClass]
    public class FmiIntegrationTests
    {
        private const string _fmiAppUrn = "urn:microsoft:identity:fmi";
        private const string _fmiRmaClientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private const string _fmiNonRmaClientId = "";
        private const string _fmiNonRmaClientIdInpersonation = "";
        private const string _fmiTenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
        private const string _fmiAuthority = "https://login.microsoftonline.com/" + _fmiTenantId;
        private const string _fmiExchangeScope = "api://AzureFMITokenExchange/.default";
        private const string _fmiExchangeScopeAsGuid = "022907d3-0f1b-48f7-badc-1ba6abab6d66/.default";
        private const string _fmiAadExchangeScope = "api://AzureADTokenExchange/.default";
        private const string _fmiAadExchangeScopeAsGuid = "api://d796a5d2-0fb6-499a-b311-8bf5b3d058f7";
        private const string _fmiPath = "SomeFmiPath/fmi";

        [TestMethod]
        public async Task RmaFmiCredFlowAsync()
        {
            await RunRmaFlow(_fmiRmaClientId, _fmiAuthority, _fmiExchangeScope).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RmaFmiCredFlowGuidAsync()
        {
            await RunRmaFlow(_fmiRmaClientId, _fmiAuthority, _fmiExchangeScopeAsGuid).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FmiCredFlow2Async()
        {
            var fmiCredential = await RunRmaFlow(_fmiRmaClientId, _fmiAuthority, _fmiExchangeScope).ConfigureAwait(false);
            await RunFicFlow(fmiCredential, _fmiExchangeScope, _fmiAppUrn).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FmiCredFlowAadExchangeAsync()
        {
            var fmiCredential = await RunRmaFlow(_fmiRmaClientId, _fmiAuthority, _fmiExchangeScope).ConfigureAwait(false);
            await RunFicFlow(fmiCredential, _fmiAadExchangeScope, _fmiAppUrn).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FmiCredFlowAadExchangeGuidAsync()
        {
            var fmiCredential = await RunRmaFlow(_fmiRmaClientId, _fmiAuthority, _fmiAadExchangeScopeAsGuid).ConfigureAwait(false);
            await RunFicFlow(fmiCredential, _fmiAadExchangeScope, _fmiAppUrn).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NonRmaFmiCredFlowAsync()
        {
            await RunRmaFlow(_fmiNonRmaClientId, _fmiAuthority, _fmiAadExchangeScope).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task NonRmaFmiCredFlowAadExchangeAsync()
        {
            var fmiCredential = await RunRmaFlow(_fmiRmaClientId, _fmiAuthority, _fmiExchangeScope).ConfigureAwait(false);
            await RunFicFlow(fmiCredential, _fmiExchangeScope, _fmiNonRmaClientIdInpersonation).ConfigureAwait(false);
        }

        private async Task<string> RunRmaFlow(string clientId, string authority, string scope)
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(authority, true)
                        .WithExtraQueryParameters("dc=ESTS-PUB-SCUS-LZ1-FD000-TEST1")
                        .WithExperimentalFeatures(true)
                        .WithCertificate(cert, sendX5C: true)
                        .BuildConcrete();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath(_fmiPath)
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            CollectionAssert.AreEquivalent(new[] { scope }, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestScopes.ToArray());
            CollectionAssert.AreEquivalent(new[] { scope }, appCacheRecorder.LastAfterAccessNotificationArgs.RequestScopes.ToArray());
            Assert.AreEqual(_fmiTenantId, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestTenantId ?? "");
            Assert.AreEqual(_fmiTenantId, appCacheRecorder.LastAfterAccessNotificationArgs.RequestTenantId ?? "");
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs > 0);

            return authResult.AccessToken;
        }

        private async Task RunFicFlow(string fmiCredential, string scope, string clientId)
        {
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(_fmiAuthority, true)
                        .WithExtraQueryParameters("dc=ESTS-PUB-SCUS-LZ1-FD000-TEST1")
                        .WithExperimentalFeatures(true)
                        .WithClientAssertion((options) =>
                            {
                                Assert.AreEqual(clientId, options.ClientID);
                                return Task.FromResult(fmiCredential);
                            })
                        .BuildConcrete();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath(_fmiPath)
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            CollectionAssert.AreEquivalent(new[] { scope }, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestScopes.ToArray());
            CollectionAssert.AreEquivalent(new[] { scope }, appCacheRecorder.LastAfterAccessNotificationArgs.RequestScopes.ToArray());
            Assert.AreEqual(_fmiTenantId, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestTenantId ?? "");
            Assert.AreEqual(_fmiTenantId, appCacheRecorder.LastAfterAccessNotificationArgs.RequestTenantId ?? "");
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs > 0);
        }
    }
}
