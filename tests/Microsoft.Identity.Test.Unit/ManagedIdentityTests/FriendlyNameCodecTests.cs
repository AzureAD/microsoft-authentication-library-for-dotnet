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

            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryEncode(alias, ep, out var fn));
            Assert.IsNotNull(fn);
            StringAssert.StartsWith(fn, MsiCertificateFriendlyNameEncoder.Prefix);

            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryDecode(fn, out var a2, out var ep2));
            Assert.AreEqual(alias, a2);
            Assert.AreEqual(ep, ep2);
        }

        [DataTestMethod]
        [DataRow("foo|bar", "https://x")]
        [DataRow("foo\nbar", "https://x")]
        [DataRow("foo", "https://x|y")]
        [DataRow("foo", "https://x\ny")]
        [DataRow(null, "https://x")]
        [DataRow("   ", "https://x")]
        [DataRow("foo", null)]
        [DataRow("foo", "  ")]
        [DataRow("bad|alias", "https://ok")]
        [DataRow("ok", "https://bad|ep")]
        public void TryEncode_Rejects_IllegalInputs(string alias, string endpointBase)
        {
            Assert.IsFalse(MsiCertificateFriendlyNameEncoder.TryEncode(alias, endpointBase, out _));
        }

        [TestMethod]
        public void TryDecode_InvalidPrefix_ReturnsFalse()
        {
            // Wrong prefix
            var bad = "NOTMSAL|alias=a|ep=b";
            Assert.IsFalse(MsiCertificateFriendlyNameEncoder.TryDecode(bad, out _, out _));

            // Missing tags
            var missing = MsiCertificateFriendlyNameEncoder.Prefix + "alias=a";
            Assert.IsFalse(MsiCertificateFriendlyNameEncoder.TryDecode(missing, out _, out _));
        }

        [TestMethod]
        public void Decode_Ignores_Unknown_Tags_LastWins()
        {
            var fn = MsiCertificateFriendlyNameEncoder.Prefix +
                     MsiCertificateFriendlyNameEncoder.TagAlias + "=a|" +
                     "xtra=foo|" +
                     MsiCertificateFriendlyNameEncoder.TagEp + "=E";
            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryDecode(fn, out var a, out var e));
            Assert.AreEqual("a", a);
            Assert.AreEqual("E", e);
        }

        [TestMethod]
        public void EncodeDecode_VeryLongAliasAndEndpoint_Succeeds()
        {
            var alias = new string('a', 2048);
            var ep = "https://example.test/" + new string('b', 2048);

            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryEncode(alias, ep, out var fn));
            Assert.IsNotNull(fn);

            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryDecode(fn, out var a2, out var ep2));
            Assert.AreEqual(alias, a2);
            Assert.AreEqual(ep, ep2);
        }

        [TestMethod]
        public void EncodeDecode_UnicodeAliasAndEndpoint_Succeeds()
        {
            var alias = "uami-ümläüt-用户-🔐";
            var ep = "https://例え.テスト/路径/ресурс";

            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryEncode(alias, ep, out var fn));
            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryDecode(fn, out var a2, out var ep2));

            Assert.AreEqual(alias, a2);
            Assert.AreEqual(ep, ep2);
        }

        [TestMethod]
        public void TryDecode_DoublePrefixedString_IsResilient()
        {
            // First, build a normal friendly name.
            var alias = "alias-double";
            var ep = "https://ep/base";
            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryEncode(alias, ep, out var inner));

            // Now create a "double-prefixed" string: "MSAL|MSAL|alias=...|ep=..."
            var doublePrefixed = MsiCertificateFriendlyNameEncoder.Prefix + inner;

            // Decoder should not throw and should still recover alias/endpoint.
            Assert.IsTrue(MsiCertificateFriendlyNameEncoder.TryDecode(doublePrefixed, out var a2, out var ep2));
            Assert.AreEqual(alias, a2);
            Assert.AreEqual(ep, ep2);
        }
    }
}
