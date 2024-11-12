// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;
using NSubstitute;

namespace Microsoft.Identity.Test.Performance
{
    [SimpleJob(RuntimeMoniker.Net471)]
    [SimpleJob(RuntimeMoniker.Net462)]
    //[SimpleJob(RuntimeMoniker.Net80)]
    public class CertificateSha256Thumbprint
    {
        private X509Certificate2 _certificate;

        public CertificateSha256Thumbprint()
        {
            _certificate = CertificateHelper.FindCertificateByName(
               TestConstants.AutomationTestCertName,
               StoreLocation.CurrentUser,
               StoreName.My);
        }

        [Params(1, 2)]
        public int AlgoId { get; set; }


        [Benchmark]
        public void Algo1()
        {
            _certificate.GetX5TSha256(AlgoId);
        }
    }

    public static class X509Certificate2Helper
    {
        public static string GetX5TSha256(this X509Certificate2 certificate, int algo)
        {
#if NET6_0_OR_GREATER
            byte[] hash = certificate.GetCertHash(HashAlgorithmName.SHA256);
            return Base64UrlHelpers.Encode(hash);
#else
            using (var hasher = SHA256.Create())
            {
                byte[] hashBytes = hasher.ComputeHash(certificate.RawData);

                switch (algo)
                {

                    case 1:
                        return ByteArrayToString1(hashBytes);
                    case 2:
                        return ByteArrayToString2(hashBytes);
                    default:
                        throw new NotImplementedException();
                }
            }
#endif
        }
        private static string ByteArrayToString1(byte[] ba)
        {
            return Base64UrlHelpers.Encode(ba);
        }

        private static string ByteArrayToString2(byte[] ba)
        {
            byte[] bytes = Decode(BitConverter.ToString(ba).Replace("-", string.Empty).ToLowerInvariant());
            return Base64UrlHelpers.Encode(bytes);
        }

        private static byte[] Decode(string hexString)
        {
            if (hexString == null)
            {
                // Equivalent of assert. Not expected at runtime because higher layers should handle this.
                throw new NullReferenceException(nameof(hexString));
            }

            byte[] bytes = new byte[hexString.Length >> 1];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i << 1, 2), 16);
            }

            return bytes;
        }
    }
}

