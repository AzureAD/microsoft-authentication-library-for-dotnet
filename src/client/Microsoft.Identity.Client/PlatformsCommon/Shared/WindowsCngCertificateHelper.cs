// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NETSTANDARD
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class WindowsCngCertificateHelper
    {
        private const uint KeyProviderInfoProperty = 2;
        private const uint MachineKeyFlag = 0x20;

        internal static X509Certificate2 AttachPrivateKey(X509Certificate2 certificate, RSACng privateKey)
        {
            CngKey key = privateKey.Key;
            using (RSA publicKey = certificate.GetRSAPublicKey())
            {
                ValidateMatchingKey(publicKey, privateKey);
            }

            var attached = new X509Certificate2(certificate.RawData);
            try
            {
                var providerInfo = new KeyProviderInfo
                {
                    ContainerName = key.KeyName,
                    ProviderName = key.Provider.Provider,
                    Flags = key.IsMachineKey ? MachineKeyFlag : 0
                    // ProviderType and KeySpec are zero for this CNG software provider.
                };

                if (!CertSetCertificateContextProperty(attached.Handle, KeyProviderInfoProperty, 0, ref providerInfo))
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }

                // Reopen by name to detect unavailable or replaced containers before returning the certificate.
                using (RSA associatedKey = attached.GetRSAPrivateKey())
                {
                    ValidateMatchingKey(associatedKey, privateKey);
                }

                return attached;
            }
            catch
            {
                attached.Dispose();
                throw;
            }
            finally
            {
                GC.KeepAlive(attached);
            }
        }

        private static void ValidateMatchingKey(RSA certificateKey, RSA privateKey)
        {
            if (certificateKey is null)
            {
                throw new ArgumentException("The certificate must have an accessible RSA key.", nameof(certificateKey));
            }

            RSAParameters expected = certificateKey.ExportParameters(false);
            RSAParameters actual = privateKey.ExportParameters(false);
            if (!expected.Modulus.SequenceEqual(actual.Modulus) ||
                !expected.Exponent.SequenceEqual(actual.Exponent))
            {
                throw new ArgumentException("The certificate and private key do not match.", nameof(privateKey));
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct KeyProviderInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string ContainerName;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string ProviderName;
            internal uint ProviderType;
            internal uint Flags;
            internal uint ParameterCount;
            internal IntPtr Parameters;
            internal uint KeySpec;
        }

        [DllImport("crypt32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CertSetCertificateContextProperty(
            IntPtr certificate, uint property, uint flags, ref KeyProviderInfo data);
    }
}
#endif
