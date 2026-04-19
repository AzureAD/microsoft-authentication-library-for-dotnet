// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    /// <summary>
    /// Tests all rows of the canonical credential matrix:
    ///   Row 1  – X509Cert + Regular → JWT-bearer assertion + ResolvedCertificate
    ///   Row 2  – X509Cert + MtlsMode → empty params + ResolvedCertificate
    ///   Row 3  – Secret + Regular → client_secret
    ///   Row 4  – Secret + MtlsMode → MsalClientException
    ///   Row 5  – SignedAssertion (static) + Regular → JWT-bearer assertion
    ///   Row 6  – SignedAssertion (static) + MtlsMode → MsalClientException
    ///   Row 7  – JWT callback (string) + Regular → JWT-bearer assertion
    ///   Row 8  – JWT callback (string) + MtlsMode → MsalClientException
    ///   Row 9  – JWT+cert callback + Regular → JWT-PoP assertion + ResolvedCertificate
    ///   Row 10 – JWT+cert callback + MtlsMode → JWT-PoP assertion + ResolvedCertificate
    /// Plus additional edge-case tests: null certificate, empty assertion, null-result validation.
    /// </summary>
    [TestClass]
    public class CredentialMatrixTests : TestBase
    {
        private const string TestTokenEndpoint = "https://login.microsoftonline.com/tenant/oauth2/v2.0/token";
        private const string TestAuthority = "https://login.microsoftonline.com/test-tenant-id/";
        private const string TestTenantId = "test-tenant-id";
        private static X509Certificate2 s_cert;
        private static CommonCryptographyManager s_crypto;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            s_cert = CertHelper.GetOrCreateTestCert();
            s_crypto = new CommonCryptographyManager();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static CredentialContext RegularContext() => new CredentialContext
        {
            ClientId = "client-id",
            TokenEndpoint = TestTokenEndpoint,
            Mode = OAuthMode.Regular,
            Claims = null,
            ClientCapabilities = null,
            CryptographyManager = s_crypto,
            Logger = Substitute.For<ILoggerAdapter>(),
            SendX5C = false,
            UseSha2 = true,
            ExtraClientAssertionClaims = null,
            ClientAssertionFmiPath = null,
            Authority = TestAuthority,
            TenantId = TestTenantId
        };

        private static CredentialContext MtlsContext() => new CredentialContext
        {
            ClientId = "client-id",
            TokenEndpoint = TestTokenEndpoint,
            Mode = OAuthMode.MtlsMode,
            Claims = null,
            ClientCapabilities = null,
            CryptographyManager = s_crypto,
            Logger = Substitute.For<ILoggerAdapter>(),
            SendX5C = false,
            UseSha2 = true,
            ExtraClientAssertionClaims = null,
            ClientAssertionFmiPath = null,
            Authority = TestAuthority,
            TenantId = TestTenantId
        };

        // ──────────────────────────────────────────────
        // Row 1 – X509Cert + Regular
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row1_CertificateCredential_Regular_ReturnsJwtBearerAndCertAsync()
        {
            var credential = new CertificateClientCredential(s_cert);
            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(material);
            Assert.IsNotNull(material.ResolvedCertificate);
            Assert.IsNotNull(material.TokenRequestParameters);

            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertionType));
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion));
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
            Assert.IsFalse(string.IsNullOrWhiteSpace(material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]));
        }

        [TestMethod]
        public async Task Row1b_DynamicCertificateCredential_Regular_InvokesProviderOnce_AndReturnsJwtBearerAndCertAsync()
        {
            int callCount = 0;

            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult(s_cert);
                },
                claimsToSign: null,
                appendDefaultClaims: true);

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(material);
            Assert.AreSame(s_cert, material.ResolvedCertificate);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.AreEqual(
                OAuth2AssertionType.JwtBearer,
                material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientAssertion));
        }

        // ──────────────────────────────────────────────
        // Row 2 – X509Cert + MtlsMode
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row2_CertificateCredential_MtlsMode_ReturnsEmptyParamsAndCertAsync()
        {
            var credential = new CertificateClientCredential(s_cert);
            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(material);
            Assert.IsNotNull(material.ResolvedCertificate);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.IsEmpty(material.TokenRequestParameters,
                "MtlsMode certificate credential should not add any token request parameters.");
        }

        [TestMethod]
        public async Task Row2b_DynamicCertificateCredential_MtlsMode_InvokesProviderOnce_AndReturnsEmptyParamsAndCertAsync()
        {
            int callCount = 0;

            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: _ =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult(s_cert);
                },
                claimsToSign: null,
                appendDefaultClaims: true);

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(material);
            Assert.AreSame(s_cert, material.ResolvedCertificate);
            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.IsEmpty(material.TokenRequestParameters);
        }

        // ──────────────────────────────────────────────
        // Row 3 – Secret + Regular
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row3_SecretCredential_Regular_ReturnsClientSecretAsync()
        {
            const string secret = "my-secret";
            var credential = new ClientSecretCredential(secret);
            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(material);
            Assert.IsNull(material.ResolvedCertificate);
            Assert.IsTrue(material.TokenRequestParameters.ContainsKey(OAuth2Parameter.ClientSecret));
            Assert.AreEqual(secret, material.TokenRequestParameters[OAuth2Parameter.ClientSecret]);
        }

        // ──────────────────────────────────────────────
        // Row 4 – Secret + MtlsMode  (unsupported)
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row4_SecretCredential_MtlsMode_ThrowsMsalClientExceptionAsync()
        {
            var credential = new ClientSecretCredential("my-secret");
            MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidCredentialMaterial, ex.ErrorCode);
        }

        // ──────────────────────────────────────────────
        // Row 5 – SignedAssertion (static) + Regular
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row5_StaticSignedAssertion_Regular_ReturnsJwtBearerAsync()
        {
            const string jwt = "header.payload.signature";
            var credential = new SignedAssertionClientCredential(jwt);
            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(material);
            Assert.IsNull(material.ResolvedCertificate);
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
            Assert.AreEqual(jwt, material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]);
        }

        // ──────────────────────────────────────────────
        // Row 6 – SignedAssertion (static) + MtlsMode  (unsupported)
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row6_StaticSignedAssertion_MtlsMode_ThrowsMsalClientExceptionAsync()
        {
            var credential = new SignedAssertionClientCredential("header.payload.signature");
            MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidCredentialMaterial, ex.ErrorCode);
        }

        // ──────────────────────────────────────────────
        // Row 7 – JWT callback (string) + Regular
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row7_StringCallbackCredential_Regular_ReturnsJwtBearerAsync()
        {
            const string callbackJwt = "cb.header.payload.signature";
            int callCount = 0;

            var credential = new ClientAssertionStringDelegateCredential(
                (_, __) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult(callbackJwt);
                });

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(material);
            Assert.IsNull(material.ResolvedCertificate);
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
            Assert.AreEqual(callbackJwt, material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]);
        }

        // ──────────────────────────────────────────────
        // Row 8 – JWT callback (string) + MtlsMode  (unsupported)
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row8_StringCallbackCredential_MtlsMode_ThrowsMsalClientExceptionAsync()
        {
            var credential = new ClientAssertionStringDelegateCredential(
                (_, __) => Task.FromResult("some-jwt"));

            MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidCredentialMaterial, ex.ErrorCode);
        }

        // ──────────────────────────────────────────────
        // Row 9 – JWT+cert callback + Regular (bearer-over-mTLS)
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row9_AssertionWithCertCallback_Regular_ReturnsJwtPopAndCertAsync()
        {
            // When the callback returns a cert in Regular mode (implicit bearer-over-mTLS),
            // the credential still uses jwt-pop so the token is bound to the certificate.
            const string jwt = "signed.jwt.for.test";
            var credential = new ClientAssertionDelegateCredential(
                (_, __) => Task.FromResult(new ClientSignedAssertion { Assertion = jwt, TokenBindingCertificate = s_cert }));

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(material);
            Assert.IsNotNull(material.ResolvedCertificate);
            // Even in Regular mode, returning a cert from the callback triggers JWT-PoP binding.
            Assert.AreEqual(OAuth2AssertionType.JwtPop, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
            Assert.AreEqual(jwt, material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]);
        }

        // ──────────────────────────────────────────────
        // Row 10 – JWT+cert callback + MtlsMode (JWT-PoP)
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task Row10_AssertionWithCertCallback_MtlsMode_ReturnsJwtPopAndCertAsync()
        {
            const string jwt = "signed.jwt.pop.for.test";
            int callCount = 0;

            var credential = new ClientAssertionDelegateCredential(
                (_, __) =>
                {
                    Interlocked.Increment(ref callCount);
                    return Task.FromResult(new ClientSignedAssertion
                    {
                        Assertion = jwt,
                        TokenBindingCertificate = s_cert
                    });
                });

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.ResolvedCertificate);
            Assert.AreEqual(OAuth2AssertionType.JwtPop, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
            Assert.AreEqual(jwt, material.TokenRequestParameters[OAuth2Parameter.ClientAssertion]);
        }

        // ──────────────────────────────────────────────
        // Edge cases
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task EdgeCase_AssertionCallbackReturnsNullCert_Regular_StillSucceedsAsync()
        {
            // When the callback returns a ClientSignedAssertion without a cert, regular mode is fine.
            const string jwt = "jwt.without.cert";
            var credential = new ClientAssertionDelegateCredential(
                (_, __) => Task.FromResult(new ClientSignedAssertion { Assertion = jwt, TokenBindingCertificate = null }));

            CredentialMaterial material = await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(material);
            Assert.IsNull(material.ResolvedCertificate);
            Assert.AreEqual(OAuth2AssertionType.JwtBearer, material.TokenRequestParameters[OAuth2Parameter.ClientAssertionType]);
        }

        [TestMethod]
        public async Task EdgeCase_AssertionCallbackReturnsNullCert_MtlsMode_ThrowsMsalClientExceptionAsync()
        {
            // MtlsMode without a cert should throw.
            var credential = new ClientAssertionDelegateCredential(
                (_, __) => Task.FromResult(new ClientSignedAssertion { Assertion = "jwt", TokenBindingCertificate = null }));

            MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(MtlsContext(), CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.MtlsCertificateNotProvided, ex.ErrorCode);
        }

        [TestMethod]
        public async Task EdgeCase_AssertionCallbackReturnsEmptyAssertion_ThrowsMsalClientExceptionAsync()
        {
            var credential = new ClientAssertionStringDelegateCredential(
                (_, __) => Task.FromResult(string.Empty));

            MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(RegularContext(), CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidClientAssertion, ex.ErrorCode);
        }

        [TestMethod]
        public async Task EdgeCase_DelegateCredential_CallbackReturnsNullAssertion_ThrowsMsalClientExceptionAsync()
        {
            var credential = new ClientAssertionDelegateCredential(
                (_, __) => Task.FromResult(new ClientSignedAssertion { Assertion = null, TokenBindingCertificate = null }));

            MsalClientException ex = await Assert.ThrowsExactlyAsync<MsalClientException>(
                () => credential.GetCredentialMaterialAsync(RegularContext(), CancellationToken.None))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.InvalidClientAssertion, ex.ErrorCode);
        }

        [TestMethod]
        public void CredentialMaterial_NullTokenRequestParameters_ThrowsArgumentNullException()
        {
            // CredentialMaterial rejects null TokenRequestParameters at construction time.
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new CredentialMaterial(null));
        }

        [TestMethod]
        public void CredentialMaterial_EmptyTokenRequestParameters_IsValid()
        {
            // Empty dictionary (e.g. for mTLS cert credential in MtlsMode) is explicitly allowed.
            var material = new CredentialMaterial(
                new Dictionary<string, string>(),
                s_cert);

            Assert.IsNotNull(material.TokenRequestParameters);
            Assert.IsEmpty(material.TokenRequestParameters);
            Assert.IsNotNull(material.ResolvedCertificate);
        }

        // ──────────────────────────────────────────────
        // Authority / TenantId propagation
        // ──────────────────────────────────────────────

        [TestMethod]
        public async Task DelegateCredential_PropagatesAuthorityAndTenantId_ToCallbackOptionsAsync()
        {
            AssertionRequestOptions capturedOptions = null;

            var credential = new ClientAssertionDelegateCredential(
                (opts, _) =>
                {
                    capturedOptions = opts;
                    return Task.FromResult(new ClientSignedAssertion { Assertion = "jwt", TokenBindingCertificate = s_cert });
                });

            await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(capturedOptions);
            Assert.AreEqual(TestAuthority, capturedOptions.Authority);
            Assert.AreEqual(TestTenantId, capturedOptions.TenantId);
            Assert.AreEqual("client-id", capturedOptions.ClientID);
            Assert.AreEqual(TestTokenEndpoint, capturedOptions.TokenEndpoint);
        }

        [TestMethod]
        public async Task StringDelegateCredential_PropagatesAuthorityAndTenantId_ToCallbackOptionsAsync()
        {
            AssertionRequestOptions capturedOptions = null;

            var credential = new ClientAssertionStringDelegateCredential(
                (opts, _) =>
                {
                    capturedOptions = opts;
                    return Task.FromResult("signed.jwt");
                });

            await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(capturedOptions);
            Assert.AreEqual(TestAuthority, capturedOptions.Authority);
            Assert.AreEqual(TestTenantId, capturedOptions.TenantId);
        }

        [TestMethod]
        public async Task DynamicCertCredential_PropagatesAuthorityAndTenantId_ToCertProviderAsync()
        {
            AssertionRequestOptions capturedOptions = null;

            var credential = new CertificateAndClaimsClientCredential(
                certificateProvider: opts =>
                {
                    capturedOptions = opts;
                    return Task.FromResult(s_cert);
                },
                claimsToSign: null,
                appendDefaultClaims: true);

            await credential
                .GetCredentialMaterialAsync(RegularContext(), CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(capturedOptions);
            Assert.AreEqual(TestAuthority, capturedOptions.Authority);
            Assert.AreEqual(TestTenantId, capturedOptions.TenantId);
        }
    }
}
