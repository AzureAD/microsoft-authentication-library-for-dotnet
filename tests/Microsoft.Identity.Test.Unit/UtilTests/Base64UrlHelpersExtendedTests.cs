// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class Base64UrlHelpersExtendedTests: TestBase
    {
        [TestMethod]
        public void Encode_NullString_ReturnsNull()
        {
            Assert.IsNull(Base64UrlHelpers.Encode((string)null));
        }

        [TestMethod]
        public void Encode_NullByteArray_ReturnsNull()
        {
            Assert.IsNull(Base64UrlHelpers.Encode((byte[])null));
        }

        [TestMethod]
        public void Encode_EmptyString_ReturnsEmptyString()
        {
            string result = Base64UrlHelpers.Encode(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Encode_EmptyByteArray_ReturnsEmptyString()
        {
            string result = Base64UrlHelpers.Encode(Array.Empty<byte>());
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Encode_SimpleString_NoSpecialChars()
        {
            string result = Base64UrlHelpers.Encode("Hello");
            Assert.IsNotNull(result);
            // Should not contain +, /, or =
            Assert.DoesNotContain("+", result);
            Assert.DoesNotContain("/", result);
            Assert.DoesNotContain("=", result);
        }

        [TestMethod]
        public void DecodeBytes_NullInput_ReturnsNull()
        {
            Assert.IsNull(Base64UrlHelpers.DecodeBytes(null));
        }

        [TestMethod]
        public void DecodeBytes_InvalidLength_Throws()
        {
            // A string with length % 4 == 1 is invalid base64url
            Assert.Throws<FormatException>(() => Base64UrlHelpers.DecodeBytes("A"));
        }

        [TestMethod]
        public void EncodeString_NullInput_ReturnsNull()
        {
            Assert.IsNull(Base64UrlHelpers.EncodeString(null));
        }

        [TestMethod]
        public void Encode_BytesThatProducePlusAndSlash()
        {
            // 0xFB, 0xEF, 0xBE => produces + and / in standard base64
            byte[] data = { 0xFB, 0xEF, 0xBE };
            string encoded = Base64UrlHelpers.Encode(data);
            Assert.DoesNotContain("+", encoded);
            Assert.DoesNotContain("/", encoded);

            // Verify round trip
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void Decode_StandardBase64WithPadding_Works()
        {
            // Standard base64 for "Hello" is "SGVsbG8=" (with padding)
            // Verify the decoder handles standard base64 padding correctly
            byte[] result = Base64UrlHelpers.DecodeBytes("SGVsbG8=");
            Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(result));
        }

        [TestMethod]
        public void Decode_NoPaddingNeeded()
        {
            // "AAAA" is valid base64 (length % 4 == 0), no URL-safe chars
            byte[] result = Base64UrlHelpers.DecodeBytes("AAAA");
            Assert.IsNotNull(result);
            Assert.HasCount(3, result);
        }

        [TestMethod]
        public void Decode_WithOnePaddingNeeded()
        {
            // length % 4 == 3 -> needs 1 padding char
            byte[] data = { 0xAB, 0xCD };
            string encoded = Base64UrlHelpers.Encode(data);
            Assert.AreEqual(3, encoded.Length); // 2 bytes -> 3 base64url chars
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void Decode_WithTwoPaddingNeeded()
        {
            // length % 4 == 2 -> needs 2 padding chars
            byte[] data = { 0xAB };
            string encoded = Base64UrlHelpers.Encode(data);
            Assert.AreEqual(2, encoded.Length); // 1 byte -> 2 base64url chars
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }
    }
}
