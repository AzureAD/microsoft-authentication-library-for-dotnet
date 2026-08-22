// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.E2E.SniMtls
{
    [TestClass]
    public class SniMtlsPopTests
    {
        private const string SniClientId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string SniAuthority = "https://login.microsoftonline.com/bea21ebe-8b64-4d06-9f6d-6a889b120a7c";
        private const string SniCertificateSubjectName = "LabAuth.MSIDLab.com";
        private const string GraphScope = "https://graph.microsoft.com/.default";
        private const string GraphMtlsEndpoint = "https://mtlstb.graph.microsoft.com/v1.0/applications?$top=1";

        [RunOnAzureDevOps]
        [TestCategory("SNI_MTLS_E2E_NonExportableVbs")]
        [TestMethod]
        public async Task SniMtlsPop_WithNonExportableVbsKey_CallsGraphSuccessfully()
        {
            // Arrange
            using X509Certificate2 certificate = FindLocalMachineCertificate(SniCertificateSubjectName);
            ValidateNonExportableVbsKey(certificate);

            // Act
            (IConfidentialClientApplication confidentialApp, AuthenticationResult result) =
                await AcquireMtlsPopTokenAsync(certificate).ConfigureAwait(false);
            GraphResponse graphResponse = await CallGraphAsync(result, certificate).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.OK,
                graphResponse.StatusCode,
                "Graph mTLS request should succeed when the binding certificate is presented.");

            AuthenticationResult cachedResult = await confidentialApp
                .AcquireTokenForClient(new[] { GraphScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, cachedResult.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(result.AccessToken, cachedResult.AccessToken);
            Assert.IsNotNull(cachedResult.BindingCertificate, "Cached result should preserve the binding certificate.");
            CollectionAssert.AreEqual(
                certificate.RawData,
                cachedResult.BindingCertificate.RawData,
                "Cached result must remain bound to the non-exportable SNI certificate.");
        }

        [RunOnAzureDevOps]
        [TestCategory("SNI_MTLS_E2E_NonExportableVbs")]
        [TestMethod]
        public async Task SniMtlsPop_WithoutBindingCertificate_IsRejectedByGraph()
        {
            // Arrange
            using X509Certificate2 certificate = FindLocalMachineCertificate(SniCertificateSubjectName);
            ValidateNonExportableVbsKey(certificate);
            (_, AuthenticationResult result) = await AcquireMtlsPopTokenAsync(certificate).ConfigureAwait(false);

            // Act
            GraphResponse graphResponse = await CallGraphAsync(result, certificate: null).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(
                HttpStatusCode.Unauthorized,
                graphResponse.StatusCode,
                "Graph should reject an mTLS PoP token when the binding certificate is not presented.");
            Assert.IsTrue(
                graphResponse.WwwAuthenticateChallenges.Any(),
                "Graph should return a WWW-Authenticate challenge.");

            using JsonDocument responseJson = JsonDocument.Parse(graphResponse.ResponseBody);
            JsonElement error = responseJson.RootElement.GetProperty("error");
            Assert.AreEqual("InvalidAuthenticationToken", error.GetProperty("code").GetString());
            Assert.AreEqual("MtlsMissingClientCertificate", error.GetProperty("message").GetString());
        }

        private static async Task<(IConfidentialClientApplication Application, AuthenticationResult Result)> AcquireMtlsPopTokenAsync(
            X509Certificate2 certificate)
        {
            IConfidentialClientApplication confidentialApp = ConfidentialClientApplicationBuilder
                .Create(SniClientId)
                .WithAuthority(SniAuthority)
                .WithCertificate(certificate, sendX5C: true)
                .Build();

            AuthenticationResult result = await confidentialApp
                .AcquireTokenForClient(new[] { GraphScope })
                .WithMtlsProofOfPossession()
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken), "Access token should not be empty.");
            Assert.AreEqual("mtls_pop", result.TokenType, "Token type should be mTLS PoP.");
            Assert.IsNotNull(result.BindingCertificate, "Binding certificate should be returned for mTLS PoP.");
            CollectionAssert.AreEqual(
                certificate.RawData,
                result.BindingCertificate.RawData,
                "Binding certificate must match the non-exportable SNI certificate.");
            ValidateMtlsPopBinding(result.AccessToken, certificate);
            Assert.AreEqual(
                TokenSource.IdentityProvider,
                result.AuthenticationResultMetadata.TokenSource,
                "First acquisition must use the identity provider.");

            return (confidentialApp, result);
        }

        private static X509Certificate2 FindLocalMachineCertificate(string subjectName)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("The SNI mTLS E2E tests require a Windows certificate store and CNG KeyGuard.");
            }

            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            DateTime now = DateTime.Now;
            X509Certificate2 certificate = store.Certificates
                .OfType<X509Certificate2>()
                .Where(candidate =>
                    candidate.HasPrivateKey &&
                    string.Equals(
                        candidate.GetNameInfo(X509NameType.SimpleName, forIssuer: false),
                        subjectName,
                        StringComparison.OrdinalIgnoreCase) &&
                    candidate.NotBefore <= now &&
                    now <= candidate.NotAfter)
                .OrderByDescending(candidate => candidate.NotBefore)
                .FirstOrDefault();

            Assert.IsNotNull(
                certificate,
                $"A currently valid certificate with simple name '{subjectName}' and an accessible private key must exist in LocalMachine\\My.");

            return certificate;
        }

        private static void ValidateNonExportableVbsKey(X509Certificate2 certificate)
        {
            Assert.IsTrue(certificate.HasPrivateKey, "Certificate must have an accessible private key.");

            RSA rsa = certificate.GetRSAPrivateKey();
            Assert.IsNotNull(rsa, "Certificate must have an RSA private key.");
            Assert.IsInstanceOfType<RSACng>(rsa, "Certificate private key must use CNG.");

            var rsaCng = (RSACng)rsa;

            Assert.AreEqual(
                CngExportPolicies.None,
                rsaCng.Key.ExportPolicy,
                "Certificate private key must be non-exportable.");

            Assert.IsTrue(
                WindowsCngKeyOperations.IsKeyGuardProtected(rsaCng.Key),
                "Certificate private key must be protected by VBS virtual isolation.");
        }

        private static async Task<GraphResponse> CallGraphAsync(
            AuthenticationResult result,
            X509Certificate2 certificate)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            if (certificate is not null)
            {
                handler.ClientCertificates.Add(certificate);
            }

            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(result.TokenType, result.AccessToken);

            using HttpResponseMessage response = await httpClient
                .GetAsync(new Uri(GraphMtlsEndpoint))
                .ConfigureAwait(false);

            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            string[] challenges = response.Headers.WwwAuthenticate
                .Select(challenge => challenge.ToString())
                .ToArray();

            return new GraphResponse(response.StatusCode, responseContent, challenges);
        }

        private static void ValidateMtlsPopBinding(string accessToken, X509Certificate2 certificate)
        {
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(accessToken);
            var cnfClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "cnf");
            Assert.IsNotNull(cnfClaim, "Access token should contain a cnf claim.");

            using JsonDocument cnfJson = JsonDocument.Parse(cnfClaim.Value);
            Assert.IsTrue(
                cnfJson.RootElement.TryGetProperty("x5t#S256", out JsonElement thumbprintElement),
                "cnf claim should contain x5t#S256.");

            string tokenThumbprint = thumbprintElement.GetString();
            Assert.IsFalse(string.IsNullOrEmpty(tokenThumbprint), "x5t#S256 should not be empty.");

            byte[] certificateHash = SHA256.HashData(certificate.RawData);
            string expectedThumbprint = Convert.ToBase64String(certificateHash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            Assert.AreEqual(
                expectedThumbprint,
                tokenThumbprint,
                "Token x5t#S256 must match the supplied SNI certificate.");
        }

        private sealed class GraphResponse
        {
            public GraphResponse(
                HttpStatusCode statusCode,
                string responseBody,
                string[] wwwAuthenticateChallenges)
            {
                StatusCode = statusCode;
                ResponseBody = responseBody;
                WwwAuthenticateChallenges = wwwAuthenticateChallenges;
            }

            public HttpStatusCode StatusCode { get; }

            public string ResponseBody { get; }

            public string[] WwwAuthenticateChallenges { get; }
        }
    }
}
