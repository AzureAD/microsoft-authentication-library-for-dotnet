// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Identity.Client.Internal.Credential
{
    internal static class CsrHelper
    {
        private static readonly Oid s_challengePwdOid = new("1.2.840.113549.1.9.7");

        internal static string Build(string clientId, string tenantId, string cuid, KeyMaterial km)
        {
#if NET462 || NETSTANDARD2_0
            return BuildFallback(clientId, tenantId, cuid, km.Rsa);
#else
            return BuildWithCertificateRequest(clientId, tenantId, cuid, km.Rsa);
#endif
        }

#if !NET462 && !NETSTANDARD2_0
        // ---------- modern BCL path ----------------------------------------
        private static string BuildWithCertificateRequest(string clientId, string tenantId, string cuid, RSA rsa)
        {
            var dn  = new X500DistinguishedName($"CN={clientId},DC={tenantId}");
            var req = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var cuidRaw = new AsnEncodedData(s_challengePwdOid, Encoding.ASCII.GetBytes(cuid));
            req.CertificateExtensions.Add(new X509Extension(cuidRaw, false));

            return Convert.ToBase64String(req.CreateSigningRequest());
        }
#endif

#if NET462 || NETSTANDARD2_0
        // ---------- fallback path (manual DER writer) ----------------------
        private static string BuildFallback(string clientId, string tenantId, string cuid, RSA rsa)
        {
            byte[] subject = EncodeSubject($"CN={clientId},DC={tenantId}");
            byte[] spki = EncodeSpki(rsa);

            byte[] pwdAttr = Asn.Sequence(
                                Asn.ObjectId("1.2.840.113549.1.9.7"),
                                Asn.Set(Asn.PrintableString(cuid)));

            byte[] attrs = Asn.Context0(Asn.Set(pwdAttr));

            byte[] cri = Asn.Sequence(
                             Asn.Integer(0),
                             subject,
                             spki,
                             attrs);

            byte[] hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = sha.ComputeHash(cri);
            }

            byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] algoId = Asn.Sequence(
                                Asn.ObjectId("1.2.840.113549.1.1.11"),   // sha256WithRSA
                                Asn.Null());

            byte[] csr = Asn.Sequence(cri, algoId, Asn.BitString(sig));
            return Convert.ToBase64String(csr);
        }

        // ---- tiny DER helpers ---------------------------------------------
        private static byte[] EncodeSubject(string rdns)
        {
            string[] parts = rdns.Split(',');
            var rdnSet = new byte[parts.Length][];
            for (int i = 0; i < parts.Length; i++)
            {
                string[] kv = parts[i].Split('=');
                string oid = kv[0].Trim().ToUpperInvariant() == "CN"
                           ? "2.5.4.3"
                           : "0.9.2342.19200300.100.1.25"; // DC
                rdnSet[i] = Asn.Sequence(
                                Asn.ObjectId(oid),
                                Asn.PrintableString(kv[1].Trim()));
            }
            return Asn.Sequence(Asn.Set(rdnSet));
        }

        private static byte[] EncodeSpki(RSA rsa)
        {
            RSAParameters p = rsa.ExportParameters(false);
            byte[] pubKey = Asn.Sequence(
                                Asn.Integer(p.Modulus, unsigned: true),
                                Asn.Integer(p.Exponent, unsigned: true));

            byte[] algoId = Asn.Sequence(
                                Asn.ObjectId("1.2.840.113549.1.1.1"),
                                Asn.Null());

            return Asn.Sequence(algoId, Asn.BitString(pubKey));
        }

        // ---- minimal ASN.1 writer -----------------------------------------
        private static class Asn
        {
            internal static byte[] Integer(int v) => Integer(ToBigEndian(v));
            internal static byte[] Integer(byte[] be, bool unsigned = false)
            {
                if (unsigned && be[0] >= 0x80)
                {
                    var tmp = new byte[be.Length + 1];
                    Buffer.BlockCopy(be, 0, tmp, 1, be.Length);
                    be = tmp;
                }
                return Write(0x02, be);
            }

            internal static byte[] ObjectId(string dotted) => Write(0x06, OidBytes(dotted));
            internal static byte[] PrintableString(string s) => Write(0x13, Encoding.ASCII.GetBytes(s));
            internal static byte[] Null() => Write(0x05, Array.Empty<byte>());
            internal static byte[] BitString(byte[] raw)
            {
                var buf = new byte[raw.Length + 1];
                Buffer.BlockCopy(raw, 0, buf, 1, raw.Length);
                return Write(0x03, buf);
            }

            internal static byte[] Set(params byte[][] elts) => Constructed(0x31, elts);
            internal static byte[] Sequence(params byte[][] elts) => Constructed(0x30, elts);
            internal static byte[] Context0(byte[] content) => Write(0xA0, content);

            // -- internals --
            private static byte[] Constructed(byte tag, byte[][] elts)
            {
                int len = 0;
                foreach (var e in elts)
                    len += e.Length;
                var data = new byte[len];
                int offset = 0;
                foreach (var e in elts)
                { Buffer.BlockCopy(e, 0, data, offset, e.Length); offset += e.Length; }
                return Write(tag, data);
            }

            private static byte[] Write(byte tag, byte[] data)
            {
                byte[] len = EncodeLen(data.Length);
                var ret = new byte[1 + len.Length + data.Length];
                ret[0] = tag;
                Buffer.BlockCopy(len, 0, ret, 1, len.Length);
                Buffer.BlockCopy(data, 0, ret, 1 + len.Length, data.Length);
                return ret;
            }

            private static byte[] EncodeLen(int length)
            {
                if (length < 0x80)
                    return new[] { (byte)length };
                byte[] tmp = ToBigEndian(length);
                var len = new byte[tmp.Length + 1];
                len[0] = (byte)(0x80 + tmp.Length);
                Buffer.BlockCopy(tmp, 0, len, 1, tmp.Length);
                return len;
            }

            private static byte[] ToBigEndian(int v)
            {
                byte[] tmp = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(tmp);
                int idx = Array.FindIndex(tmp, b => b != 0);
                if (idx < 0)
                    idx = tmp.Length - 1;
                var res = new byte[tmp.Length - idx];
                Buffer.BlockCopy(tmp, idx, res, 0, res.Length);
                return res;
            }

            private static byte[] OidBytes(string dotted)
            {
                string[] parts = dotted.Split('.');
                int first = int.Parse(parts[0]) * 40 + int.Parse(parts[1]);
                var bytes = new System.Collections.Generic.List<byte> { (byte)first };
                for (int i = 2; i < parts.Length; i++)
                {
                    uint v = uint.Parse(parts[i]);
                    var stack = new System.Collections.Generic.Stack<byte>();
                    stack.Push((byte)(v & 0x7F));
                    while ((v >>= 7) > 0)
                        stack.Push((byte)(0x80 | (v & 0x7F)));
                    bytes.AddRange(stack);
                }
                return bytes.ToArray();
            }
        }
#endif // NET462 || NETSTANDARD2_0
    }
}
