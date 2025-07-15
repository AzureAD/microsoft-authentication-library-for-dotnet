﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class AuthenticationResultTests : TestBase
    {

        [TestMethod]
        public void GetAuthorizationHeader()
        {
#pragma warning disable CS0618 // exercising obsolete constructors until full deprecation
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
#pragma warning disable CS0618 // exercising obsolete constructors until full deprecation
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
    }
}
