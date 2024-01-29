// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public class X509Certificate2Builder
    {
        private readonly RSA _rsa;
        private string _subjectName;
        private DateTimeOffset _notBefore;
        private DateTimeOffset _notAfter;

        private HashAlgorithmName _hashAlgorithm;
        private RSASignaturePadding _signatureAlgorithm;
        private bool _includeBasicConstraintsExtension;
        private bool _includeSubjectKeyIdentifierExtension;

        public X509Certificate2Builder(int keySize = 2048)
        {
            _rsa = RSA.Create(keySize);
            _notBefore = DateTimeOffset.UtcNow;
            _notAfter = _notBefore.AddDays(365); // Valid for one year by default
            _hashAlgorithm = HashAlgorithmName.SHA256;
            _signatureAlgorithm = RSASignaturePadding.Pkcs1;
            _includeBasicConstraintsExtension = true;
            _includeSubjectKeyIdentifierExtension = true;
        }

        public X509Certificate2Builder WithSubjectName(string subjectName)
        {
            _subjectName = subjectName;
            return this;
        }

        public X509Certificate2Builder ValidityPeriod(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            _notBefore = notBefore;
            _notAfter = notAfter;
            return this;
        }

        public X509Certificate2Builder WithNotBefore(DateTimeOffset notBefore)
        {
            _notBefore = notBefore;
            return this;
        }

        public X509Certificate2Builder WithNotAfter(DateTimeOffset notAfter)
        {
            _notAfter = notAfter;
            return this;
        }

        public X509Certificate2Builder WithPublicKey(RSA publicKey)
        {
            _rsa.ImportParameters(publicKey.ExportParameters(false));
            return this;
        }

        public X509Certificate2Builder WithHashAlgorithm(HashAlgorithmName hashAlgorithm)
        {
            _hashAlgorithm = hashAlgorithm;
            return this;
        }

        public X509Certificate2Builder WithSignatureAlgorithm(RSASignaturePadding signatureAlgorithm)
        {
            _signatureAlgorithm = signatureAlgorithm;
            return this;
        }

        public X509Certificate2Builder WithBasicConstraintsExtension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint, bool critical)
        {
            _includeBasicConstraintsExtension = true;
            return this;
        }

        public X509Certificate2Builder WithSubjectKeyIdentifierExtension(bool critical)
        {
            _includeSubjectKeyIdentifierExtension = true;
            return this;
        }

        public X509Certificate2 Build()
        {
            if (string.IsNullOrEmpty(_subjectName))
            {
                throw new InvalidOperationException("Subject name cannot be null or empty.");
            }

            var request = new CertificateRequest(
                $"CN={_subjectName}",
                _rsa,
                _hashAlgorithm,
                _signatureAlgorithm);

            if (_includeBasicConstraintsExtension)
            {
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));
            }

            if (_includeSubjectKeyIdentifierExtension)
            {
                request.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            }

            return request.CreateSelfSigned(_notBefore, _notAfter);
        }
    }
}
