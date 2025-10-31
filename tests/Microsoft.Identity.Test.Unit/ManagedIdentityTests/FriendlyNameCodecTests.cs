// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class FriendlyNameCodecTests
    {
        [TestMethod]
        public void EncodeDecode_RoundTrip_Succeeds()
        {
            var alias = "alias-" + System.Guid.NewGuid().ToString("N");
            var ep = "https://example.test/tenant";

            Assert.IsTrue(FriendlyNameCodec.TryEncode(alias, ep, out var fn));
            Assert.IsNotNull(fn);
            StringAssert.StartsWith(fn, FriendlyNameCodec.Prefix);

            Assert.IsTrue(FriendlyNameCodec.TryDecode(fn, out var a2, out var ep2));
            Assert.AreEqual(alias, a2);
            Assert.AreEqual(ep, ep2);
        }

        [TestMethod]
        public void TryEncode_Rejects_IllegalChars()
        {
            // '|' and newline are disallowed
            Assert.IsFalse(FriendlyNameCodec.TryEncode("foo|bar", "https://x", out _));
            Assert.IsFalse(FriendlyNameCodec.TryEncode("foo\nbar", "https://x", out _));
            Assert.IsFalse(FriendlyNameCodec.TryEncode("foo", "https://x|y", out _));
            Assert.IsFalse(FriendlyNameCodec.TryEncode("foo", "https://x\ny", out _));

            // Null/whitespace rejected
            Assert.IsFalse(FriendlyNameCodec.TryEncode(null, "https://x", out _));
            Assert.IsFalse(FriendlyNameCodec.TryEncode("   ", "https://x", out _));
            Assert.IsFalse(FriendlyNameCodec.TryEncode("foo", null, out _));
            Assert.IsFalse(FriendlyNameCodec.TryEncode("foo", "  ", out _));
        }

        [TestMethod]
        public void TryDecode_InvalidPrefix_ReturnsFalse()
        {
            // Wrong prefix
            var bad = "NOTMSAL|alias=a|ep=b";
            Assert.IsFalse(FriendlyNameCodec.TryDecode(bad, out _, out _));

            // Missing tags
            var missing = FriendlyNameCodec.Prefix + "alias=a";
            Assert.IsFalse(FriendlyNameCodec.TryDecode(missing, out _, out _));
        }
    }
}
