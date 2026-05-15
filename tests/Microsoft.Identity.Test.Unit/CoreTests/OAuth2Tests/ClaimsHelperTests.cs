// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.OAuth2Tests
{
    [TestClass]
    public class ClaimsHelperTests
    {
        #region NormalizeClaimsJson

        [TestMethod]
        public void NormalizeClaimsJson_SortsTopLevelKeys()
        {
            // Arrange
            string unordered = @"{""z"":1,""a"":2,""m"":3}";

            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(unordered);

            // Assert — keys must appear in ordinal ascending order
            Assert.AreEqual(@"{""a"":2,""m"":3,""z"":1}", result);
        }

        [TestMethod]
        public void NormalizeClaimsJson_SameClaimsWithDifferentKeyOrdering_ProduceSameString()
        {
            // Arrange
            string variant1 = @"{""b"":{""essential"":true},""a"":{""value"":""foo""}}";
            string variant2 = @"{""a"":{""value"":""foo""},""b"":{""essential"":true}}";

            // Act
            string result1 = ClaimsHelper.NormalizeClaimsJson(variant1);
            string result2 = ClaimsHelper.NormalizeClaimsJson(variant2);

            // Assert
            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void NormalizeClaimsJson_StripsInsignificantWhitespace()
        {
            // Arrange
            string withSpaces = "{ \"z\" : 1 , \"a\" : 2 }";
            string compact = @"{""z"":1,""a"":2}";

            // Act
            string result1 = ClaimsHelper.NormalizeClaimsJson(withSpaces);
            string result2 = ClaimsHelper.NormalizeClaimsJson(compact);

            // Assert — both should normalize to the same compact, sorted string
            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void NormalizeClaimsJson_NestedObjectsAreSorted()
        {
            // Arrange
            string input = @"{""userinfo"":{""z"":true,""a"":false}}";

            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(input);

            // Assert — nested keys must also be sorted
            Assert.AreEqual(@"{""userinfo"":{""a"":false,""z"":true}}", result);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void NormalizeClaimsJson_NullOrWhitespace_ReturnsInputUnchanged(string input)
        {
            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(input);

            // Assert
            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void NormalizeClaimsJson_InvalidJson_ThrowsMsalClientException()
        {
            // Arrange
            string badJson = "not-json";

            // Act & Assert
            MsalClientException ex = Assert.ThrowsExactly<MsalClientException>(
                () => ClaimsHelper.NormalizeClaimsJson(badJson));

            Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
        }

        [TestMethod]
        [DataRow("[]")]          // JSON array — valid JSON but not an object
        [DataRow("[1,2,3]")]
        [DataRow("\"string\"")]  // JSON string — valid JSON but not an object
        [DataRow("42")]          // JSON number
        public void NormalizeClaimsJson_ValidJsonButNotObject_ThrowsMsalClientException(string nonObjectJson)
        {
            // Act & Assert — InvalidOperationException from JsonNode.AsObject() must be translated
            MsalClientException ex = Assert.ThrowsExactly<MsalClientException>(
                () => ClaimsHelper.NormalizeClaimsJson(nonObjectJson));

            Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
        }

        #endregion

        #region MergeClaimsObjects

        [TestMethod]
        public void MergeClaimsObjects_BothNull_ReturnsNull()
        {
            // Act
            string result = ClaimsHelper.MergeClaimsObjects(null, null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void MergeClaimsObjects_FirstNull_ReturnsSecond()
        {
            // Arrange
            string claims2 = @"{""a"":1}";

            // Act
            string result = ClaimsHelper.MergeClaimsObjects(null, claims2);

            // Assert
            Assert.AreEqual(claims2, result);
        }

        [TestMethod]
        public void MergeClaimsObjects_SecondNull_ReturnsFirst()
        {
            // Arrange
            string claims1 = @"{""a"":1}";

            // Act
            string result = ClaimsHelper.MergeClaimsObjects(claims1, null);

            // Assert
            Assert.AreEqual(claims1, result);
        }

        [TestMethod]
        public void MergeClaimsObjects_NonOverlapping_ReturnsMergedObject()
        {
            // Arrange
            string claims1 = @"{""nsp"":{""essential"":true}}";
            string claims2 = @"{""userinfo"":{""given_name"":{""essential"":true}}}";

            // Act
            string result = ClaimsHelper.MergeClaimsObjects(claims1, claims2);

            // Assert — both top-level keys must be present
            using var doc = JsonDocument.Parse(result);
            Assert.IsTrue(doc.RootElement.TryGetProperty("nsp", out _), "nsp key should be present");
            Assert.IsTrue(doc.RootElement.TryGetProperty("userinfo", out _), "userinfo key should be present");
        }

        [TestMethod]
        public void MergeClaimsObjects_OverlappingKeys_SecondObjectWins()
        {
            // Arrange — both have 'nsp' but with different values
            string claims1 = @"{""nsp"":{""value"":""v1""}}";
            string claims2 = @"{""nsp"":{""value"":""v2""}}";

            // Act
            string result = ClaimsHelper.MergeClaimsObjects(claims1, claims2);

            // Assert — second value wins
            using var doc = JsonDocument.Parse(result);
            string nspValue = doc.RootElement.GetProperty("nsp").GetProperty("value").GetString();
            Assert.AreEqual("v2", nspValue);
        }

        [TestMethod]
        public void MergeClaimsObjects_EmptyStrings_TreatedAsNull()
        {
            // Arrange
            string claims1 = @"{""a"":1}";

            // Act
            string result = ClaimsHelper.MergeClaimsObjects(claims1, "");

            // Assert — empty string is treated as null, so first is returned
            Assert.AreEqual(claims1, result);
        }

        [TestMethod]
        public void MergeClaimsObjects_InvalidJson_ThrowsMsalClientException()
        {
            // Arrange — one side is invalid JSON
            string valid = @"{""a"":1}";
            string invalid = "not-json";

            // Act & Assert
            MsalClientException ex = Assert.ThrowsExactly<MsalClientException>(
                () => ClaimsHelper.MergeClaimsObjects(invalid, valid));

            Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
        }

        [TestMethod]
        [DataRow("[]")]
        [DataRow("\"string\"")]
        public void MergeClaimsObjects_ValidJsonButNotObject_ThrowsMsalClientException(string nonObjectJson)
        {
            // Arrange — valid JSON that is not an object triggers InvalidOperationException in ParseIntoJsonObject
            string valid = @"{""a"":1}";

            // Act & Assert — must be translated to MsalClientException, not leak InvalidOperationException
            MsalClientException ex = Assert.ThrowsExactly<MsalClientException>(
                () => ClaimsHelper.MergeClaimsObjects(nonObjectJson, valid));

            Assert.AreEqual(MsalError.InvalidJsonClaimsFormat, ex.ErrorCode);
        }

        #endregion

        #region OIDC §5.5 canonicalization edge cases

        [TestMethod]
        public void NormalizeClaimsJson_ArrayElementOrderIsPreserved()
        {
            // Arrange — OIDC §5.5 acr.values array; element order is semantically meaningful
            string input = @"{""id_token"":{""acr"":{""values"":[""urn:mace:incommon:iap:bronze"",""urn:mace:incommon:iap:silver""]}}}";

            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(input);
            string result2 = ClaimsHelper.NormalizeClaimsJson(result);

            // Assert — array order must be preserved after normalization
            Assert.IsLessThan(result.IndexOf("silver", StringComparison.Ordinal), result.IndexOf("bronze", StringComparison.Ordinal),
                "Array element order must be preserved — bronze must come before silver.");

            // Idempotency: normalizing twice gives the same result
            Assert.AreEqual(result, result2, "NormalizeClaimsJson must be idempotent.");
        }

        [TestMethod]
        public void NormalizeClaimsJson_NullClaimValue_IsPreserved()
        {
            // Arrange — voluntary claim with null value (OIDC §5.5)
            string input = @"{""userinfo"":{""picture"":null}}";

            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(input);

            // Assert
            using var doc = System.Text.Json.JsonDocument.Parse(result);
            var picture = doc.RootElement.GetProperty("userinfo").GetProperty("picture");
            Assert.AreEqual(System.Text.Json.JsonValueKind.Null, picture.ValueKind, "null claim value must be preserved.");
        }

        [TestMethod]
        public void NormalizeClaimsJson_Idempotent()
        {
            // Arrange — complex real-world claims value
            string input = @"{""z"":{""essential"":true},""a"":{""values"":[""v2"",""v1""]},""m"":null}";

            // Act
            string once = ClaimsHelper.NormalizeClaimsJson(input);
            string twice = ClaimsHelper.NormalizeClaimsJson(once);

            // Assert
            Assert.AreEqual(once, twice, "Normalize(Normalize(x)) must equal Normalize(x).");
        }

        [TestMethod]
        public void NormalizeClaimsJson_UriNamedClaim_IsHandled()
        {
            // Arrange — URI-named claim (valid per OIDC §5.5)
            string input = @"{""http://example.info/claims/groups"":{""essential"":true}}";

            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(input);

            // Assert — round-trips cleanly
            using var doc = System.Text.Json.JsonDocument.Parse(result);
            Assert.IsTrue(doc.RootElement.TryGetProperty("http://example.info/claims/groups", out _),
                "URI-named claim key must survive normalization.");
        }

        [TestMethod]
        public void NormalizeClaimsJson_CombinedUserinfoAndIdToken_BothKeysPresent()
        {
            // Arrange — canonical OIDC §5.5 shape with both top-level sections
            string input = @"{""userinfo"":{""given_name"":{""essential"":true},""email"":null},""id_token"":{""auth_time"":{""essential"":true},""acr"":{""values"":[""urn:mace:incommon:iap:silver""]}}}";

            // Act
            string result = ClaimsHelper.NormalizeClaimsJson(input);

            // Assert — both top-level keys survive, sorted (id_token < userinfo)
            using var doc = System.Text.Json.JsonDocument.Parse(result);
            Assert.IsTrue(doc.RootElement.TryGetProperty("userinfo", out _), "userinfo must be present.");
            Assert.IsTrue(doc.RootElement.TryGetProperty("id_token", out _), "id_token must be present.");

            // id_token sorts before userinfo
            Assert.IsLessThan(result.IndexOf("userinfo", StringComparison.Ordinal), result.IndexOf("id_token", StringComparison.Ordinal),
                "id_token must appear before userinfo after ordinal key sort.");
        }

        #endregion
    }
}
