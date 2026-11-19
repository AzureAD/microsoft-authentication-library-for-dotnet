// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal static class TseCertificateHelper
    {
        internal static X509Certificate2 FindCertificate(Uri identity)
        {
            if (identity == null || !identity.IsAbsoluteUri)
                throw new ArgumentException("An absolute URI SAN identity is required.", nameof(identity));

            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var certificates = store.Certificates.Cast<X509Certificate2>().ToArray();
            X509Certificate2 selected = null;
            bool returned = false;
            try
            {
                selected = SelectCertificate(certificates, identity.OriginalString, DateTime.UtcNow);
                if (selected == null)
                    throw new InvalidOperationException($"Install a valid private-key certificate with URI SAN '{identity}' in LocalMachine/My.");

                using AsymmetricAlgorithm key = selected.GetRSAPrivateKey() ?? (AsymmetricAlgorithm)selected.GetECDsaPrivateKey();
                if (key == null)
                    throw new InvalidOperationException($"The certificate for URI SAN '{identity}' has no usable RSA or ECDSA private key.");

                returned = true;
                return selected;
            }
            finally
            {
                foreach (var certificate in certificates)
                {
                    if (!returned || !ReferenceEquals(certificate, selected))
                        certificate.Dispose();
                }
            }
        }

        internal static X509Certificate2 SelectCertificate(IEnumerable<X509Certificate2> certificates, string identity, DateTime utcNow)
        {
            return certificates.Where(certificate => certificate.HasPrivateKey
                    && certificate.NotBefore.ToUniversalTime() <= utcNow
                    && certificate.NotAfter.ToUniversalTime() > utcNow
                    && HasUriSan(certificate, identity))
                .OrderByDescending(certificate => certificate.NotBefore)
                .ThenByDescending(certificate => certificate.NotAfter)
                .FirstOrDefault();
        }

        private static bool HasUriSan(X509Certificate2 certificate, string identity)
        {
            var extension = certificate.Extensions["2.5.29.17"];
            if (extension == null)
                return false;

            var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
            var sequence = reader.ReadSequence();
            bool matches = false;
            while (sequence.HasData)
            {
                var tag = sequence.PeekTag();
                if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 6)))
                    matches |= string.Equals(sequence.ReadCharacterString(UniversalTagNumber.IA5String, tag), identity, StringComparison.Ordinal);
                else
                    sequence.ReadEncodedValue();
            }

            reader.ThrowIfNotEmpty();
            return matches;
        }
    }
}
