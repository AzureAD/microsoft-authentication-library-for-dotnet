// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class IdTokenExtendedTests : TestBase
    {
        [TestMethod]
        public void Parse_Null_ReturnsNull()
        {
            Assert.IsNull(IdToken.Parse(null));
        }

        [TestMethod]
        public void Parse_EmptyString_ReturnsNull()
        {
            Assert.IsNull(IdToken.Parse(string.Empty));
        }

        [TestMethod]
        public void Parse_SingleSegment_ThrowsMsalClientException()
        {
            Assert.Throws<MsalClientException>(() => IdToken.Parse("singlesegment"));
        }

        [TestMethod]
        public void Parse_SingleSegment_HasCorrectErrorCode()
        {
            try
            {
                IdToken.Parse("singlesegment");
                Assert.Fail("Should have thrown");
            }
            catch (MsalClientException ex)
            {
                Assert.AreEqual(MsalError.InvalidJwtError, ex.ErrorCode);
            }
        }

        [TestMethod]
        public void GetUniqueId_UsesObjectId_WhenPresent()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                @"{""oid"":""object-id-123"",""sub"":""subject-456""}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);
            Assert.AreEqual("object-id-123", token.GetUniqueId());
        }

        [TestMethod]
        public void GetUniqueId_FallsBackToSubject_WhenObjectIdMissing()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                @"{""sub"":""subject-456""}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);
            Assert.IsNull(token.ObjectId);
            Assert.AreEqual("subject-456", token.GetUniqueId());
        }

        [TestMethod]
        public void Parse_MapsAllStandardClaims()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                @"{""oid"":""oid1"",""sub"":""sub1"",""tid"":""tid1"",""preferred_username"":""user@example.com"",""name"":""Test User"",""email"":""test@example.com"",""upn"":""user@domain.com"",""given_name"":""Test"",""family_name"":""User""}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);

            Assert.AreEqual("oid1", token.ObjectId);
            Assert.AreEqual("sub1", token.Subject);
            Assert.AreEqual("tid1", token.TenantId);
            Assert.AreEqual("user@example.com", token.PreferredUsername);
            Assert.AreEqual("Test User", token.Name);
            Assert.AreEqual("test@example.com", token.Email);
            Assert.AreEqual("user@domain.com", token.Upn);
            Assert.AreEqual("Test", token.GivenName);
            Assert.AreEqual("User", token.FamilyName);
        }

        [TestMethod]
        public void Parse_ClaimsPrincipal_IsPopulated()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                @"{""iss"":""https://login.example.com/tenant/v2.0"",""name"":""Test User""}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);

            Assert.IsNotNull(token.ClaimsPrincipal);
            Assert.IsNotNull(token.ClaimsPrincipal.Identity);
        }

        [TestMethod]
        public void Parse_NoIssuer_UsesLocalAuthority()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                @"{""name"":""Test User""}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);

            Assert.IsNotNull(token.ClaimsPrincipal);
            var nameClaim = token.ClaimsPrincipal.FindFirst("name");
            Assert.IsNotNull(nameClaim);
            Assert.AreEqual("LOCAL AUTHORITY", nameClaim.Issuer);
        }

        [TestMethod]
        public void Parse_NullClaimValue_CreatesJsonNullClaim()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                @"{""iss"":""issuer"",""custom_claim"":null}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);

            Assert.IsNotNull(token.ClaimsPrincipal);
            var customClaim = token.ClaimsPrincipal.FindFirst("custom_claim");
            Assert.IsNotNull(customClaim);
            Assert.AreEqual(string.Empty, customClaim.Value);
        }

        [TestMethod]
        public void Parse_InvalidBase64Payload_Throws()
        {
            // Valid JWT structure but payload is not valid base64 - may throw MsalClientException or FormatException
            try
            {
                IdToken.Parse("header.!!!invalid!!!.signature");
                Assert.Fail("Should have thrown an exception");
            }
            catch (MsalClientException)
            {
                // Expected - JSON parse error
            }
            catch (FormatException)
            {
                // Expected - invalid base64
            }
        }

        [TestMethod]
        public void Parse_MinimalToken_NoOptionalClaims()
        {
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"{}"));
            string jwt = $"header.{payload}.signature";

            var token = IdToken.Parse(jwt);

            Assert.IsNull(token.ObjectId);
            Assert.IsNull(token.Subject);
            Assert.IsNull(token.TenantId);
            Assert.IsNull(token.PreferredUsername);
            Assert.IsNull(token.Name);
            Assert.IsNull(token.Email);
            Assert.IsNull(token.Upn);
            Assert.IsNull(token.GivenName);
            Assert.IsNull(token.FamilyName);
            Assert.IsNull(token.GetUniqueId());
        }
    }
}
