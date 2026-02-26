// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class AgenticApplicationTests : TestBase
    {
        private const string AgentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";
        private const string PlatformClientId = "aab5089d-e764-47e3-9f28-cc11c2513821";
        private const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";
        private const string Scope = "https://graph.microsoft.com/.default";
        private const string TokenExchangeUrl = "api://AzureADTokenExchange/.default";
        private const string FakeAccessToken = "fake-agent-access-token";
        private const string FakeFmiToken = "fake-fmi-credential-token";
        private const string FakeUserFicToken = "fake-user-fic-token";
        private const string FakeUserToken = "fake-user-delegated-token";

        private static X509Certificate2 GetTestCertificate()
        {
            // Create a self-signed certificate for testing
            return CertificateHelper.CreateSelfSignedCertificate("CN=AgenticTestCert");
        }

        #region Builder Validation Tests

        [TestMethod]
        public void Builder_NullAgentIdentity_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => AgenticApplicationBuilder.Create(null));
        }

        [TestMethod]
        public void Builder_MissingAuthority_ThrowsOnBuild()
        {
            using var cert = GetTestCertificate();
            var ex = Assert.ThrowsException<InvalidOperationException>(
                () => AgenticApplicationBuilder
                    .Create(AgentIdentity)
                    .WithPlatformCredential(PlatformClientId, cert)
                    .Build());

            Assert.IsTrue(ex.Message.Contains("Authority"));
        }

        [TestMethod]
        public void Builder_MissingPlatformCredential_ThrowsOnBuild()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(
                () => AgenticApplicationBuilder
                    .Create(AgentIdentity)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .Build());

            Assert.IsTrue(ex.Message.Contains("Platform credential"));
        }

        [TestMethod]
        public void Builder_NullCertificate_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => AgenticApplicationBuilder
                    .Create(AgentIdentity)
                    .WithAuthority("https://login.microsoftonline.com/", TenantId)
                    .WithPlatformCredential(PlatformClientId, null));
        }

        [TestMethod]
        public void Builder_ValidConfig_Succeeds()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            Assert.IsNotNull(app);
            Assert.IsInstanceOfType(app, typeof(IAgenticApplication));
        }

        [TestMethod]
        public void Builder_AllOptions_Succeeds()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert, sendX5C: true)
                .WithTokenExchangeUrl("api://CustomTokenExchange/.default")
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .Build();

            Assert.IsNotNull(app);
        }

        #endregion

        #region AcquireTokenForAgent Tests

        [TestMethod]
        public void AcquireTokenForAgent_NullScopes_Throws()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            Assert.ThrowsException<ArgumentNullException>(
                () => app.AcquireTokenForAgent(null));
        }

        [TestMethod]
        public void AcquireTokenForAgent_ReturnsBuilder()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            var builder = app.AcquireTokenForAgent(new[] { Scope });
            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType(builder, typeof(AcquireTokenForAgentParameterBuilder));
        }

        [TestMethod]
        public void AcquireTokenForAgent_BuilderFluency()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            var correlationId = Guid.NewGuid();
            var builder = app.AcquireTokenForAgent(new[] { Scope })
                .WithForceRefresh(true)
                .WithCorrelationId(correlationId);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType(builder, typeof(AcquireTokenForAgentParameterBuilder));
        }

        [TestMethod]
        public async Task AcquireTokenForAgent_WithMockedHttp_SucceedsAsync()
        {
            using var cert = GetTestCertificate();
            using var harness = CreateTestHarness();

            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .WithHttpManager(harness.HttpManager)
                .WithInstanceDiscovery(false)
                .Build();

            // Mock 1: Platform CCA gets FMI credential (client assertion delegate calls AcquireTokenForClient)
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeFmiToken);

            // Mock 2: Agent CCA AcquireTokenForClient with the FMI credential as assertion
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeAccessToken);

            var result = await app.AcquireTokenForAgent(new[] { Scope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual(FakeAccessToken, result.AccessToken);
        }

        [TestMethod]
        public async Task AcquireTokenForAgent_WithForceRefresh_WorksAsync()
        {
            using var cert = GetTestCertificate();
            using var harness = CreateTestHarness();

            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .WithHttpManager(harness.HttpManager)
                .WithInstanceDiscovery(false)
                .Build();

            // First call - from IDP
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeFmiToken);
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeAccessToken);

            var result1 = await app.AcquireTokenForAgent(new[] { Scope })
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual(FakeAccessToken, result1.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result1.AuthenticationResultMetadata.TokenSource);

            // Second call with force refresh — should hit IDP again
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeFmiToken);
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: "refreshed-token");

            var result2 = await app.AcquireTokenForAgent(new[] { Scope })
                .WithForceRefresh(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.AreEqual("refreshed-token", result2.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, result2.AuthenticationResultMetadata.TokenSource);
        }

        #endregion

        #region AcquireTokenForAgentOnBehalfOfUser Tests

        [TestMethod]
        public void AcquireTokenForAgentOnBehalfOfUser_NullScopes_Throws()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            Assert.ThrowsException<ArgumentNullException>(
                () => app.AcquireTokenForAgentOnBehalfOfUser(null, "user@contoso.com"));
        }

        [TestMethod]
        public void AcquireTokenForAgentOnBehalfOfUser_NullUpn_Throws()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            Assert.ThrowsException<ArgumentNullException>(
                () => app.AcquireTokenForAgentOnBehalfOfUser(new[] { Scope }, null));
        }

        [TestMethod]
        public void AcquireTokenForAgentOnBehalfOfUser_EmptyUpn_Throws()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            Assert.ThrowsException<ArgumentNullException>(
                () => app.AcquireTokenForAgentOnBehalfOfUser(new[] { Scope }, ""));
        }

        [TestMethod]
        public void AcquireTokenForAgentOnBehalfOfUser_ReturnsBuilder()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            var builder = app.AcquireTokenForAgentOnBehalfOfUser(
                new[] { Scope }, "user@contoso.com");
            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType(builder, typeof(AcquireTokenForAgentOnBehalfOfUserParameterBuilder));
        }

        [TestMethod]
        public void AcquireTokenForAgentOnBehalfOfUser_BuilderFluency()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            var correlationId = Guid.NewGuid();
            var builder = app.AcquireTokenForAgentOnBehalfOfUser(
                new[] { Scope }, "user@contoso.com")
                .WithForceRefresh(true)
                .WithCorrelationId(correlationId);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType(builder, typeof(AcquireTokenForAgentOnBehalfOfUserParameterBuilder));
        }

        [TestMethod]
        public async Task AcquireTokenForAgentOnBehalfOfUser_WithMockedHttp_SucceedsAsync()
        {
            using var cert = GetTestCertificate();
            using var harness = CreateTestHarness();

            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .WithHttpManager(harness.HttpManager)
                .WithInstanceDiscovery(false)
                .Build();

            // Mock 1: Platform CCA gets FMI credential for User FIC (agent assertion delegate)
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeFmiToken);

            // Mock 2: Agent CCA gets User FIC token (AcquireTokenForClient with FmiPathForClientAssertion)
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeUserFicToken);

            // Mock 3: Platform CCA gets FMI credential again (for the username/password rewrite assertion)
            harness.HttpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(
                token: FakeFmiToken);

            // Mock 4: User token response for the user_fic grant rewrite
            harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                authority: "https://login.microsoftonline.com/" + TenantId + "/");

            var result = await app.AcquireTokenForAgentOnBehalfOfUser(
                new[] { Scope }, "user@contoso.com")
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);
        }

        #endregion

        #region AcquireTokenSilent Tests

        [TestMethod]
        public void AcquireTokenSilent_ReturnsBuilder()
        {
            using var cert = GetTestCertificate();
            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .Build();

            // Create a mock IAccount
            var account = new Account("uid.utid", "user@contoso.com", "login.microsoftonline.com");
            var builder = app.AcquireTokenSilent(new[] { Scope }, account);
            Assert.IsNotNull(builder);
        }

        #endregion

        #region Cancellation Tests

        [TestMethod]
        public async Task AcquireTokenForAgent_Cancelled_ThrowsAsync()
        {
            using var cert = GetTestCertificate();
            using var harness = CreateTestHarness();

            var app = AgenticApplicationBuilder
                .Create(AgentIdentity)
                .WithAuthority("https://login.microsoftonline.com/", TenantId)
                .WithPlatformCredential(PlatformClientId, cert)
                .WithHttpManager(harness.HttpManager)
                .WithInstanceDiscovery(false)
                .Build();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // The assertion delegate should throw because the CCA call is cancelled
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                () => app.AcquireTokenForAgent(new[] { Scope })
                    .ExecuteAsync(cts.Token));
        }

        #endregion

        #region Helper

        /// <summary>
        /// Lightweight self-signed cert factory for unit tests.
        /// </summary>
        internal static class CertificateHelper
        {
            public static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
            {
#if NET8_0_OR_GREATER
                using var rsa = System.Security.Cryptography.RSA.Create(2048);
                var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                    subjectName,
                    rsa,
                    System.Security.Cryptography.HashAlgorithmName.SHA256,
                    System.Security.Cryptography.RSASignaturePadding.Pkcs1);

                var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
                return cert;
#else
                // For netstandard/net48, use a pre-created test certificate or skip
                // This path won't be hit in net8 test runs
                throw new PlatformNotSupportedException("Self-signed cert creation requires .NET 8+");
#endif
            }
        }

        #endregion
    }
}
