// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// Integration tests for the User Federated Identity Credential (UserFIC) flow.
    /// These tests require the LabAuth.MSIDLab.com certificate to be installed locally
    /// (available to Microsoft employees only).
    /// </summary>
    [TestClass]
    public class UserFicIntegrationTests
    {
        // Fixed test values as specified in the feature request
        private const string ClientId = "979a25aa-0daf-41a5-bcad-cebec5c7c254";
        private const string Authority = "https://login.microsoftonline.com/msidlabtse4.onmicrosoft.com";
        private const string Username = "ficuser@msidlabtse4.onmicrosoft.com";
        private const string TokenExchangeScope = "api://AzureADTokenExchange/.default";
        private static readonly string[] s_scopes = new[] { "https://graph.microsoft.com/.default" };

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        /// <summary>
        /// Tests that AcquireTokenByUserFederatedIdentityCredential returns a token from IdentityProvider
        /// and that the assertion callback is invoked.
        /// </summary>
        [RunOn(TargetFrameworks.NetCore)]
        public async Task UserFic_AcquireToken_ReturnsTokenFromIdentityProviderAsync()
        {
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(cert, "Test setup error - cannot find LabAuth certificate. This test requires Microsoft employee access.");

            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(Authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            int callbackInvocations = 0;

            var result = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(
                    s_scopes,
                    Username,
                    async () =>
                    {
                        callbackInvocations++;
                        // Use the same app to acquire an assertion token via client credentials
                        var assertionResult = await app
                            .AcquireTokenForClient(new[] { TokenExchangeScope })
                            .ExecuteAsync()
                            .ConfigureAwait(false);
                        return assertionResult.AccessToken;
                    })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, callbackInvocations, "Callback must be invoked exactly once for the first network call.");
            Assert.IsNotNull(result.Account);
            Assert.AreEqual(Username, result.Account.Username, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that after an initial UserFIC call, AcquireTokenSilent hits the cache
        /// without invoking the assertion callback.
        /// </summary>
        [RunOn(TargetFrameworks.NetCore)]
        public async Task UserFic_SubsequentSilentCall_HitsCacheAndDoesNotInvokeCallbackAsync()
        {
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(cert, "Test setup error - cannot find LabAuth certificate. This test requires Microsoft employee access.");

            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(Authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            int callbackInvocations = 0;
            Func<Task<string>> assertionCallback = async () =>
            {
                callbackInvocations++;
                var assertionResult = await app
                    .AcquireTokenForClient(new[] { TokenExchangeScope })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                return assertionResult.AccessToken;
            };

            // First call: acquires from IdentityProvider and caches the result
            var firstResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, Username, assertionCallback)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, firstResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, callbackInvocations);

            // Subsequent silent call: should hit cache without invoking callback
            IAccount account = await app.GetAccountAsync(firstResult.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(account, "Account must be in cache after first UserFIC call.");

            int callbackCountBeforeSilent = callbackInvocations;
            var silentResult = await app
                .AcquireTokenSilent(s_scopes, account)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.Cache, silentResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(callbackCountBeforeSilent, callbackInvocations, "Assertion callback must NOT be invoked on a cache hit.");
        }

        /// <summary>
        /// Tests that WithForceRefresh(true) bypasses the cache and invokes the assertion callback again.
        /// </summary>
        [RunOn(TargetFrameworks.NetCore)]
        public async Task UserFic_WithForceRefresh_InvokesCallbackAndReturnsFromIdentityProviderAsync()
        {
            var cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);
            Assert.IsNotNull(cert, "Test setup error - cannot find LabAuth certificate. This test requires Microsoft employee access.");

            var app = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(Authority)
                .WithCertificate(cert, sendX5C: true)
                .WithTestLogging()
                .Build();

            int callbackInvocations = 0;
            Func<Task<string>> assertionCallback = async () =>
            {
                callbackInvocations++;
                var assertionResult = await app
                    .AcquireTokenForClient(new[] { TokenExchangeScope })
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                return assertionResult.AccessToken;
            };

            // First call
            await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, Username, assertionCallback)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(1, callbackInvocations);

            // Force-refresh call: must bypass cache, invoke callback, and return from IdentityProvider
            var forcedResult = await (app as IByUserFederatedIdentityCredential)
                .AcquireTokenByUserFederatedIdentityCredential(s_scopes, Username, assertionCallback)
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(TokenSource.IdentityProvider, forcedResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, callbackInvocations, "Assertion callback must be invoked again on ForceRefresh.");
        }
    }
}
