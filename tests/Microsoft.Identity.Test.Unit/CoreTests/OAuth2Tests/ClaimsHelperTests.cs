// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        #endregion
    }
}
