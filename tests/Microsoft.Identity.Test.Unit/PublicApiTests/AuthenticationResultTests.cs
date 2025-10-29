// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AuthenticationResultTests : TestBase
    {

        [TestMethod]
        public void PublicTestConstructorCoversAllProperties()
        {
            // The first public ctor that is meant only for tests (“for-test”)
            var ctorParameters = typeof(AuthenticationResult)
                .GetConstructors()
                .First(ctor => ctor.GetParameters().Length > 3)
                .GetParameters();

            var classProperties = typeof(AuthenticationResult)
                .GetProperties()
                .Where(p => p.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                .Where(p => p.SetMethod == null || p.SetMethod.IsPublic)
                .Where(p => p.Name != nameof(AuthenticationResult.BindingCertificate));

            // +2 for the 2 obsolete ExtendedExpires* props that are deliberately
            // not represented in the ctor.
            Assert.AreEqual(
                ctorParameters.Length,
                classProperties.Count() + 2,
                "The <for-test> constructor should include all public-settable or read-only properties of AuthenticationResult (except BindingCertificate and the obsolete ExtendedExpires* pair).");
        }

        [TestMethod]
        public void GetAuthorizationHeader()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                "idt", new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache));

            Assert.AreEqual("SomeTokenType at", ar.CreateAuthorizationHeader());
        }

        [TestMethod]
        public void GetHybridSpaAuthCode()
        {
            var ar = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                "idt", new[] { "scope" }, Guid.NewGuid(),
                "SomeTokenType",
                new AuthenticationResultMetadata(TokenSource.Cache),
                null,
                "SpaAuthCatCode");

            Assert.AreEqual("SpaAuthCatCode", ar.SpaAuthCode);
        }

        [TestMethod]
        [Description(
            "In MSAL 4.17 we made a mistake and added AuthenticationResultMetadata with no default value before tokenType param. " +
            "To fix this breaking change, we added 2 constructors - " +
            "one for backwards compat with 4.17+ and one for 4.16 and below")]
        public void AuthenticationResult_PublicApi()
        {
            // old constructor, before 4.16
            var ar1 = new AuthenticationResult(
                "at",
                false,
                "uid",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid());

            Assert.IsNull(ar1.AuthenticationResultMetadata);
            Assert.AreEqual("Bearer", ar1.TokenType);

            // old constructor, before 4.16
            var ar2 = new AuthenticationResult(
              "at",
              false,
              "uid",
              DateTime.UtcNow,
              DateTime.UtcNow,
              "tid",
              new Account("aid", "user", "env"),
              "idt",
              new[] { "scope" },
              Guid.NewGuid(),
              "ProofOfBear");

            Assert.IsNull(ar2.AuthenticationResultMetadata);
            Assert.AreEqual("ProofOfBear", ar2.TokenType);

            // new ctor, after 4.17
            var ar3 = new AuthenticationResult(
             "at",
             false,
             "uid",
             DateTime.UtcNow,
             DateTime.UtcNow,
             "tid",
             new Account("aid", "user", "env"),
             "idt",
             new[] { "scope" },
             Guid.NewGuid(),
             new AuthenticationResultMetadata(TokenSource.Broker),
             tokenType: "Bearer");

            Assert.AreEqual(TokenSource.Broker, ar3.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("Bearer", ar1.TokenType);

        }

        [TestMethod]
        public async Task MsalTokenResponseParseTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                  .WithRedirectUri(TestConstants.RedirectUri)
                  .WithClientSecret(TestConstants.ClientSecret)
                  .WithHttpManager(harness.HttpManager)
                  .BuildConcrete();

                string jsonContent = MockHelpers.CreateSuccessTokenResponseString(
                        TestConstants.Uid,
                       TestConstants.DisplayableId,
                       TestConstants.s_scope.ToArray());

                jsonContent = jsonContent.TrimEnd('}');
                jsonContent += ",";

                jsonContent += "\"number_extension\":1209599,";
                jsonContent += "\"true_extension\":true,";
                jsonContent += "\"false_extension\":false,";
                jsonContent += "\"date_extension\":\"2019-08-01T00:00:00-07:00\",";
                jsonContent += "\"null_extension\":null,";
                jsonContent += "\"null_string_extension\":\"null\",";
                jsonContent += "\"array_extension\":[\"a\",\"b\",\"c\"],";
                jsonContent += "\"object_extension\":{\"a\":\"b\"}";
                jsonContent += "}";

                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var handler = harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                    responseMessage: MockHelpers.CreateSuccessResponseMessage(jsonContent));

                AuthenticationResult result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithSpaAuthorizationCode(true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                IReadOnlyDictionary<string, string> extMap = result.AdditionalResponseParameters;

                // Strongly typed properties should not be exposed (we don't want to expose refresh token)
                Assert.IsFalse(extMap.ContainsKey("scope"));
                Assert.IsFalse(extMap.ContainsKey("expires_in"));
                Assert.IsFalse(extMap.ContainsKey("ext_expires_in"));
                Assert.IsFalse(extMap.ContainsKey("access_token"));
                Assert.IsFalse(extMap.ContainsKey("refresh_token"));
                Assert.IsFalse(extMap.ContainsKey("id_token"));
                Assert.IsFalse(extMap.ContainsKey("client_info"));

                // all other properties should be in the map
                Assert.AreEqual("1209599", extMap["number_extension"]);
                Assert.AreEqual("True", extMap["true_extension"]);
                Assert.AreEqual("False", extMap["false_extension"]);
                Assert.AreEqual("2019-08-01T00:00:00-07:00", extMap["date_extension"]);
                Assert.AreEqual("null", extMap["null_string_extension"]);
                Assert.AreEqual("", extMap["null_extension"]);

            }
        }

        /// <summary>
        /// Verifies that if no token type is specified, the default is 'Bearer',
        /// and CreateAuthorizationHeader() uses it.
        /// </summary>
        [TestMethod]
        public void DefaultTokenType_IsBearer_Test()
        {
            DateTime now = DateTime.UtcNow;

            var ar = new AuthenticationResult(
                accessToken: "some-access-token",
                isExtendedLifeTimeToken: false,
                uniqueId: "unique-id",
                expiresOn: now.AddMinutes(15),
                extendedExpiresOn: now.AddMinutes(30),
                tenantId: "tid",
                account: new Account("aid", "user", "env"),
                idToken: "my-id-token",
                scopes: new[] { "scope" },
                correlationId: Guid.NewGuid()
            );

            Assert.AreEqual("Bearer", ar.TokenType, "Expected default token type to be 'Bearer'");
            Assert.AreEqual("Bearer some-access-token", ar.CreateAuthorizationHeader());
        }

        /// <summary>
        /// Tests that all public properties of AuthenticationResult have a public setter.
        /// </summary>
        [TestMethod]
        public void AllPublicProperties_HavePublicSetter()
        {
            // ---- expected public-settable properties ----
            string[] expected =
            [
                // core primitives
                nameof(AuthenticationResult.AccessToken),
                nameof(AuthenticationResult.UniqueId),
                nameof(AuthenticationResult.ExpiresOn),
                nameof(AuthenticationResult.TenantId),
                nameof(AuthenticationResult.Account),
                nameof(AuthenticationResult.IdToken),
                nameof(AuthenticationResult.Scopes),
                nameof(AuthenticationResult.CorrelationId),
                nameof(AuthenticationResult.TokenType),

                // SPA / mTLS extras
                nameof(AuthenticationResult.SpaAuthCode),
                nameof(AuthenticationResult.BindingCertificate),

                // ancillary data
                nameof(AuthenticationResult.AdditionalResponseParameters),
                nameof(AuthenticationResult.ClaimsPrincipal),
                nameof(AuthenticationResult.AuthenticationResultMetadata)
            ];

            // ---- reflection gather ----
            var propsWithPublicSetter = typeof(AuthenticationResult)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)     // skip obsolete
                .Where(p => p.GetSetMethod(/*nonPublic*/ false) != null)           // has public setter
                .Select(p => p.Name)
                .OrderBy(n => n)
                .ToArray();

            // ---- assertion ----
            CollectionAssert.AreEquivalent(
                expected.OrderBy(n => n).ToArray(),
                propsWithPublicSetter,
                "All non-obsolete public properties should expose a public setter."
            );
        }

        /// <summary>
        /// Tests that the BindingCertificate property can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void BindingCertificate_Property_CanBeSetAndRetrieved()
        {
            // Arrange
            var bindingCertificate = CertHelper.GetOrCreateTestCert();
            var accessToken = "test-access-token";

            var ar = new AuthenticationResult(
                accessToken,
                false,
                "uid",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(2),
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid(),
                "Bearer",
                new AuthenticationResultMetadata(TokenSource.Cache))
            {
                BindingCertificate = bindingCertificate
            };

            // Assert
            Assert.IsNotNull(ar.BindingCertificate, "BindingCertificate should not be null");
            Assert.AreEqual(bindingCertificate, ar.BindingCertificate, "BindingCertificate should match the provided certificate");
        }

        /// <summary>
        /// Tests that the BindingCertificate property can be null.
        /// </summary>
        [TestMethod]
        public void BindingCertificate_Property_CanBeNull()
        {
            // Arrange
            var accessToken = "test-access-token";

            var ar = new AuthenticationResult(
                accessToken,
                false,
                "uid",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(2),
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid(),
                "Bearer",
                new AuthenticationResultMetadata(TokenSource.Cache))
            {
                BindingCertificate = null
            };

            // Assert
            Assert.IsNull(ar.BindingCertificate, "BindingCertificate should be null when not provided");
        }

        /// <summary>
        /// Tests that CreateAuthorizationHeaderBound returns correct AuthorizationHeaderInformation with binding certificate.
        /// </summary>
        [TestMethod]
        public void CreateAuthorizationHeaderBound_WithBindingCertificate_ReturnsCorrectHeaderInformation()
        {
            // Arrange
            var bindingCertificate = CertHelper.GetOrCreateTestCert();
            var accessToken = "test-access-token";
            var tokenType = "Bearer";

            var ar = new AuthenticationResult(
                accessToken,
                false,
                "uid",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(2),
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid(),
                tokenType,
                new AuthenticationResultMetadata(TokenSource.Cache))
            {
                BindingCertificate = bindingCertificate
            };

            // Act
            var headerInfo = ar.CreateAuthorizationHeaderBound();

            // Assert
            Assert.IsNotNull(headerInfo, "AuthorizationHeaderInformation should not be null");
            Assert.AreEqual($"{tokenType} {accessToken}", headerInfo.AuthorizationHeaderValue, 
                "AuthorizationHeaderValue should match expected format");
            Assert.AreEqual(bindingCertificate, headerInfo.BindingCertificate, 
                "BindingCertificate should match the certificate from AuthenticationResult");
        }

        /// <summary>
        /// Tests that CreateAuthorizationHeaderBound works with null binding certificate.
        /// </summary>
        [TestMethod]
        public void CreateAuthorizationHeaderBound_WithNullBindingCertificate_ReturnsHeaderInformationWithNullCertificate()
        {
            // Arrange
            var accessToken = "test-access-token";
            var tokenType = "PoP";

            var ar = new AuthenticationResult(
                accessToken,
                false,
                "uid",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(2),
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid(),
                tokenType,
                new AuthenticationResultMetadata(TokenSource.Cache))
            {
                BindingCertificate = null
            };

            // Act
            var headerInfo = ar.CreateAuthorizationHeaderBound();

            // Assert
            Assert.IsNotNull(headerInfo, "AuthorizationHeaderInformation should not be null");
            Assert.AreEqual($"{tokenType} {accessToken}", headerInfo.AuthorizationHeaderValue, 
                "AuthorizationHeaderValue should match expected format");
            Assert.IsNull(headerInfo.BindingCertificate, 
                "BindingCertificate should be null when AuthenticationResult has null BindingCertificate");
        }

        /// <summary>
        /// Tests that CreateAuthorizationHeaderBound uses the correct token type in authorization header.
        /// </summary>
        [TestMethod]
        public void CreateAuthorizationHeaderBound_WithCustomTokenType_UsesCorrectTokenType()
        {
            // Arrange
            var bindingCertificate = CertHelper.GetOrCreateTestCert();
            var accessToken = "custom-token";
            var customTokenType = "CustomType";

            var ar = new AuthenticationResult(
                accessToken,
                false,
                "uid",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(2),
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid(),
                customTokenType,
                new AuthenticationResultMetadata(TokenSource.Cache))
            {
                BindingCertificate = bindingCertificate
            };

            // Act
            var headerInfo = ar.CreateAuthorizationHeaderBound();

            // Assert
            Assert.IsNotNull(headerInfo, "AuthorizationHeaderInformation should not be null");
            Assert.AreEqual($"{customTokenType} {accessToken}", headerInfo.AuthorizationHeaderValue, 
                "AuthorizationHeaderValue should use the custom token type");
            Assert.AreEqual(bindingCertificate, headerInfo.BindingCertificate, 
                "BindingCertificate should match the provided certificate");
        }

        /// <summary>
        /// Tests that CreateAuthorizationHeaderBound and CreateAuthorizationHeader return the same authorization header value.
        /// </summary>
        [TestMethod]
        public void CreateAuthorizationHeaderBound_AuthorizationHeaderValue_MatchesCreateAuthorizationHeader()
        {
            // Arrange
            var bindingCertificate = CertHelper.GetOrCreateTestCert();
            var accessToken = "test-token";
            var tokenType = "Bearer";

            var ar = new AuthenticationResult(
                accessToken,
                false,
                "uid",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddHours(2),
                "tid",
                new Account("aid", "user", "env"),
                "idt",
                new[] { "scope" },
                Guid.NewGuid(),
                tokenType,
                new AuthenticationResultMetadata(TokenSource.Cache))
            {
                BindingCertificate = bindingCertificate
            };

            // Act
            var headerInfo = ar.CreateAuthorizationHeaderBound();
            var directHeader = ar.CreateAuthorizationHeader();

            // Assert
            Assert.AreEqual(directHeader, headerInfo.AuthorizationHeaderValue, 
                "CreateAuthorizationHeaderBound().AuthorizationHeaderValue should match CreateAuthorizationHeader()");
        }

        /// <summary>
        /// Tests the internal constructor with binding certificate using cache items.
        /// </summary>
        [TestMethod]
        public void InternalConstructor_WithBindingCertificate_SetsBindingCertificateProperty()
        {
            // Arrange
            var bindingCertificate = CertHelper.GetOrCreateTestCert();
            var correlationId = Guid.NewGuid();
            var account = new Account("test-account-id", "test-user", "test-env");
            var authScheme = new BearerAuthenticationOperation();
            var tokenSource = TokenSource.Cache;
            var apiEvent = new ApiEvent(correlationId);
            var clientInfo = MockHelpers.CreateClientInfo();

            // Create simple cache items with mocked data
            var accessTokenCacheItem = new MsalAccessTokenCacheItem(
                "login.microsoftonline.com",
                "test-client-id",
                "scope1 scope2",
                "test-tenant-id",
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(2),
                clientInfo,
                "test-home-account-id");
            accessTokenCacheItem.Secret = "test-access-token";

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                "login.microsoftonline.com",
                "test-client-id",
                MockHelpers.CreateIdToken("test-unique-id", "test@example.com"),
                clientInfo,
                "test-home-account-id",
                "test-tenant-id");

            // Act
            var result = new AuthenticationResult(
                accessTokenCacheItem,
                idTokenCacheItem,
                authScheme,
                correlationId,
                tokenSource,
                apiEvent,
                account,
                null, // spaAuthCode
                null, // additionalResponseParameters
                bindingCertificate);

            // Assert
            Assert.IsNotNull(result.BindingCertificate, "BindingCertificate should not be null");
            Assert.AreEqual(bindingCertificate, result.BindingCertificate, "BindingCertificate should match the provided certificate");
            Assert.AreEqual("test-access-token", result.AccessToken, "AccessToken should be set from cache item");
            Assert.AreEqual(correlationId, result.CorrelationId, "CorrelationId should be set");
        }

        /// <summary>
        /// Tests the internal constructor with null binding certificate using cache items.
        /// </summary>
        [TestMethod]
        public void InternalConstructor_WithNullBindingCertificate_SetsBindingCertificateToNull()
        {
            // Arrange
            var correlationId = Guid.NewGuid();
            var account = new Account("test-account-id", "test-user", "test-env");
            var authScheme = new BearerAuthenticationOperation();
            var tokenSource = TokenSource.Broker;
            var apiEvent = new ApiEvent(correlationId);
            var clientInfo = MockHelpers.CreateClientInfo();

            // Create simple cache items with mocked data
            var accessTokenCacheItem = new MsalAccessTokenCacheItem(
                "login.microsoftonline.com",
                "test-client-id",
                "scope1 scope2",
                "test-tenant-id",
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(2),
                clientInfo,
                "test-home-account-id");
            accessTokenCacheItem.Secret = "test-access-token-2";

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                "login.microsoftonline.com",
                "test-client-id",
                MockHelpers.CreateIdToken("test-unique-id-2", "test2@example.com"),
                clientInfo,
                "test-home-account-id",
                "test-tenant-id");

            // Act
            var result = new AuthenticationResult(
                accessTokenCacheItem,
                idTokenCacheItem,
                authScheme,
                correlationId,
                tokenSource,
                apiEvent,
                account,
                null, // spaAuthCode
                null, // additionalResponseParameters
                null); // bindingCertificate

            // Assert
            Assert.IsNull(result.BindingCertificate, "BindingCertificate should be null when not provided");
            Assert.AreEqual("test-access-token-2", result.AccessToken, "AccessToken should be set");
            Assert.AreEqual(correlationId, result.CorrelationId, "CorrelationId should be set");
        }
    }
}
