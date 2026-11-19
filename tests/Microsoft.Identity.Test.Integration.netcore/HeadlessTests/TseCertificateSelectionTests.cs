// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class TseCertificateSelectionTests
    {
        private const string Identity = "urn:msal:test:managed-certificate";

        [TestMethod]
        public void Rotation_SelectsReplacementWithoutChangingIdentity()
        {
            using var older = CreateCertificate(Identity, -10, 30);
            using var replacement = CreateCertificate(Identity, -1, 20);
            using var unrelated = CreateCertificate(Identity + ":other", -0.5, 40);

            Assert.AreSame(older, TseCertificateHelper.SelectCertificate(new[] { older }, Identity, DateTime.UtcNow));
            Assert.AreSame(replacement, TseCertificateHelper.SelectCertificate(
                new[] { unrelated, older, replacement }, Identity, DateTime.UtcNow));
        }

        [TestMethod]
        public void Selection_RejectsExpiredFutureAndPublicOnlyCertificates()
        {
            using var expired = CreateCertificate(Identity, -10, -1);
            using var future = CreateCertificate(Identity, 1, 10);
            using var valid = CreateCertificate(Identity, -1, 10);
            using var publicOnly = new X509Certificate2(valid.RawData);

            Assert.IsNull(TseCertificateHelper.SelectCertificate(
                new[] { expired, future, publicOnly }, Identity, DateTime.UtcNow));
        }

        [TestMethod]
        public void Selection_DoesNotMatchSubjectOrDnsName()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest($"CN={Identity}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName("msal-test.example");
            request.CertificateExtensions.Add(names.Build());
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

            Assert.IsNull(TseCertificateHelper.SelectCertificate(new[] { certificate }, Identity, DateTime.UtcNow));
        }

        private static X509Certificate2 CreateCertificate(string identity, double notBeforeDays, double notAfterDays)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest($"CN=MSAL-{Guid.NewGuid():N}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder();
            names.AddUri(new Uri(identity));
            request.CertificateExtensions.Add(names.Build());
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(notBeforeDays), DateTimeOffset.UtcNow.AddDays(notAfterDays));
        }
    }
}
