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
        private const string _fmiScope3 = "api://AzureAADTokenExchange/.default";
        private const string _fmiPath = "SomeFmiPath/fmi";

        [TestMethod]
        [DataRow(_fmiClientId, _fmiAuthority, _fmiScope1, false)]
        [DataRow(_fmiClientId, _fmiAuthority, _fmiScope2, false)]
        [DataRow(_fmiClientId, _fmiAuthority, _fmiScope1, true)]
        //[DataRow(_fmiAppUrn, _fmiAuthority, _fmiScope3, true)]
        public async Task FmiIntegrationTestAsync(string clientId, string authority, string scope, bool useAssertion)
        {
            await RunHappyPath(clientId, authority, scope, useAssertion).ConfigureAwait(false);
        }

        private async Task RunHappyPath(string clientId, string authority, string scope, bool useAssertion)
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            ConfidentialClientApplication confidentialApp = null;
            var builder = ConfidentialClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(authority, true)
                        .WithExtraQueryParameters("dc=ESTS-PUB-SCUS-LZ1-FD000-TEST1")
                        .WithExperimentalFeatures(true);

            if (useAssertion)
            {
                builder.WithClientAssertion(options => GetSignedClientAssertion(cert, options.TokenEndpoint, options.ClientID));
            }
            else
            {
                builder.WithCertificate(cert, sendX5C: true);
            }

            confidentialApp = builder.BuildConcrete();

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

        private Task<string> GetSignedClientAssertion(X509Certificate2 certificate, string tokenEndpoint, string clientId)
        {
            // no need to add exp, nbf as JsonWebTokenHandler will add them by default.
            var claims = new Dictionary<string, object>()
            {
                { "aud", tokenEndpoint },
                { "iss", clientId },
                { "jti", Guid.NewGuid().ToString() },
                { "sub", clientId }
            };

            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(certificate)
            };

            var handler = new JsonWebTokenHandler();
            return Task.FromResult(handler.CreateToken(securityTokenDescriptor));
        }
    }
}
