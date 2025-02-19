// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Integration.NetCore.HeadlessTests
{
    [TestClass]
    public class FmiIntegrationTests
    {
        private const string _fmiAppUrn = "urn:microsoft:identity:fmi";
        private const string _fmiClientId = "4df2cbbb-8612-49c1-87c8-f334d6d065ad";
        private const string _fmiTenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
        private const string _fmiAuthority = "https://login.microsoftonline.com/" + _fmiTenantId;
        private const string _fmiScope1 = "api://AzureFMITokenExchange/.default";
        private const string _fmiScope2 = "022907d3-0f1b-48f7-badc-1ba6abab6d66/.default";
        private const string _fmiScope3 = "api://AzureADTokenExchange/.default";
        private const string _fmiPath = "SomeFmiPath/fmi";

        [TestMethod]
        public async Task RmaFmiCredFlow1Async()
        {
            await RunRmaFlow(_fmiClientId, _fmiAuthority, _fmiScope1).ConfigureAwait(false);
        }

        public async Task RmaFmiCredFlow2Async()
        {
            await RunRmaFlow(_fmiClientId, _fmiAuthority, _fmiScope2).ConfigureAwait(false);
        }

        public async Task FmiCredFlow2Async()
        {
            var fmiCredential = await RunRmaFlow(_fmiClientId, _fmiAuthority, _fmiScope2).ConfigureAwait(false);
            await RunFicFlow(fmiCredential).ConfigureAwait(false);
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

        private async Task RunFicFlow(string fmiCredential)
        {
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create(_fmiAppUrn)
                        .WithAuthority(_fmiAuthority, true)
                        .WithExtraQueryParameters("dc=ESTS-PUB-SCUS-LZ1-FD000-TEST1")
                        .WithExperimentalFeatures(true)
                        .WithClientAssertion((options) =>
                            {
                                Assert.AreEqual(_fmiAppUrn, options.ClientID);
                                return Task.FromResult(fmiCredential);
                            })
                        .BuildConcrete();

            var appCacheRecorder = confidentialApp.AppTokenCache.RecordAccess();

            var authResult = await confidentialApp.AcquireTokenForClient(new[] { _fmiScope3 })
                                                    .WithFmiPath(_fmiPath)
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            MsalAssert.AssertAuthResult(authResult);
            appCacheRecorder.AssertAccessCounts(1, 1);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.IsApplicationCache);
            Assert.IsTrue(appCacheRecorder.LastAfterAccessNotificationArgs.HasTokens);
            CollectionAssert.AreEquivalent(new[] { _fmiScope3 }, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestScopes.ToArray());
            CollectionAssert.AreEquivalent(new[] { _fmiScope3 }, appCacheRecorder.LastAfterAccessNotificationArgs.RequestScopes.ToArray());
            Assert.AreEqual(_fmiTenantId, appCacheRecorder.LastBeforeAccessNotificationArgs.RequestTenantId ?? "");
            Assert.AreEqual(_fmiTenantId, appCacheRecorder.LastAfterAccessNotificationArgs.RequestTenantId ?? "");
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationTotalInMs > 0);
            Assert.IsTrue(authResult.AuthenticationResultMetadata.DurationInHttpInMs > 0);
        }
    }
}
