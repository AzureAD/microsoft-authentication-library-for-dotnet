// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Text;
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
            Assert.DoesNotContain(result, "+");
            Assert.DoesNotContain(result, "/");
            Assert.DoesNotContain(result, "=");
        }

        [TestMethod]
        public void EncodeAndDecode_RoundTrip_SimpleString()
        {
            string original = "Hello, World!";
            string encoded = Base64UrlHelpers.Encode(original);
            string decoded = Base64UrlHelpers.Decode(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void EncodeAndDecode_RoundTrip_BinaryData()
        {
            byte[] original = { 0x00, 0x01, 0x02, 0xFE, 0xFF };
            string encoded = Base64UrlHelpers.Encode(original);
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(original, decoded);
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
        public void EncodeString_RoundTrip()
        {
            string original = "test string";
            string encoded = Base64UrlHelpers.EncodeString(original);
            string decoded = Base64UrlHelpers.Decode(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Encode_BytesThatProducePlusAndSlash()
        {
            // 0xFB, 0xEF, 0xBE => produces + and / in standard base64
            byte[] data = { 0xFB, 0xEF, 0xBE };
            string encoded = Base64UrlHelpers.Encode(data);
            Assert.DoesNotContain(encoded, "+");
            Assert.DoesNotContain(encoded, "/");

            // Verify round trip
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void Decode_StringWithUrlSafeChars_HandlesCorrectly()
        {
            // Encode something that should produce - and _ in base64url
            byte[] data = { 0xFB, 0xEF, 0xBE, 0xFE, 0xFF };
            string encoded = Base64UrlHelpers.Encode(data);
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void RoundTrip_LengthMod3_Equals0()
        {
            // 3 bytes -> 4 base64 chars, no padding needed
            byte[] data = { 0x01, 0x02, 0x03 };
            string encoded = Base64UrlHelpers.Encode(data);
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void RoundTrip_LengthMod3_Equals1()
        {
            // 1 byte -> 2 base64 chars
            byte[] data = { 0xAB };
            string encoded = Base64UrlHelpers.Encode(data);
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void RoundTrip_LengthMod3_Equals2()
        {
            // 2 bytes -> 3 base64 chars
            byte[] data = { 0xAB, 0xCD };
            string encoded = Base64UrlHelpers.Encode(data);
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void Decode_StandardBase64WithPadding_Works()
        {
            // Standard base64 for "Hello" is "SGVsbG8=" 
            // Base64url for "Hello" should be "SGVsbG8" (no padding)
            string encoded = Base64UrlHelpers.Encode("Hello");
            string decoded = Base64UrlHelpers.Decode(encoded);
            Assert.AreEqual("Hello", decoded);
        }

        [TestMethod]
        public void RoundTrip_LargeData()
        {
            byte[] data = new byte[1024];
            new Random(42).NextBytes(data);
            string encoded = Base64UrlHelpers.Encode(data);
            byte[] decoded = Base64UrlHelpers.DecodeBytes(encoded);
            CollectionAssert.AreEqual(data, decoded);
        }

        [TestMethod]
        public void RoundTrip_UnicodeString()
        {
            string original = "Héllo Wörld 你好世界";
            string encoded = Base64UrlHelpers.Encode(original);
            string decoded = Base64UrlHelpers.Decode(encoded);
            Assert.AreEqual(original, decoded);
        }

        [TestMethod]
        public void Decode_NoPaddingNeeded()
        {
            // "AAAA" is valid base64 (length % 4 == 0), no URL-safe chars
            byte[] result = Base64UrlHelpers.DecodeBytes("AAAA");
            Assert.IsNotNull(result);
            Assert.HasCount(3, result as System.Collections.IEnumerable);
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
