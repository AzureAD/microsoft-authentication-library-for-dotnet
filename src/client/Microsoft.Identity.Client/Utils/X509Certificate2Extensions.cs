// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Utils
{
#if !NET6_0_OR_GREATER

    internal static class X509Certificate2Extensions
    {
        /// <summary>
        /// Extension method to compute the cert SHA256 thumbprint bytes on .NET FWK, as per https://github.com/dotnet/runtime/issues/20349
        /// </summary>
        public static byte[] GetCertHash(this X509Certificate certificate, HashAlgorithm alg)
        {
            return alg.ComputeHash(certificate.GetRawCertData());
        }

        /// <summary>
        /// Extension method to compute the cert SHA256 thumbprint string on .NET FWK. This computes the byte[], as per https://github.com/dotnet/runtime/issues/20349
        /// then transforms it to a hex uppercase string. The transformation code is inspired from https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/26304129#26304129
        /// and has been benchmarked.
        /// </summary>
        public static string GetCertHashString(this X509Certificate certificate, HashAlgorithm alg, int algo)
        {
            byte[] hashBytes = certificate.GetCertHash(alg);

            switch (algo)
            {
                case 1:
                    return ByteArrayToString1(hashBytes);
                case 2:
                    return ByteArrayToString2(hashBytes);
                case 3:
                    return ByteArrayToString3(hashBytes);
                case 4:
                    return ByteArrayToString4(hashBytes);
                default:
                    return ByteArrayToString1(hashBytes);
            }

        }

        private static string ByteArrayToString1(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private static string ByteArrayToString2(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        private static string ByteArrayToString3(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", string.Empty).ToUpper();

        }

        private static string ByteArrayToString4(byte[] ba)
        {
            return Base64UrlHelpers.Encode(ba);
        }
    }
#endif

}
