// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    // Workload token binding with optional endorsement by the configured RDP certificate.
    internal static class EndorsedUnattestedManagedIdentitySource
    {
        // Temporary version supported by the test endpoint pending a dedicated API version.
        internal const string ApiVersion = "2018-02-01";
        internal const string ProofAudience = "http://169.254.169.254/metadata/identity/oauth2";
        internal const string InvalidResponse = "managed_identity_binding_response_invalid";
        internal const string RdpCertificateNotFound = "managed_identity_rdp_certificate_not_found";

        internal static void ValidateCertificate(X509Certificate2 certificate)
        {
            if (certificate is null)
            {
                throw new MsalClientException("managed_identity_binding_certificate_invalid",
                    "No binding certificate was available. No key was created and no key permissions were changed.");
            }

            try
            {
                DateTime now = DateTime.UtcNow;
                if (!certificate.HasPrivateKey || now < certificate.NotBefore.ToUniversalTime() ||
                    now >= certificate.NotAfter.ToUniversalTime())
                {
                    throw new MsalClientException("managed_identity_binding_certificate_invalid",
                        "The binding certificate must be valid and have an accessible RSA private key.");
                }

                using (RSA key = certificate.GetRSAPrivateKey())
                {
                    if (key is null || key.KeySize < 2048)
                    {
                        throw new MsalClientException("managed_identity_binding_certificate_invalid",
                            "The binding certificate requires an accessible RSA key of at least 2048 bits.");
                    }
                }
            }
            catch (Exception ex) when (ex is CryptographicException || ex is ObjectDisposedException)
            {
                throw new MsalClientException("managed_identity_binding_certificate_invalid",
                    "The binding certificate or its private key cannot be opened. No key permissions were changed.", ex);
            }
        }

        internal static void ValidateRequest(
            AuthenticationRequestParameters request,
            AcquireTokenForManagedIdentityParameters parameters,
            X509Certificate2 certificate)
        {
            ValidateRequestOptions(request, parameters);
            ValidateCertificate(certificate);
        }

        internal static void ValidateRequestOptions(
            AuthenticationRequestParameters request,
            AcquireTokenForManagedIdentityParameters parameters)
        {
            var config = request.AppConfig;
            if (parameters.MtlsPopMinStrength == MtlsBindingStrength.KeyGuard)
            {
                throw new MsalClientException(MsalError.MinStrengthNotMet,
                    "The endorsed unattested flow establishes only Software-strength binding. " +
                    "A platform certificate alone does not establish KeyGuard protection.");
            }

            if (!request.IsMtlsPopRequested || request.PreferMsiV2 || parameters.AttestationTokenProvider is not null ||
                (parameters.MtlsPopMinStrength != MtlsBindingStrength.None &&
                 parameters.MtlsPopMinStrength != MtlsBindingStrength.Software))
            {
                throw new MsalClientException("managed_identity_binding_options_invalid",
                    "The endorsed unattested flow supports only mTLS PoP without attestation, with no floor or the Software floor.");
            }

            if (ManagedIdentityClient.GetManagedIdentitySourceNoImds(request.RequestContext.Logger) != ManagedIdentitySource.None ||
                !string.IsNullOrEmpty(EnvironmentVariables.PodIdentityEndpoint) || EnvironmentVariables.IsImdsV2Disabled)
            {
                throw new MsalClientException("managed_identity_binding_environment_invalid",
                    "The endorsed unattested flow requires the direct Azure VM IMDS endpoint and cannot use endpoint overrides.");
            }

            if (config.ManagedIdentityId.IdType != ManagedIdentityIdType.SystemAssigned &&
                config.ManagedIdentityId.IdType != ManagedIdentityIdType.ClientId)
            {
                throw new MsalClientException("managed_identity_binding_selector_unsupported",
                    "This prototype supports system-assigned identities or user-assigned client IDs only.");
            }

            if (!string.IsNullOrEmpty(request.Claims) || config.ClientCapabilities?.Any() == true ||
                request.ExtraQueryParameters?.Count > 0 || config.ExtraQueryParameters?.Count > 0)
            {
                throw new MsalClientException("managed_identity_binding_options_unsupported",
                    "Claims, client capabilities, and extra query parameters are not defined by this prototype contract.");
            }
        }

        internal static async Task<MsalTokenResponse> AcquireTokenAsync(
            AuthenticationRequestParameters request,
            AcquireTokenForManagedIdentityParameters parameters,
            X509Certificate2 certificate,
            X509Certificate2 endorsementCertificate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateRequest(request, parameters, certificate);
            if (endorsementCertificate is not null)
            {
                ValidateCertificate(endorsementCertificate);
            }
            var context = request.RequestContext;
            // Match the successful test-endpoint PoC without changing the normalized cache resource.
            string wireResource = parameters.Resource + "/.default";
            string tokenEndpoint = ImdsManagedIdentitySource.DefaultImdsBaseEndpoint + ImdsManagedIdentitySource.ImdsTokenPath;
            var metadataRequest = new ManagedIdentityRequest(HttpMethod.Post,
                new Uri(tokenEndpoint));
            metadataRequest.QueryParameters["api-version"] = ApiVersion;
            metadataRequest.QueryParameters["resource"] = wireResource;
            metadataRequest.QueryParameters["withMinStrength"] = endorsementCertificate is null ? "0" : "1";
            if (request.AppConfig.ManagedIdentityId.IsUserAssigned)
            {
                metadataRequest.QueryParameters["client_id"] = request.AppConfig.ManagedIdentityId.UserAssignedId;
            }
            metadataRequest.Headers["Metadata"] = "true";
            metadataRequest.Headers[OAuth2Header.XMsCorrelationId] = context.CorrelationId.ToString();

            string assertion;
            var proofBody = new Dictionary<string, string>();
            try
            {
                assertion = CreateProof(request, wireResource, certificate);
                proofBody["req_cnf_proof"] = assertion;
                if (endorsementCertificate is not null)
                {
                    using RSA endorsementKey = endorsementCertificate.GetRSAPrivateKey();
                    proofBody["endorsement_certificate"] = Convert.ToBase64String(endorsementCertificate.RawData);
                    proofBody["endorsement_signature"] = Convert.ToBase64String(
                        endorsementKey.SignData(Encoding.UTF8.GetBytes(assertion),
                            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                }
            }
            catch (CryptographicException ex)
            {
                throw new MsalClientException(MsalError.CryptographicError,
                    "The workload proof or its RDP endorsement could not be signed.", ex);
            }

            // Replaying a single-use proof during automatic retries is not defined by the draft.
            // The caller can retry acquisition, which creates a new assertion and jti.
            HttpResponse response;
            DateTimeOffset requestStartedAt = DateTimeOffset.UtcNow;
            using (var body = new StringContent(
                JsonHelper.SerializeToJson(proofBody),
                Encoding.UTF8, "application/json"))
            {
                if (context.ApiEvent is not null)
                {
                    context.ApiEvent.TokenEndpoint = tokenEndpoint;
                }

                try
                {
                    // Custom factories own handler policy. Use normal selection rather than
                    // the WS-Trust route, which can replace an ordinary configured factory.
                    response = await context.ServiceBundle.HttpManager.SendRequestAsync(
                        metadataRequest.ComputeUri(), metadataRequest.Headers, body, HttpMethod.Post,
                        context.Logger, doNotThrow: true, mtlsCertificate: null, validateServerCertificate: null,
                        cancellationToken, retryPolicy: null,
                        allowAutoRedirect: false, useDefaultCredentials: false,
                        useConfiguredHttpClientFactory: context.ServiceBundle.UsesCustomHttpClientFactory)
                        .ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.ManagedIdentityUnreachableNetwork, "The experimental IMDS endpoint could not be reached.",
                        ex, ManagedIdentitySource.Imds, null);
                }
                finally
                {
                    if (context.ApiEvent is not null)
                    {
                        context.ApiEvent.DurationInHttpInMs += context.ServiceBundle.HttpManager.LastRequestDurationInMs;
                    }
                }
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed, "The experimental IMDS certificate-binding request failed.",
                    null, ManagedIdentitySource.Imds, (int)response.StatusCode, response);
            }

            return ParseResponse(response.Body, parameters.Resource, request.AppConfig.ManagedIdentityId, certificate, requestStartedAt);
        }

        private static string CreateProof(
            AuthenticationRequestParameters request, string resource, X509Certificate2 certificate)
        {
            string bindingId = certificate.ComputeX5tS256KeyId();
            string header = WriteJson(writer =>
            {
                writer.WriteString("alg", "RS256");
                writer.WriteString("typ", "JWT");
                writer.WriteStartArray("x5c");
                writer.WriteStringValue(Convert.ToBase64String(certificate.RawData));
                writer.WriteEndArray();
            });
            string payload = WriteJson(writer =>
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                writer.WriteString("iss", "urn:azure:token-binding:self-signed");
                writer.WriteString("sub", bindingId);
                writer.WriteString("aud", ProofAudience);
                writer.WriteNumber("iat", now);
                writer.WriteNumber("nbf", now);
                writer.WriteNumber("exp", now + 60);
                writer.WriteString("jti", Guid.NewGuid().ToString("N"));
                writer.WriteString("resource", resource);
                writer.WriteString("managed_identity", request.AppConfig.ManagedIdentityId.IsUserAssigned
                    ? request.AppConfig.ManagedIdentityId.UserAssignedId : "system_assigned");
                writer.WriteStartObject("cnf");
                writer.WriteString("x5t#S256", bindingId);
                writer.WriteEndObject();
            });
            string input = Base64UrlHelpers.EncodeString(header) + "." + Base64UrlHelpers.EncodeString(payload);
            byte[] signature = request.RequestContext.ServiceBundle.PlatformProxy.CryptographyManager
                .SignWithCertificate(input, certificate, RSASignaturePadding.Pkcs1);
            return input + "." + Base64UrlHelpers.Encode(signature);
        }

        private static string WriteJson(Action<Utf8JsonWriter> writeProperties)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    writeProperties(writer);
                    writer.WriteEndObject();
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        internal static MsalTokenResponse ParseResponse(
            string body, string resource, ManagedIdentityId identity, X509Certificate2 certificate,
            DateTimeOffset requestStartedAt)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new MsalClientException(InvalidResponse, "The experimental IMDS response is empty.");
            }

            try
            {
                using (var json = JsonDocument.Parse(body))
                {
                    var root = json.RootElement;
                    string accessToken = root.GetProperty("access_token").GetString();
                    string clientId = root.GetProperty("client_id").GetString();
                    if (!root.TryGetProperty("expires_in", out JsonElement lifetime) ||
                        lifetime.ValueKind != JsonValueKind.Number ||
                        !lifetime.TryGetInt64(out long expiresIn) || expiresIn <= 0)
                    {
                        throw new MsalClientException(InvalidResponse,
                            "The IMDS response must contain a positive integer expires_in lifetime.");
                    }

                    // Account for transport time rather than extending a relative lifetime on receipt.
                    long expiresOn = checked(requestStartedAt.ToUnixTimeSeconds() + expiresIn);
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long certificateExpiry = new DateTimeOffset(certificate.NotAfter.ToUniversalTime()).ToUnixTimeSeconds();
                    if (string.IsNullOrWhiteSpace(accessToken) || !Guid.TryParse(clientId, out Guid resolvedClientId) ||
                        resolvedClientId == Guid.Empty ||
                        !string.Equals(root.GetProperty("token_type").GetString(), Constants.MtlsPoPTokenType, StringComparison.Ordinal) ||
                        (identity.IsUserAssigned &&
                            (!Guid.TryParse(identity.UserAssignedId, out Guid requestedClientId) || resolvedClientId != requestedClientId)) ||
                        expiresOn <= now || expiresOn > certificateExpiry)
                    {
                        throw new MsalClientException(InvalidResponse,
                            "The IMDS response contains an invalid token, identity, token type, or lifetime for the supplied certificate.");
                    }

                    // Tokens are opaque. IMDS binds the token to the signed request; the caller
                    // retains its certificate. No outer cnf, returned certificate or strength is required.
                    // Do not use ext_expires_in to extend a certificate-bound token's cache lifetime.
                    return MsalTokenResponse.CreateFromManagedIdentityResponse(new ManagedIdentityResponse
                    {
                        AccessToken = accessToken,
                        ClientId = clientId,
                        Resource = resource,
                        TokenType = Constants.MtlsPoPTokenType,
                        ExpiresOn = expiresOn.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException ||
                ex is InvalidOperationException || ex is FormatException || ex is OverflowException)
            {
                // Do not attach parser exceptions: they can contain response data.
                throw new MsalClientException(InvalidResponse, "The experimental IMDS response is malformed or incomplete.");
            }
        }
    }
}
