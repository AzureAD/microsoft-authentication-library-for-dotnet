// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class Agentic
    {
        const string ClientId = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // agent app
        const string TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
        const string AgentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";
        const string UserUpn = "aui1@msidlabtoint.onmicrosoft.com";
        private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";
        private const string Scope = "https://graph.microsoft.com/.default";

        [TestMethod]
        public async Task AgentUserIdentityGetsTokenForGraphTest()
        {
            await AgentUserIdentityGetsTokenForGraphAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AgentGetsAppTokenForGraphTest()
        {
            await AgentGetsAppTokenForGraph().ConfigureAwait(false);
        }

        private static async Task AgentGetsAppTokenForGraph()
        {
            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                        .Build();

            var result = await cca.AcquireTokenForClient([Scope])
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");
        }

        private static async Task AgentUserIdentityGetsTokenForGraphAsync()
        {
            var cca = ConfidentialClientApplicationBuilder
                        .Create(AgentIdentity)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithExtraQueryParameters("slice=first")
                        .WithClientAssertion((AssertionRequestOptions _) => GetAppCredentialAsync(AgentIdentity))
                        .Build();

            var result = await (cca as IByUsernameAndPassword).AcquireTokenByUsernamePassword([Scope], UserUpn, "no_password")
                .OnBeforeTokenRequest(
                async (request) =>
                {
                    string userFicAssertion = await GetUserFic().ConfigureAwait(false);
                    request.BodyParameters["user_federated_identity_credential"] = userFicAssertion;
                    request.BodyParameters["grant_type"] = "user_fic";

                    // remove the password
                    request.BodyParameters.Remove("password");

                    if (request.BodyParameters.TryGetValue("client_secret", out var secret)
                            && secret.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        request.BodyParameters.Remove("client_secret");
                    }
                }
                )
                .ExecuteAsync()
                .ConfigureAwait(false);

            IAccount account = await cca.GetAccountAsync(result.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            var result2 = await cca.AcquireTokenSilent([Scope], account).ExecuteAsync().ConfigureAwait(false);

            Assert.IsTrue(result2.AuthenticationResultMetadata.TokenSource == TokenSource.Cache, "Token should be from cache");
        }

        private static async Task<string> GetAppCredentialAsync(string fmiPath)
        {
            Assert.IsNotNull(fmiPath, "fmiPath cannot be null");
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var cca1 = ConfidentialClientApplicationBuilder
                        .Create(ClientId)
                        .WithAuthority("https://login.microsoftonline.com/", TenantId)
                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                        .WithExperimentalFeatures(true)
                        .WithCertificate(cert, sendX5C: true) //sendX5c enables SN+I auth which is required for FMI flows                        
                        .Build();

            var result = await cca1.AcquireTokenForClient([TokenExchangeUrl])
                .WithFmiPath(fmiPath)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine($"FMI app credential from : {result.AuthenticationResultMetadata.TokenSource}");

            return result.AccessToken;
        }

        private  static async Task<string> GetUserFic()
        {
            var cca1 = ConfidentialClientApplicationBuilder
                     .Create(AgentIdentity)
                     .WithAuthority("https://login.microsoftonline.com/", TenantId)
                     .WithExperimentalFeatures(true)
                     .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                     .WithClientAssertion(async (AssertionRequestOptions a) =>
                     {
                         Assert.AreEqual(AgentIdentity, a.ClientAssertionFmiPath);
                         var cred = await GetAppCredentialAsync(a.ClientAssertionFmiPath).ConfigureAwait(false);
                         return cred;
                     })                   
                     .Build();

            var result = await cca1.AcquireTokenForClient([TokenExchangeUrl])   
                .WithFmiPathForClientAssertion(AgentIdentity)
                .ExecuteAsync().ConfigureAwait(false);

            Trace.WriteLine($"User FIC credential from : {result.AuthenticationResultMetadata.TokenSource}");

            return result.AccessToken;
        }
    }
}
