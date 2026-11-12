// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.KeyAttestation;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ImdsVersion = Microsoft.Identity.Client.ManagedIdentity.V2.ImdsVersion;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    [DoNotParallelize]
    public class EndorsedUnattestedManagedIdentityTests : TestBase
    {
        private const string Resource = "https://resource.example";
        private const string ClientId = "11111111-2222-3333-4444-555555555555";
        private EnvVariableContext _environment;
        private X509Certificate2 _certificate;

        [TestInitialize]
        public void Initialize()
        {
            _environment = new EnvVariableContext();
            foreach (string name in new[] { "IDENTITY_ENDPOINT", "IDENTITY_HEADER", "IDENTITY_SERVER_THUMBPRINT",
                "MSI_ENDPOINT", "MSI_SECRET", "IMDS_ENDPOINT", "AZURE_POD_IDENTITY_AUTHORITY_HOST",
                EnvironmentVariables.DisableImdsV2EnvVar })
            {
                Environment.SetEnvironmentVariable(name, null);
            }
            ManagedIdentityClient.ResetSourceForTest();
            using RSA key = RSA.Create(2048);
            _certificate = CreateCertificate(key);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _certificate.Dispose();
            ManagedIdentityClient.ResetSourceForTest();
            _environment.Dispose();
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task ActualMsalRequestSeparatesBindingAndEndorsementKeysAsync(bool userAssigned, bool includeDefaultSuffix)
        {
            // Arrange
            using var http = new MockHttpManager();
            var identity = userAssigned ? ManagedIdentityId.WithUserAssignedClientId(ClientId) : ManagedIdentityId.SystemAssigned;
            var app = CreateApp(http, identity);
            string inputResource = includeDefaultSuffix ? Resource + "/.default" : Resource;
            var handler = AddResponse(http, CreateResponse(), userAssigned);
            string firstProof = null;
            handler.AdditionalRequestValidation = message =>
            {
                Assert.IsNull(message.Headers.Authorization);
                Assert.AreEqual("application/json", message.Content.Headers.ContentType.MediaType);
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyEndorsement(body.RootElement, _certificate);
                Assert.IsFalse(body.RootElement.TryGetProperty("client_assertion", out _));
                firstProof = body.RootElement.GetProperty("req_cnf_proof").GetString();
                VerifyProof(firstProof, _certificate, userAssigned);
                Assert.AreEqual(userAssigned, message.RequestUri.Query.Contains("client_id="));
            };

            // Act
            var first = await app.AcquireTokenForManagedIdentity(inputResource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            var cached = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("mtls_pop", first.TokenType);
            Assert.AreEqual(Resource, first.Scopes.Single());
            Assert.AreEqual(Resource, cached.Scopes.Single());
            AssertBindingCertificate(app, first);
            AssertBindingCertificate(app, cached);
            using var firstHeader = JsonDocument.Parse(Decode(firstProof.Split('.')[0]));
            Assert.AreEqual(Convert.ToBase64String(first.BindingCertificate.RawData),
                firstHeader.RootElement.GetProperty("x5c")[0].GetString());
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);

            // ForceRefresh must keep the same key/certificate but create a fresh proof.
            var refreshHandler = AddResponse(http, CreateResponse("refreshed-token"), userAssigned);
            refreshHandler.AdditionalRequestValidation = message =>
            {
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyEndorsement(body.RootElement, _certificate);
                Assert.IsFalse(body.RootElement.TryGetProperty("client_assertion", out _));
                string proof = body.RootElement.GetProperty("req_cnf_proof").GetString();
                Assert.AreNotEqual(firstProof, proof);
                VerifyProof(proof, _certificate, userAssigned);
            };
            var refreshed = await app.AcquireTokenForManagedIdentity(inputResource)
                .WithMtlsProofOfPossession()
                .WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            AssertBindingCertificate(app, refreshed);
            Assert.AreEqual("refreshed-token", refreshed.AccessToken);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            using RSA privateKey = _certificate.GetRSAPrivateKey();
            Assert.IsNotNull(privateKey);
            Assert.IsNotEmpty(privateKey.SignData(new byte[] { 1 }, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ParameterlessPopSelectsRdpCertificateAndUsesRealCacheWithoutExperimentalOptInAsync(bool emptyOptions)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            AddResponse(http, CreateResponse()).AdditionalRequestValidation = message =>
            {
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyProof(body.RootElement.GetProperty("req_cnf_proof").GetString(), _certificate, false);
            };

            // Act
            var request = app.AcquireTokenForManagedIdentity(Resource);
            request = emptyOptions ? request.WithMtlsProofOfPossession(new PoPOptions()) : request.WithMtlsProofOfPossession();
            var first = await request.ExecuteAsync().ConfigureAwait(false);
            var cached = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.IsFalse(app.ServiceBundle.Config.ExperimentalFeaturesEnabled);
            AssertBindingCertificate(app, first);
            AssertBindingCertificate(app, cached);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(first.BindingCertificate.ComputeX5tS256KeyId(), app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().KeyId);
            await platform.Received(2).GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        public async Task AutomaticRdpDiscoveryObservesCertificateRotationAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            using RSA key = RSA.Create(2048);
            using var rotated = CreateCertificate(key);
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_certificate), Task.FromResult(rotated));
            AddResponse(http, CreateResponse("first-certificate"));
            AddResponse(http, CreateResponse("rotated-certificate")).AdditionalRequestValidation = message =>
            {
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyProof(body.RootElement.GetProperty("req_cnf_proof").GetString(), rotated, false);
                VerifyEndorsement(body.RootElement, rotated);
            };
            var request = app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession();

            // Act
            var first = await request.ExecuteAsync().ConfigureAwait(false);
            var second = await request.ExecuteAsync().ConfigureAwait(false);
            var cached = await request.ExecuteAsync().ConfigureAwait(false);

            // Assert
            AssertBindingCertificate(app, first);
            Assert.AreSame(first.BindingCertificate, second.BindingCertificate);
            Assert.AreSame(first.BindingCertificate, cached.BindingCertificate);
            Assert.AreEqual("rotated-certificate", cached.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        public async Task RdpDiscoveryFailureDoesNotFallBackOrSendNetworkRequestAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            var failure = new MsalClientException(RdpCertificateReader.CertificateUnavailable, "Simulated inaccessible listener.");
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<X509Certificate2>(failure));

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession().ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreSame(failure, error);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MissingRdpCertificateUsesUnendorsedBindingAndSeparateCachePartitionAsync(bool userAssigned)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, userAssigned
                ? ManagedIdentityId.WithUserAssignedClientId(ClientId) : ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<X509Certificate2>(new MsalClientException(RdpCertificateReader.CertificateNotFound)));
            AddResponse(http, CreateResponse("unendorsed"), userAssigned, endorsed: false)
                .AdditionalRequestValidation = message =>
                {
                    Assert.Contains("withMinStrength=0", message.RequestUri.Query);
                    using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    Assert.AreEqual(1, body.RootElement.EnumerateObject().Count());
                    VerifyProof(body.RootElement.GetProperty("req_cnf_proof").GetString(), _certificate, userAssigned);
                };
            var request = app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession();

            // Act
            var unendorsed = await request.ExecuteAsync().ConfigureAwait(false);
            var cached = await request.ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            AssertBindingCertificate(app, unendorsed);
            Assert.AreEqual("mtls_pop", unendorsed.TokenType);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_certificate));
            AddResponse(http, CreateResponse("endorsed"), userAssigned).AdditionalRequestValidation = message =>
            {
                Assert.Contains("withMinStrength=1", message.RequestUri.Query);
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyEndorsement(body.RootElement, _certificate);
            };
            var endorsed = await request.ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(TokenSource.IdentityProvider, endorsed.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("endorsed", endorsed.AccessToken);
            Assert.AreSame(unendorsed.BindingCertificate, endorsed.BindingCertificate);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task AutomaticRdpRejectsUnsupportedOptionsBeforeDiscoveryAsync(bool keyGuardFloor)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            var request = app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession(
                new PoPOptions { MinStrength = keyGuardFloor ? MtlsBindingStrength.KeyGuard : MtlsBindingStrength.None });
            if (!keyGuardFloor)
            {
                request.WithClaims("claims-not-supported-by-this-protocol");
            }

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() => request.ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(keyGuardFloor ? MsalError.MinStrengthNotMet : "managed_identity_binding_options_unsupported",
                error.ErrorCode);
            await platform.DidNotReceive().GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task CanceledAutomaticRequestDoesNotDiscoverRdpCertificateAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            // Act / Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellation.Token)).ConfigureAwait(false);
            await platform.DidNotReceive().GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task AutomaticRdpRoutingDependsOnAttestationNotExperimentalOptInAsync(bool experimental, bool attestation)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithExperimentalFeatures(experimental).WithHttpManager(http).BuildConcrete();
            var executor = Substitute.For<IManagedIdentityApplicationExecutor>();
            executor.ServiceBundle.Returns(app.ServiceBundle);
            executor.ExecuteAsync(default, default, default).ReturnsForAnyArgs(Task.FromResult<AuthenticationResult>(null));
            var request = AcquireTokenForManagedIdentityParameterBuilder.Create(executor, Resource);
            if (attestation)
            {
                request.WithAttestationSupport();
            }

            // Act
            request.WithMtlsProofOfPossession();
            if (attestation && !DesktopOsHelper.IsWindows())
            {
                var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                    request.ExecuteAsync()).ConfigureAwait(false);
                Assert.AreEqual(MsalError.MtlsNotSupportedForManagedIdentity, error.ErrorCode);
                await executor.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default).ConfigureAwait(false);
                return;
            }
            await request.ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(!attestation, request.Parameters.UseRdpCertificate);
            Assert.AreEqual(!attestation, request.CommonParameters.CacheKeyComponents.ContainsKey("mi_binding_protocol"));
        }

        [TestMethod]
        [DataRow("rotation")]
        [DataRow("attestation")]
        [DataRow("strength")]
        [DataRow("claims")]
        public async Task ProactiveRefreshKeepsExecutionCertificateAndOptionsAsync(string change)
        {
            // Arrange
            using var http = new MockHttpManager();
            using RSA key = RSA.Create(2048);
            using var secondCertificate = CreateCertificate(key);
            var completed = new TaskCompletionSource<ExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithExperimentalFeatures() // Required by the background-completion callback, not certificate selection.
                .WithHttpManager(http)
                .OnBackgroundTokenRefreshCompleted(result =>
                {
                    completed.TrySetResult(result);
                    return Task.CompletedTask;
                })
                .BuildConcrete();
            var platform = ConfigureRdpDiscovery(app, _certificate);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_certificate), Task.FromResult(_certificate), Task.FromResult(secondCertificate));
            var request = app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession();
            AddResponse(http, CreateResponse("initial-token"));
            await request.ExecuteAsync().ConfigureAwait(false);
            TestCommon.UpdateATWithRefreshOn(app.AppTokenCacheInternal.Accessor);
            string proof = null;
            string signingKeyId = null;
            var refresh = AddResponse(http, CreateResponse());
            refresh.AdditionalRequestValidation = message =>
            {
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                proof = body.RootElement.GetProperty("req_cnf_proof").GetString();
                VerifyEndorsement(body.RootElement, _certificate);
                using var payload = JsonDocument.Parse(Decode(proof.Split('.')[1]));
                signingKeyId = payload.RootElement.GetProperty("cnf").GetProperty("x5t#S256").GetString();
                refresh.ResponseMessage.Content = new StringContent(
                    JsonSerializer.Serialize(CreateResponse("refreshed-for-" + signingKeyId)),
                    Encoding.UTF8, "application/json");
            };
            var semaphore = (SemaphoreSlim)typeof(ManagedIdentityAuthRequest)
                .GetField("s_semaphoreSlim", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            AuthenticationResult served;

            // Act: the foreground cache hit returns while its refresh cannot enter the semaphore.
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                served = await request.ExecuteAsync().ConfigureAwait(false);
                if (change == "claims")
                {
                    request.WithClaims("{\"access_token\":{\"test\":{\"essential\":true}}}");
                }
                else if (change != "rotation")
                {
                    request.WithMtlsProofOfPossession(new PoPOptions
                    {
                        MinStrength = change == "strength" ? MtlsBindingStrength.KeyGuard : MtlsBindingStrength.None
                    });
                    if (change == "attestation")
                    {
                        request.WithAttestationSupport();
                    }

                    using var canceled = new CancellationTokenSource();
                    canceled.Cancel();
                    if (change == "attestation" && !DesktopOsHelper.IsWindows())
                    {
                        var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                            request.ExecuteAsync(canceled.Token)).ConfigureAwait(false);
                        Assert.AreEqual(MsalError.MtlsNotSupportedForManagedIdentity, error.ErrorCode);
                    }
                    else
                    {
                        await Assert.ThrowsAsync<OperationCanceledException>(() =>
                            request.ExecuteAsync(canceled.Token)).ConfigureAwait(false);
                    }
                    Assert.AreEqual(change == "attestation", request.Parameters.AttestationTokenProvider is not null);
                    Assert.AreEqual(change == "strength" ? MtlsBindingStrength.KeyGuard : MtlsBindingStrength.None,
                        request.Parameters.MtlsPopMinStrength);
                }
            }
            finally
            {
                semaphore.Release();
            }
            Assert.AreSame(completed.Task, await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(30))).ConfigureAwait(false),
                "The proactive refresh did not complete.");
            var background = await completed.Task.ConfigureAwait(false);
            Assert.IsTrue(background.Successful, background.Exception?.ToString());
            await platform.Received(2).GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_certificate));
            var cached = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession()
                .ExecuteAsync().ConfigureAwait(false);

            // Assert against the real MSAL cache and the real foreground/background results.
            var stored = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single();
            Assert.AreEqual(TokenSource.Cache, served.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("initial-token", served.AccessToken);
            AssertBindingCertificate(app, served);
            Assert.AreEqual(served.BindingCertificate.ComputeX5tS256KeyId(), stored.KeyId);
            Assert.AreEqual(signingKeyId, stored.KeyId, "The cached key must match the certificate that signed the proof.");
            Assert.AreEqual("refreshed-for-" + signingKeyId, stored.Secret);
            Assert.AreEqual(stored.Secret, background.Result.AccessToken);
            Assert.AreSame(served.BindingCertificate, background.Result.BindingCertificate);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(stored.Secret, cached.AccessToken);
            Assert.AreSame(served.BindingCertificate, cached.BindingCertificate);
            VerifyProof(proof, _certificate, userAssigned: false);
            if (change == "claims")
            {
                var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                    request.ExecuteAsync()).ConfigureAwait(false);
                Assert.AreEqual("managed_identity_binding_options_unsupported", error.ErrorCode);
            }
            await platform.Received(3).GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow("N")]
        [DataRow("D")]
        [DataRow("B")]
        [DataRow("P")]
        public async Task EquivalentUserAssignedClientIdFormatsAreAcceptedAsync(string format)
        {
            // Arrange
            using var http = new MockHttpManager();
            string requestedClientId = Guid.Parse(ClientId).ToString(format);
            var app = CreateApp(http, ManagedIdentityId.WithUserAssignedClientId(requestedClientId));
            var response = AddResponse(http, CreateResponse(), userAssigned: true);
            // The mock compares raw query values, including URI-escaped braces.
            response.ExpectedQueryParams["client_id"] =
                new Uri(Resource + "?client_id=" + requestedClientId).Query.Substring("?client_id=".Length);

            // Act
            var result = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("simulated-bound-token", result.AccessToken);
            AssertBindingCertificate(app, result);
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow("valid")]
        [DataRow("missing")]
        [DataRow("invalid")]
        [DataRow("canceled")]
        public async Task ImdsV1CapabilitiesReflectRdpAvailabilityAndAcquisitionAsync(string certificateState)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(_ => certificateState switch
                {
                    "valid" => Task.FromResult(new X509Certificate2(_certificate)),
                    "invalid" => Task.FromResult(new X509Certificate2(_certificate.RawData)),
                    "canceled" => Task.FromCanceled<X509Certificate2>(new CancellationToken(canceled: true)),
                    _ => Task.FromException<X509Certificate2>(new MsalClientException(RdpCertificateReader.CertificateNotFound))
                });
            http.AddMockHandler(MockHelpers.MockImdsProbeFailure(ImdsVersion.V2));
            http.AddMockHandler(MockHelpers.MockImdsProbe(ImdsVersion.V1));
            http.AddMockHandler(MockHelpers.MockImdsComputeMetadata(
                osType: DesktopOsHelper.IsWindows() ? "Windows" : "Linux", securityType: null));

            // Act
            if (certificateState == "canceled")
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                    app.GetManagedIdentityCapabilitiesAsync(CancellationToken.None)).ConfigureAwait(false);
                Assert.AreEqual(0, http.QueueSize);
                return;
            }
            var capabilities = await app.GetManagedIdentityCapabilitiesAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            bool certificateAvailable = certificateState is "valid" or "missing";
            Assert.AreEqual(ManagedIdentitySource.Imds, capabilities.Source);
            Assert.AreEqual(certificateAvailable ? MtlsBindingStrength.Software : MtlsBindingStrength.None,
                capabilities.MaxSupportedBindingStrength);
            Assert.AreEqual(certificateAvailable, capabilities.IsMtlsPopSupportedByHost);
            if (certificateAvailable)
            {
                AddResponse(http, CreateResponse(), endorsed: certificateState == "valid");
                var result = await app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession(new PoPOptions { MinStrength = MtlsBindingStrength.Software })
                    .ExecuteAsync().ConfigureAwait(false);
                using (result.BindingCertificate)
                {
                    Assert.AreEqual("mtls_pop", result.TokenType);
                    AssertBindingCertificate(app, result);
                }
            }
            else
            {
                var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                    app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession()
                        .ExecuteAsync()).ConfigureAwait(false);
                Assert.AreEqual("managed_identity_binding_certificate_invalid", error.ErrorCode);
            }
            Assert.AreEqual(0, http.QueueSize);
            await platform.Received(2).GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OrdinaryCustomFactoryIsUsedWithoutFallbackAsync()
        {
            // Arrange
            var factory = Substitute.For<IMsalHttpClientFactory>();
            using var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedUrl = ImdsManagedIdentitySource.DefaultImdsBaseEndpoint + ImdsManagedIdentitySource.ImdsTokenPath,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(CreateResponse()))
                }
            };
            using var client = new HttpClient(handler);
            factory.GetHttpClient().Returns(client);
            var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpClientFactory(factory).BuildConcrete();
            ConfigureRdpDiscovery(app, _certificate);
            var manager = (HttpManager)app.ServiceBundle.HttpManager;
            var fallback = Substitute.For<IMsalWsTrustHttpClientFactory>();
            fallback.GetHttpClient(false).Returns(client);
            // Keep the production manager, but prevent a regression from reaching live IMDS.
            typeof(HttpManager).GetField("_defaultWsTrustHttpClientFactory", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, new Lazy<IMsalWsTrustHttpClientFactory>(() => fallback));

            // Act
            var result = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);

            // Assert
            AssertBindingCertificate(app, result);
            Assert.AreEqual("simulated-bound-token", result.AccessToken);
            factory.Received(1).GetHttpClient();
            fallback.DidNotReceive().GetHttpClient(false);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RdpCertificateUsesConfiguredServiceBundleManagerAsync(bool customFactory)
        {
            // Arrange
            var factory = Substitute.For<IMsalHttpClientFactory>();
            var manager = Substitute.For<IHttpManager>();
            manager.SendRequestAsync(default, default, default, default, default, default, default, default,
                default, default).ReturnsForAnyArgs(Task.FromResult(new HttpResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    Body = JsonSerializer.Serialize(CreateResponse())
                }));
            var appBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(manager);
            if (customFactory)
            {
                appBuilder.WithHttpClientFactory(factory);
            }
            var app = appBuilder.BuildConcrete();
            ConfigureRdpDiscovery(app, _certificate);
            // Mutating and rebuilding the application builder must not change the first app's transport decision.
            var replacementFactory = Substitute.For<IMsalHttpClientFactory>();
            var secondApp = appBuilder.WithHttpClientFactory(replacementFactory).BuildConcrete();
            Assert.IsTrue(secondApp.ServiceBundle.UsesCustomHttpClientFactory);
            Assert.AreEqual(customFactory, app.ServiceBundle.UsesCustomHttpClientFactory);

            // Act
            var result = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);

            // Assert
            AssertBindingCertificate(app, result);
            Assert.AreSame(manager, app.ServiceBundle.HttpManager);
            factory.DidNotReceive().GetHttpClient();
            await manager.Received(1).SendRequestAsync(
                Arg.Is<Uri>(uri => uri.Host == "169.254.169.254"),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<HttpContent>(), HttpMethod.Post,
                Arg.Any<ILoggerAdapter>(), doNotThrow: true, mtlsCertificate: null, validateServerCertificate: null,
                Arg.Any<CancellationToken>(), retryPolicy: null, retryCount: 0,
                allowAutoRedirect: false, useDefaultCredentials: false,
                retryDelayCancellationToken: default,
                useConfiguredHttpClientFactory: customFactory).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow("success")]
        [DataRow("http-error")]
        [DataRow("network-error")]
        [DataRow("invalid-response")]
        public async Task RdpRequestRecordsEndpointAndHttpDurationAsync(string outcome)
        {
            // Arrange
            const long duration = 37;
            string endpoint = ImdsManagedIdentitySource.DefaultImdsBaseEndpoint + ImdsManagedIdentitySource.ImdsTokenPath;
            var manager = Substitute.For<IHttpManager>();
            manager.LastRequestDurationInMs.Returns(duration);
            manager.SendRequestAsync(default, default, default, default, default, default, default, default,
                default, default).ReturnsForAnyArgs(_ =>
                {
                    if (outcome == "network-error")
                    {
                        return Task.FromException<HttpResponse>(new HttpRequestException("Simulated network failure."));
                    }
                    return Task.FromResult(new HttpResponse
                    {
                        StatusCode = outcome == "http-error" ? HttpStatusCode.BadRequest : HttpStatusCode.OK,
                        Body = outcome == "invalid-response" ? "{}" : JsonSerializer.Serialize(CreateResponse())
                    });
                });
            var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpManager(manager).BuildConcrete();
            ConfigureRdpDiscovery(app, _certificate);
            var request = app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession();

            // Act
            AuthenticationResultMetadata metadata;
            if (outcome == "success")
            {
                var result = await request.ExecuteAsync().ConfigureAwait(false);
                metadata = result.AuthenticationResultMetadata;
            }
            else
            {
                var error = await Assert.ThrowsAsync<MsalException>(() => request.ExecuteAsync()).ConfigureAwait(false);
                metadata = error.AuthenticationResultMetadata;
                Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }

            // Assert
            Assert.IsNotNull(metadata);
            Assert.AreEqual(endpoint, metadata.TokenEndpoint);
            Assert.AreEqual(duration, metadata.DurationInHttpInMs);
        }

        [TestMethod]
        [DataRow(200, "ordinary")]
        [DataRow(302, "ordinary")]
        [DataRow(429, "ordinary")]
        [DataRow(500, "ordinary")]
        [DataRow(503, "ordinary")]
        [DataRow(200, "ws-trust")]
        [DataRow(302, "ws-trust")]
        [DataRow(429, "ws-trust")]
        [DataRow(500, "ws-trust")]
        [DataRow(503, "ws-trust")]
        [DataRow(200, "mtls")]
        [DataRow(302, "mtls")]
        [DataRow(429, "mtls")]
        [DataRow(500, "mtls")]
        [DataRow(503, "mtls")]
        public async Task CustomFactoryUsesConfiguredClientAndNeverReplaysProofAsync(int status, string factoryType)
        {
            // Arrange
            IMsalHttpClientFactory factory = factoryType switch
            {
                "ws-trust" => Substitute.For<IMsalWsTrustHttpClientFactory>(),
                "mtls" => Substitute.For<IMsalMtlsHttpClientFactory>(),
                _ => Substitute.For<IMsalHttpClientFactory>()
            };
            int attempts = 0;
            using var handler = new MockHttpMessageHandler
            {
                ExpectedMethod = HttpMethod.Post,
                ExpectedUrl = ImdsManagedIdentitySource.DefaultImdsBaseEndpoint + ImdsManagedIdentitySource.ImdsTokenPath,
                AdditionalRequestValidation = message =>
                {
                    attempts++;
                    using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    VerifyProof(body.RootElement.GetProperty("req_cnf_proof").GetString(), _certificate, userAssigned: false);
                },
                ResponseMessage = new HttpResponseMessage((HttpStatusCode)status)
                {
                    Content = new StringContent(JsonSerializer.Serialize(CreateResponse()))
                }
            };
            using var client = new HttpClient(handler);
            factory.GetHttpClient().Returns(client);
            var app = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                .WithHttpClientFactory(factory).BuildConcrete();
            ConfigureRdpDiscovery(app, _certificate);

            // Act
            var acquisition = app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession();
            if (status == 200)
            {
                AssertBindingCertificate(app, await acquisition.ExecuteAsync().ConfigureAwait(false));
                Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
                var cached = await acquisition.ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            }
            else
            {
                var error = await Assert.ThrowsAsync<MsalServiceException>(() => acquisition.ExecuteAsync()).ConfigureAwait(false);
                Assert.AreEqual(status, error.StatusCode);
                Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            }

            // Assert
            Assert.AreEqual(1, attempts);
            factory.Received(1).GetHttpClient();
            if (factory is IMsalWsTrustHttpClientFactory specializedFactory)
            {
                specializedFactory.DidNotReceive().GetHttpClient(Arg.Any<bool>());
            }
            if (factory is IMsalMtlsHttpClientFactory mtlsFactory)
            {
                mtlsFactory.DidNotReceive().GetHttpClient(Arg.Any<X509Certificate2>());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task InterruptedRequestDoesNotReplayOrCacheProofAsync(bool callerCanceled)
        {
            // Arrange
            using var http = new MockHttpManager();
            using var cancellation = new CancellationTokenSource();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            int attempts = 0;
            AddResponse(http, CreateResponse()).AdditionalRequestValidation = _ =>
            {
                attempts++;
                if (callerCanceled)
                {
                    cancellation.Cancel();
                    throw new TaskCanceledException("Simulated cancellation.");
                }
                throw new TaskCanceledException("Simulated HTTP timeout.");
            };

            // Act
            Task<AuthenticationResult> request = app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync(cancellation.Token);
            if (callerCanceled)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() => request).ConfigureAwait(false);
            }
            else
            {
                var error = await Assert.ThrowsAsync<MsalServiceException>(() => request).ConfigureAwait(false);
                Assert.AreEqual(MsalError.RequestTimeout, error.ErrorCode);
            }

            // Assert
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(0, http.QueueSize);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow("token_type")]
        [DataRow("client_id")]
        [DataRow("malformed_client_id")]
        [DataRow("empty_client_id")]
        [DataRow("missing_access_token")]
        [DataRow("missing_client_id")]
        [DataRow("missing_token_type")]
        [DataRow("missing_expires_in")]
        [DataRow("zero_lifetime")]
        [DataRow("negative_lifetime")]
        [DataRow("string_lifetime")]
        [DataRow("fractional_lifetime")]
        [DataRow("overflow_lifetime")]
        [DataRow("expires_on_only")]
        [DataRow("past_certificate")]
        [DataRow("access_token")]
        [DataRow("whitespace_access_token")]
        public async Task InvalidResponseNeverEntersMsalCacheAsync(string fault)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.WithUserAssignedClientId(ClientId));
            var body = CreateResponse();
            switch (fault)
            {
                case "missing_access_token":
                    body.Remove("access_token");
                    break;
                case "missing_client_id":
                    body.Remove("client_id");
                    break;
                case "missing_token_type":
                    body.Remove("token_type");
                    break;
                case "missing_expires_in":
                    body.Remove("expires_in");
                    break;
                case "zero_lifetime":
                    body["expires_in"] = 0;
                    break;
                case "negative_lifetime":
                    body["expires_in"] = -1;
                    break;
                case "string_lifetime":
                    body["expires_in"] = "3600";
                    break;
                case "fractional_lifetime":
                    body["expires_in"] = 1.5;
                    break;
                case "overflow_lifetime":
                    body["expires_in"] = long.MaxValue;
                    break;
                case "expires_on_only":
                    body.Remove("expires_in");
                    body["expires_on"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
                    break;
                case "past_certificate":
                    body["expires_in"] = 31 * 24 * 3600;
                    break;
                case "malformed_client_id":
                    body["client_id"] = "not-a-guid";
                    break;
                case "empty_client_id":
                    body["client_id"] = Guid.Empty.ToString();
                    break;
                case "client_id":
                    body["client_id"] = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
                    break;
                case "access_token":
                    body["access_token"] = "";
                    break;
                case "whitespace_access_token":
                    body["access_token"] = " ";
                    break;
                default:
                    body[fault] = "wrong-value";
                    break;
            }
            AddResponse(http, body, userAssigned: true);

            // Act
            var exception = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession().ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(EndorsedUnattestedManagedIdentitySource.InvalidResponse, exception.ErrorCode);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            AddResponse(http, CreateResponse(), userAssigned: true);
            var result = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ObservedLiveResponseUsesRelativeExpiryAndOpaqueTokenAsync(bool includeExtendedExpiry)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var handler = AddResponse(http, CreateResponse());
            const string response = "{\"access_token\":\"opaque-not-a-jwt\",\"client_id\":\"11111111-2222-3333-4444-555555555555\",\"expires_in\":86399,\"token_type\":\"mtls_pop\"";
            handler.ResponseMessage.Content = new StringContent(
                response + (includeExtendedExpiry ? ",\"ext_expires_in\":172800}" : "}"), Encoding.UTF8, "application/json");
            DateTimeOffset before = DateTimeOffset.UtcNow;

            // Act
            var result = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            var cached = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("opaque-not-a-jwt", result.AccessToken);
            AssertBindingCertificate(app, result);
            Assert.IsTrue(result.ExpiresOn > before.AddSeconds(86394));
            Assert.IsTrue(result.ExpiresOn <= DateTimeOffset.UtcNow.AddSeconds(86399));
            Assert.AreEqual(result.ExpiresOn, cached.ExpiresOn);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("null")]
        [DataRow("[]")]
        [DataRow("not-json")]
        public async Task MalformedResponseIsNotCachedOrExposedAsync(string response)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var handler = AddResponse(http, CreateResponse());
            handler.ResponseMessage.Content = new StringContent(response);

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession().ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(EndorsedUnattestedManagedIdentitySource.InvalidResponse, error.ErrorCode);
            Assert.IsNull(error.InnerException);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public void ResponseLifetimeDoesNotRestartAfterTransportDelay()
        {
            // Arrange
            string response = JsonSerializer.Serialize(CreateResponse());
            DateTimeOffset started = DateTimeOffset.UtcNow.AddMinutes(-10);

            // Act
            var result = EndorsedUnattestedManagedIdentitySource.ParseResponse(
                response, Resource, ManagedIdentityId.SystemAssigned, _certificate, started);

            // Assert
            Assert.IsTrue(result.ExpiresIn > 2995 && result.ExpiresIn <= 3000);
            Assert.Throws<MsalClientException>(() => EndorsedUnattestedManagedIdentitySource.ParseResponse(
                response, Resource, ManagedIdentityId.SystemAssigned, _certificate, started.AddHours(-1)));
        }

        [TestMethod]
        public async Task SameKeyDifferentCertificateUsesDifferentCachePartitionAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            using RSA key = RSA.Create(2048);
            using var firstCertificate = CreateCertificate(key);
            using var secondCertificate = CreateCertificate(key);
            CollectionAssert.AreEqual(firstCertificate.GetPublicKey(), secondCertificate.GetPublicKey());
            Assert.AreNotEqual(firstCertificate.ComputeX5tS256KeyId(), secondCertificate.ComputeX5tS256KeyId());
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, firstCertificate);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(firstCertificate), Task.FromResult(secondCertificate),
                    Task.FromResult(firstCertificate), Task.FromResult(secondCertificate));
            var firstRequest = app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession();
            var secondRequest = app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession();
            AddResponse(http, CreateResponse("first-token")).AdditionalRequestValidation = message =>
            {
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyProof(body.RootElement.GetProperty("req_cnf_proof").GetString(), firstCertificate, userAssigned: false);
            };
            AddResponse(http, CreateResponse("second-token")).AdditionalRequestValidation = message =>
            {
                using var body = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyProof(body.RootElement.GetProperty("req_cnf_proof").GetString(), secondCertificate, userAssigned: false);
            };

            // Act
            var first = await firstRequest.ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            var second = await secondRequest.ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("first-token", first.AccessToken);
            Assert.AreEqual("second-token", second.AccessToken);
            AssertBindingCertificate(app, first);
            Assert.AreSame(first.BindingCertificate, second.BindingCertificate);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            var cachedFirst = await firstRequest.ExecuteAsync().ConfigureAwait(false);
            var cachedSecond = await secondRequest.ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, cachedFirst.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(TokenSource.Cache, cachedSecond.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(first.AccessToken, cachedFirst.AccessToken);
            Assert.AreEqual(second.AccessToken, cachedSecond.AccessToken);
            Assert.AreSame(first.BindingCertificate, cachedFirst.BindingCertificate);
            Assert.AreSame(second.BindingCertificate, cachedSecond.BindingCertificate);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RdpCertificateCacheIsPartitionedByIdentityAndStrengthAsync(bool changeIdentity)
        {
            // Arrange
            using var http = new MockHttpManager();
            var firstApp = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var secondApp = changeIdentity
                ? CreateApp(http, ManagedIdentityId.WithUserAssignedClientId(ClientId)) : firstApp;
            var secondOptions = new PoPOptions
            {
                MinStrength = changeIdentity ? MtlsBindingStrength.None : MtlsBindingStrength.Software
            };
            AddResponse(http, CreateResponse("first-partition"));
            AddResponse(http, CreateResponse("second-partition"), userAssigned: changeIdentity);

            // Act / Assert
            var first = await firstApp.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(1, firstApp.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            var second = await secondApp.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession(secondOptions).ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(2, firstApp.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual("first-partition", first.AccessToken);
            Assert.AreEqual("second-partition", second.AccessToken);
            AssertBindingCertificate(firstApp, first);
            AssertBindingCertificate(secondApp, second);
            var cached = await firstApp.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(first.AccessToken, cached.AccessToken);
            Assert.HasCount(2, firstApp.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task KeyGuardRequestIsRejectedBeforeNetworkOrCacheAsync(bool populateCache)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            if (populateCache)
            {
                AddResponse(http, CreateResponse());
                await app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            }

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession(new PoPOptions
                    {
                        MinStrength = MtlsBindingStrength.KeyGuard
                    }).ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.MinStrengthNotMet, error.ErrorCode);
            Assert.HasCount(populateCache ? 1 : 0, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        [DataRow(302)]
        [DataRow(400)]
        [DataRow(404)]
        [DataRow(500)]
        [DataRow(503)]
        public async Task EndpointFailureNeverFallsBackOrReplaysProofAsync(int status)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            const string description = "AADSTS70011: Simulated invalid scope.";
            var handler = AddResponse(http, new Dictionary<string, object>
            {
                ["error"] = "invalid_scope",
                ["error_description"] = description
            });
            handler.ResponseMessage.StatusCode = (HttpStatusCode)status;
            handler.AdditionalRequestValidation = message =>
            {
                using var proofBody = JsonDocument.Parse(message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                VerifyEndorsement(proofBody.RootElement, _certificate);
                Assert.Contains("withMinStrength=1", message.RequestUri.Query);
            };
            handler.ResponseMessage.Headers.Add("x-ms-request-id", "simulated-request-id");

            // Act
            var error = await Assert.ThrowsAsync<MsalServiceException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession().ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, error.ErrorCode);
            Assert.AreEqual(status, error.StatusCode);
            Assert.AreEqual(ManagedIdentitySource.Imds.ToString(), error.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
            Assert.AreEqual(status is 404 or 500 or 503, error.IsRetryable);
            Assert.AreEqual("simulated-request-id", error.Headers.GetValues("x-ms-request-id").Single());
            using var body = JsonDocument.Parse(error.ResponseBody);
            Assert.AreEqual("invalid_scope", body.RootElement.GetProperty("error").GetString());
            Assert.AreEqual(description, body.RootElement.GetProperty("error_description").GetString());
            Assert.DoesNotContain(description, error.Message);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        public async Task EndpointOverrideIsRejectedBeforeAnyHttpAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            Environment.SetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST", "http://127.0.0.1");

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession().ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("managed_identity_binding_environment_invalid", error.ErrorCode);
        }

        [TestMethod]
        public async Task DiscoveredCertificateRequiresPrivateKeyWithoutExperimentalOptInAsync()
        {
            // Arrange
            using var publicOnly = new X509Certificate2(_certificate.RawData);
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            ConfigureRdpDiscovery(app, publicOnly);

            // Act
            var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                app.AcquireTokenForManagedIdentity(Resource).WithMtlsProofOfPossession().ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(app.ServiceBundle.Config.ExperimentalFeaturesEnabled);
            Assert.AreEqual("managed_identity_binding_certificate_invalid", error.ErrorCode);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.AreEqual(0, http.QueueSize);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RdpCertificateCannotBeCombinedWithBearerOverMtls(bool bearerFirst)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var request = app.AcquireTokenForManagedIdentity(Resource);
            var options = new PoPOptions();

            // Act
            var error = Assert.Throws<MsalClientException>(() =>
            {
                if (bearerFirst)
                {
                    request.WithRequestOverMtls().WithMtlsProofOfPossession(options);
                }
                else
                {
                    request.WithMtlsProofOfPossession(options).WithRequestOverMtls();
                }
            });

            // Assert
            Assert.AreEqual(bearerFirst && !DesktopOsHelper.IsWindows()
                ? MsalError.MtlsNotSupportedForManagedIdentity
                : "mtls_pop_and_bearer_exclusive", error.ErrorCode);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task ReapplyingDefaultPopOptionsClearsStrengthAndRetainsRdpSelectionAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var executor = Substitute.For<IManagedIdentityApplicationExecutor>();
            executor.ServiceBundle.Returns(app.ServiceBundle);
            executor.ExecuteAsync(default, default, default)
                .ReturnsForAnyArgs(Task.FromResult<AuthenticationResult>(null));
            var request = AcquireTokenForManagedIdentityParameterBuilder.Create(executor, Resource)
                .WithMtlsProofOfPossession(new PoPOptions { MinStrength = MtlsBindingStrength.Software });
            await request.ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(MtlsBindingStrength.Software, request.Parameters.MtlsPopMinStrength);
            Assert.IsTrue(request.CommonParameters.CacheKeyComponents.ContainsKey("mi_binding_protocol"));

            // Act
            request.WithMtlsProofOfPossession();
            await request.ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MtlsBindingStrength.None, request.Parameters.MtlsPopMinStrength);
            Assert.IsTrue(request.Parameters.UseRdpCertificate);
            Assert.IsTrue(request.CommonParameters.CacheKeyComponents.ContainsKey("mi_binding_protocol"));
            Assert.IsTrue(request.CommonParameters.IsMtlsPopRequested);

            request.WithAttestationSupport();
            if (!DesktopOsHelper.IsWindows())
            {
                var error = await Assert.ThrowsAsync<MsalClientException>(() =>
                    request.ExecuteAsync()).ConfigureAwait(false);
                Assert.AreEqual(MsalError.MtlsNotSupportedForManagedIdentity, error.ErrorCode);
                return;
            }
            await request.ExecuteAsync().ConfigureAwait(false);
            Assert.IsFalse(request.Parameters.UseRdpCertificate);
            Assert.IsFalse(request.CommonParameters.CacheKeyComponents.ContainsKey("mi_binding_protocol"));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task NetworkFailureDoesNotRetryOrRemintCertificateAsync(bool schannelFailure)
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            var platform = ConfigureRdpDiscovery(app, _certificate);
            var failure = schannelFailure
                ? new HttpRequestException("Simulated TLS failure.", new AuthenticationException(
                    "Authentication failed.", new Win32Exception(unchecked((int)0x8009030D))))
                : new HttpRequestException("Simulated network failure.");
            var handler = AddResponse(http, CreateResponse());
            int attempts = 0;
            handler.AdditionalRequestValidation = _ =>
            {
                attempts++;
                throw failure;
            };

            // Act
            var error = await Assert.ThrowsAsync<MsalServiceException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.ManagedIdentityUnreachableNetwork, error.ErrorCode);
            Assert.AreSame(failure, error.InnerException);
            Assert.AreEqual(1, attempts);
            await platform.Received(1).GetRdpBindingCertificateAsync(
                Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>()).ConfigureAwait(false);
            Assert.AreEqual(0, http.QueueSize);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task CanceledRequestDoesNotSendProofAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            // Act / Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                app.AcquireTokenForManagedIdentity(Resource)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync(cancellation.Token)).ConfigureAwait(false);
            Assert.IsEmpty(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        [TestMethod]
        public async Task OrdinaryBearerRequestStillUsesExistingImdsPathAsync()
        {
            // Arrange
            using var http = new MockHttpManager();
            var app = CreateApp(http, ManagedIdentityId.SystemAssigned);
            AddResponse(http, CreateResponse("bound-token"));
            await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            Assert.HasCount(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedUrl = ImdsManagedIdentitySource.DefaultImdsBaseEndpoint + ImdsManagedIdentitySource.ImdsTokenPath,
                ExpectedMethod = HttpMethod.Get,
                ExpectedQueryParams = new Dictionary<string, string>
                {
                    ["api-version"] = ImdsManagedIdentitySource.ImdsApiVersion,
                    ["resource"] = Resource
                },
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["access_token"] = "ordinary-token",
                        ["token_type"] = "Bearer",
                        ["client_id"] = ClientId,
                        ["resource"] = Resource,
                        ["expires_on"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }))
                }
            });

            // Act
            var result = await app.AcquireTokenForManagedIdentity(Resource).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("ordinary-token", result.AccessToken);
            Assert.IsNull(result.BindingCertificate);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
            var cachedBound = await app.AcquireTokenForManagedIdentity(Resource)
                .WithMtlsProofOfPossession().ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("bound-token", cachedBound.AccessToken);
            AssertBindingCertificate(app, cachedBound);
            Assert.AreEqual(TokenSource.Cache, cachedBound.AuthenticationResultMetadata.TokenSource);
            Assert.HasCount(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens());
        }

        private ManagedIdentityApplication CreateApp(MockHttpManager http, ManagedIdentityId identity)
        {
            var app = ManagedIdentityApplicationBuilder.Create(identity)
                .WithHttpManager(http).BuildConcrete();
            ConfigureRdpDiscovery(app, _certificate);
            return app;
        }

        private static IPlatformProxy ConfigureRdpDiscovery(ManagedIdentityApplication app, X509Certificate2 certificate)
        {
            var original = app.ServiceBundle.PlatformProxy;
            var cryptographyManager = original.CryptographyManager;
            var keyProvider = original.ManagedIdentityKeyProvider;
            var platform = Substitute.For<IPlatformProxy>();
            platform.CryptographyManager.Returns(cryptographyManager);
            platform.ManagedIdentityKeyProvider.Returns(keyProvider);
            platform.GetRdpBindingCertificateAsync(Arg.Any<ILoggerAdapter>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(certificate));
            app.ServiceBundle.SetPlatformProxyForTest(platform);
            return platform;
        }

        private static X509Certificate2 CreateCertificate(RSA key)
        {
            var request = new CertificateRequest("CN=Simulated-Rdp-Binding", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(2));
        }

        private static Dictionary<string, object> CreateResponse(string token = "simulated-bound-token")
        {
            return new Dictionary<string, object>
            {
                ["access_token"] = token,
                ["token_type"] = "mtls_pop",
                ["client_id"] = ClientId,
                ["expires_in"] = 3600,
                ["ext_expires_in"] = 3600
            };
        }

        private static MockHttpMessageHandler AddResponse(MockHttpManager http, Dictionary<string, object> body,
            bool userAssigned = false, bool endorsed = true)
        {
            var query = new Dictionary<string, string>
            {
                ["resource"] = "https://resource.example/.default",
                ["api-version"] = "2018-02-01",
                ["withminstrength"] = endorsed ? "1" : "0"
            };
            if (userAssigned)
            {
                query["client_id"] = ClientId;
            }

            return http.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedUrl = ImdsManagedIdentitySource.DefaultImdsBaseEndpoint + ImdsManagedIdentitySource.ImdsTokenPath,
                ExpectedMethod = HttpMethod.Post,
                ExpectedQueryParams = query,
                ExpectedRequestHeaders = new Dictionary<string, string> { ["Metadata"] = "true" },
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                }
            });
        }

        private static void VerifyProof(string proof, X509Certificate2 certificate, bool userAssigned)
        {
            string[] parts = proof.Split('.');
            Assert.HasCount(3, parts);
            using var header = JsonDocument.Parse(Decode(parts[0]));
            Assert.AreEqual("RS256", header.RootElement.GetProperty("alg").GetString());
            using var binding = new X509Certificate2(Convert.FromBase64String(header.RootElement.GetProperty("x5c")[0].GetString()));
            CollectionAssert.AreNotEqual(certificate.GetPublicKey(), binding.GetPublicKey(),
                "The workload proof must not use the endorsement key.");
            using var payload = JsonDocument.Parse(Decode(parts[1]));
            var claims = payload.RootElement;
            Assert.AreEqual(EndorsedUnattestedManagedIdentitySource.ProofAudience, claims.GetProperty("aud").GetString());
            Assert.AreEqual("https://resource.example/.default", claims.GetProperty("resource").GetString());
            Assert.AreEqual(userAssigned ? ClientId : "system_assigned", claims.GetProperty("managed_identity").GetString());
            Assert.AreEqual(binding.ComputeX5tS256KeyId(), claims.GetProperty("cnf").GetProperty("x5t#S256").GetString());
            Assert.AreEqual(60L, claims.GetProperty("exp").GetInt64() - claims.GetProperty("iat").GetInt64());
            using RSA publicKey = binding.GetRSAPublicKey();
            Assert.IsTrue(publicKey.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]),
                Decode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private static void AssertBindingCertificate(ManagedIdentityApplication app, AuthenticationResult result)
        {
            Assert.AreSame(app.ManagedIdentityClient.GetOrCreateUnattestedBindingCertificate(CancellationToken.None),
                result.BindingCertificate);
        }

        private static void VerifyEndorsement(JsonElement body, X509Certificate2 certificate)
        {
            Assert.AreEqual(3, body.EnumerateObject().Count());
            Assert.AreEqual(Convert.ToBase64String(certificate.RawData), body.GetProperty("endorsement_certificate").GetString());
            string proof = body.GetProperty("req_cnf_proof").GetString();
            byte[] signature = Convert.FromBase64String(body.GetProperty("endorsement_signature").GetString());
            using RSA key = certificate.GetRSAPublicKey();
            Assert.IsTrue(key.VerifyData(Encoding.UTF8.GetBytes(proof), signature,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            Assert.IsFalse(key.VerifyData(Encoding.UTF8.GetBytes(proof + "x"), signature,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            Assert.IsFalse(key.VerifyData(Decode(proof.Split('.')[1]), signature,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private static byte[] Decode(string value)
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(base64.PadRight((base64.Length + 3) / 4 * 4, '='));
        }
    }
}
