// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity.V2;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class CertHelper
    {
        private static Dictionary<KnownTestCertType, X509Certificate2> s_x509Certificates = new Dictionary<KnownTestCertType, X509Certificate2>();

        public static X509Certificate2 GetOrCreateTestCert(KnownTestCertType knownTestCertType = KnownTestCertType.RSA, bool regenerateCert = false)
        {
            // create the cert if it doesn't exist. use a lock to prevent multiple threads from creating the cert
            s_x509Certificates.TryGetValue(knownTestCertType, out X509Certificate2 x509Certificate2);

            if (x509Certificate2 == null || regenerateCert)
            {
                lock (typeof(CertHelper))
                {
                    if (x509Certificate2 != null)
                    {
                        x509Certificate2 = CreateTestCert(knownTestCertType);
                        s_x509Certificates[knownTestCertType] = x509Certificate2;
                    }
                    else
                    {
                        x509Certificate2 = CreateTestCert(knownTestCertType);
                        s_x509Certificates.Add(knownTestCertType, x509Certificate2);
                    }
                }
            }

            return x509Certificate2;
        }

        private static X509Certificate2 CreateTestCert(KnownTestCertType knownTestCertType = KnownTestCertType.RSA)
        {
            switch (knownTestCertType)
            {
                case KnownTestCertType.ECD:
                    using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
                    {
                        string subjectName = "SelfSignedEdcCert";

                        var certRequest = new System.Security.Cryptography.X509Certificates.CertificateRequest($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);

                        X509Certificate2 generatedCert = certRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10)); // generate the cert and sign!

                        X509Certificate2 pfxGeneratedCert = new X509Certificate2(generatedCert.Export(X509ContentType.Pfx)); //has to be turned into PFX or Windows at least throws a security credentials not found during sslStream.connectAsClient or HttpClient request...

                        return pfxGeneratedCert;
                    }
                case KnownTestCertType.RSA:
                default:
                    using (RSA rsa = RSA.Create(4096))
                    {
                        var parentReq = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                            "CN=Test Cert",
                            rsa,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1);

                        parentReq.CertificateExtensions.Add(
                            new X509BasicConstraintsExtension(true, false, 0, true));

                        parentReq.CertificateExtensions.Add(
                            new X509SubjectKeyIdentifierExtension(parentReq.PublicKey, false));

                        X509Certificate2 cert = parentReq.CreateSelfSigned(
                             DateTimeOffset.UtcNow,
                             DateTimeOffset.UtcNow.AddDays(1));

                        return cert;
                    }
            }
        }

        public static X509Certificate2 CreateSelfSigned(string subjectDn, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using var rsa = RSA.Create(2048);
            var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                new X500DistinguishedName(subjectDn),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Create the self-signed certificate
            var cert = req.CreateSelfSigned(notBefore, notAfter);

            // On some runtimes, CreateSelfSigned already associates the private key.
            // On others it doesn't; attach if needed.
            if (!cert.HasPrivateKey)
            {
                cert = cert.CopyWithPrivateKey(rsa);
            }

            // (Recommended for stability) Re-import as PFX with PersistKeySet so the key survives
            // across process/store operations, especially on Windows test runners.
            var pfx = cert.Export(X509ContentType.Pkcs12);
            var persisted = new X509Certificate2(
                pfx,
                (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            return persisted;
        }

        public static X509Certificate2 CreateShortLivedCert(string subjectDn, TimeSpan lifetime)
        {
            var nb = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromTicks(lifetime.Ticks / 2));
            var na = DateTimeOffset.UtcNow.Add(TimeSpan.FromTicks(lifetime.Ticks / 2));
            return CreateSelfSigned(subjectDn, nb, na);
        }

        public static void RemoveBySubjectPrefix(string prefix)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            foreach (var c in store.Certificates.OfType<X509Certificate2>())
            {
                if (!string.IsNullOrEmpty(c.Subject) && c.Subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    { store.Remove(c); }
                    catch { /* best-effort */ }
                }
            }
        }

        public static bool ExistsByThumbprint(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false).Count > 0;
        }

        public static async Task<bool> WaitForThumbprintChangeAsync(
            string identityKey,
            string tokenType,
            string oldThumbprint,
            TimeSpan timeout)
        {
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < timeout)
            {
                if (ImdsV2ManagedIdentitySource.TryGetImdsV2BindingMetadata(identityKey, tokenType, out _, out _, out var tp))
                {
                    if (!string.IsNullOrEmpty(tp) &&
                        !string.Equals(tp, oldThumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                await Task.Delay(50).ConfigureAwait(false);
            }
            return false;
        }

        internal static CertificateRequestResponse MakeResp(
            X509Certificate2 cert,
            string endpoint = "https://fake-mtls-endpoint",
            string tenant = "t1",
            string client = "c1")
        {
            return new CertificateRequestResponse
            {
                // These 3 are used elsewhere but for mapping tests they are just stored
                MtlsAuthenticationEndpoint = endpoint,
                TenantId = tenant,
                ClientId = client,
                // For our tests we also set the certificate to something real (Base64 DER, as required)
                Certificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert))
            };
        }
    }

    public enum KnownTestCertType
    {
        RSA,
        ECD
    }
}
