// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class RequestBaseTests : TestBase
    {
        /// <summary>
        /// Test helper class that exposes the protected CacheTokenResponseAndCreateAuthenticationResultAsync method
        /// </summary>
        private class TestableRequestBase : RequestBase
        {
            public TestableRequestBase(
                IServiceBundle serviceBundle,
                AuthenticationRequestParameters authenticationRequestParameters,
                IAcquireTokenParameters acquireTokenParameters) 
                : base(serviceBundle, authenticationRequestParameters, acquireTokenParameters)
            {
            }

            protected override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
            {
                // Not used in our tests
                throw new NotImplementedException();
            }

            // Expose the protected method for testing
            public Task<AuthenticationResult> TestCacheTokenResponseAndCreateAuthenticationResultAsync(MsalTokenResponse msalTokenResponse)
            {
                return CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse);
            }
        }

        /// <summary>
        /// Tests CacheTokenResponseAndCreateAuthenticationResultAsync when ClientCredentialCertificate is provided.
        /// </summary>
        [TestMethod]
        public async Task CacheTokenResponseAndCreateAuthenticationResultAsync_WithClientCredentialCertificate_SetsBindingCertificateAsync()
        {
            // Arrange
            var clientCredentialCertificate = CertHelper.GetOrCreateTestCert();
            
            // Create a confidential client app with certificate
            using var harness = new MockHttpAndServiceBundle();
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(clientCredentialCertificate)
                .WithHttpManager(harness.HttpManager)
                .BuildConcrete();

            // Create common parameters
            var commonParameters = new AcquireTokenCommonParameters
            {
                Scopes = TestConstants.s_scope,
                ExtraQueryParameters = new Dictionary<string, string>(),
                ApiId = ApiEvent.ApiIds.None
            };

            var authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            var requestContext = new RequestContext(app.ServiceBundle, Guid.NewGuid(), commonParameters.MtlsCertificate);
            
            // Initialize ApiEvent to avoid null reference exceptions
            requestContext.ApiEvent = new ApiEvent(Guid.NewGuid());
            
            // Create parameters with the app's service bundle that has the certificate
            var parameters = new AuthenticationRequestParameters(
                app.ServiceBundle, // Use the app's service bundle that contains the certificate
                new TokenCache(app.ServiceBundle, false),
                commonParameters,
                requestContext,
                authority);
            
            var acquireTokenParameters = new AcquireTokenSilentParameters();
            var testableRequest = new TestableRequestBase(app.ServiceBundle, parameters, acquireTokenParameters);

            // Create a mock token response
            var tokenResponse = new MsalTokenResponse
            {
                AccessToken = "test-access-token",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = TestConstants.s_scope.AsSingleString(),
                ClientInfo = MockHelpers.CreateClientInfo(),
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RefreshToken = "test-refresh-token"
            };

            // Act
            var result = await testableRequest.TestCacheTokenResponseAndCreateAuthenticationResultAsync(tokenResponse).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should not be null when ClientCredentialCertificate is provided");
            Assert.AreEqual(clientCredentialCertificate, result.BindingCertificate, "BindingCertificate should match the ClientCredentialCertificate");
            Assert.AreEqual("test-access-token", result.AccessToken, "AccessToken should be set correctly");
        }

        /// <summary>
        /// Tests CacheTokenResponseAndCreateAuthenticationResultAsync when ClientCredentialCertificate is null.
        /// </summary>
        [TestMethod]
        public async Task CacheTokenResponseAndCreateAuthenticationResultAsync_WithNullClientCredentialCertificate_SetsBindingCertificateToNullAsync()
        {
            // Arrange
            using var harness = new MockHttpAndServiceBundle();
            
            // Create parameters without any certificate (default case)
            var parameters = harness.CreateAuthenticationRequestParameters(
                TestConstants.AuthorityHomeTenant,
                TestConstants.s_scope,
                new TokenCache(harness.ServiceBundle, false),
                account: null);
            
            // Initialize ApiEvent to avoid null reference exceptions
            parameters.RequestContext.ApiEvent = new ApiEvent(Guid.NewGuid());
            
            // No need to modify AppConfig - by default ClientCredentialCertificate should be null
            
            var acquireTokenParameters = new AcquireTokenSilentParameters();
            var testableRequest = new TestableRequestBase(harness.ServiceBundle, parameters, acquireTokenParameters);

            // Create a mock token response
            var tokenResponse = new MsalTokenResponse
            {
                AccessToken = "test-access-token-2",
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = TestConstants.s_scope.AsSingleString(),
                ClientInfo = MockHelpers.CreateClientInfo(),
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                RefreshToken = "test-refresh-token-2"
            };

            // Act
            var result = await testableRequest.TestCacheTokenResponseAndCreateAuthenticationResultAsync(tokenResponse).ConfigureAwait(false);

            // Assert
            Assert.IsNull(result.BindingCertificate, "BindingCertificate should be null when ClientCredentialCertificate is not provided");
            Assert.AreEqual("test-access-token-2", result.AccessToken, "AccessToken should be set correctly");
        }
    }
}
