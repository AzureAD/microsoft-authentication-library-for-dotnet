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
    }
}