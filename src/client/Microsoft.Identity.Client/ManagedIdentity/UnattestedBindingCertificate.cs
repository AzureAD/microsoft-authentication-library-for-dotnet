// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
#if !NET8_0_OR_GREATER && !NET472
using System.Formats.Asn1;
using System.Runtime.InteropServices;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal static class UnattestedBindingCertificate
    {
        internal static X509Certificate2 Create()
        {
            try
            {
                using RSA key = InMemoryManagedIdentityKeyProvider.CreateRsaKeyPair();
                var subject = new X500DistinguishedName("CN=MSAL workload binding");
                DateTimeOffset now = DateTimeOffset.UtcNow;
#if NET8_0_OR_GREATER || NET472
                var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                    subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
                return request.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(30));
#else
                // CertificateRequest.CreateSelfSigned is unavailable in the netstandard2.0 reference API.
                var writer = new AsnWriter(AsnEncodingRules.DER);
                using (writer.PushSequence())
                {
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, true)))
                    {
                        writer.WriteInteger(2);
                    }
                    writer.WriteIntegerUnsigned(Guid.NewGuid().ToByteArray());
                    WriteSignatureAlgorithm(writer);
                    writer.WriteEncodedValue(subject.RawData);
                    using (writer.PushSequence())
                    {
                        writer.WriteUtcTime(now.AddMinutes(-5));
                        writer.WriteUtcTime(now.AddDays(30));
                    }
                    writer.WriteEncodedValue(subject.RawData);
                    using (writer.PushSequence())
                    {
                        using (writer.PushSequence())
                        {
                            writer.WriteObjectIdentifier("1.2.840.113549.1.1.1");
                            writer.WriteNull();
                        }
                        var publicKey = new AsnWriter(AsnEncodingRules.DER);
                        using (publicKey.PushSequence())
                        {
                            var parameters = key.ExportParameters(false);
                            publicKey.WriteIntegerUnsigned(parameters.Modulus);
                            publicKey.WriteIntegerUnsigned(parameters.Exponent);
                        }
                        writer.WriteBitString(publicKey.Encode());
                    }
                }
                byte[] tbs = writer.Encode();
                writer.Reset();
                using (writer.PushSequence())
                {
                    writer.WriteEncodedValue(tbs);
                    WriteSignatureAlgorithm(writer);
                    writer.WriteBitString(key.SignData(tbs, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                }
                var certificate = new X509Certificate2(writer.Encode());
                try
                {
                    // The Windows netstandard2.0 path cannot call CopyWithPrivateKey. Associate
                    // the persisted CNG key without exporting it or using the legacy PrivateKey setter.
                    var cngKey = ((RSACng)key).Key;
                    var provider = new KeyProviderInfo
                    {
                        ContainerName = cngKey.KeyName,
                        ProviderName = cngKey.Provider.Provider,
                        Flags = cngKey.IsMachineKey ? 0x20u : 0u,
                        KeySpec = uint.MaxValue
                    };
                    if (!CertSetCertificateContextProperty(certificate.Handle, 2, 0, ref provider))
                    {
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    }
                    return certificate;
                }
                catch
                {
                    certificate.Dispose();
                    throw;
                }
#endif
            }
            catch (CryptographicException ex)
            {
                throw new MsalClientException(MsalError.CryptographicError,
                    "The workload binding certificate could not be created.", ex);
            }
        }

#if !NET8_0_OR_GREATER && !NET472
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct KeyProviderInfo
        {
            public string ContainerName;
            public string ProviderName;
            public uint ProviderType;
            public uint Flags;
            public uint ParameterCount;
            public IntPtr Parameters;
            public uint KeySpec;
        }

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CertSetCertificateContextProperty(
            IntPtr certificate, uint propertyId, uint flags, ref KeyProviderInfo provider);

        private static void WriteSignatureAlgorithm(AsnWriter writer)
        {
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier("1.2.840.113549.1.1.11");
                writer.WriteNull();
            }
        }
#endif
    }
}
