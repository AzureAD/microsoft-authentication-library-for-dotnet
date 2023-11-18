// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Credential;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.Platforms.Features.KeyMaterial;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class CredentialBasedMsiAuthRequest : RequestBase
    {
        // TODO: do we really need the base functionality? 
        // TODO: remove all SLC code from AbstractManagedIdentity
        private class CredentialSource : AbstractManagedIdentity
        {
            private readonly Uri _credentialEndpoint;
            private readonly TokenRequestAssertionInfo _requestAssertionInfo;
            private readonly IKeyMaterialManager _keyMaterialManager;

            public static CredentialSource TryCreate(RequestContext requestContext)
            {
                return IsCredentialKeyAvailable(requestContext, out Uri credentialEndpointUri) ?
                    new CredentialSource(requestContext, credentialEndpointUri) :
                    null;
            }

            private CredentialSource(RequestContext requestContext, Uri credentialEndpoint)
                : base(requestContext, ManagedIdentitySource.Credential)
            {
                _credentialEndpoint = credentialEndpoint;
                _keyMaterialManager = requestContext.ServiceBundle.PlatformProxy.GetKeyMaterial();
                _requestAssertionInfo = new TokenRequestAssertionInfo(_keyMaterialManager, requestContext.ServiceBundle);
            }

            protected override ManagedIdentityRequest CreateRequest(string resource)
            {
                ManagedIdentityRequest request = new(HttpMethod.Post, _credentialEndpoint);
                request.Headers.Add("Metadata", "true");
                request.Headers.Add("x-ms-client-request-id", _requestContext.CorrelationId.ToString("D"));

                string jsonPayload = CreateCredentialPayload(_requestAssertionInfo.BindingCertificate);
                request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                return request;
            }

            private static bool IsCredentialKeyAvailable(
                RequestContext requestContext, out Uri credentialEndpointUri)
            {
                credentialEndpointUri = null;

                if (!requestContext.ServiceBundle.Config.IsManagedIdentityTokenRequestInfoAvailable)
                {
                    requestContext.Logger.Verbose(() => "[Managed Identity] Credential based managed identity is unavailable.");
                    return false;
                }

                string credentialUri = Constants.CredentialEndpoint;

                switch (requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
                {
                    case ManagedIdentityIdType.ClientId:
                        requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                        credentialUri += $"&{Constants.ManagedIdentityClientId}={requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId}";
                        break;

                    case ManagedIdentityIdType.ResourceId:
                        requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                        credentialUri += $"&{Constants.ManagedIdentityResourceId}={requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId}";
                        break;

                    case ManagedIdentityIdType.ObjectId:
                        requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                        credentialUri += $"&{Constants.ManagedIdentityObjectId}={requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId}";
                        break;
                }

                credentialEndpointUri = new(credentialUri);

                requestContext.Logger.Info($"[Managed Identity] Creating Credential based managed identity.");
                return true;
            }

            protected override async Task<ManagedIdentityResponse> HandleResponseAsync(
                AcquireTokenForManagedIdentityParameters parameters,
                HttpResponse response,
                CancellationToken cancellationToken)
            {

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (!response.HeadersAsDictionary.TryGetValue("Server", out string imdsVersionInfo))
                    {
                        _requestContext.Logger.Error("[Managed Identity] Credential endpoint response failed.");
                        throw new MsalManagedIdentityException(MsalError.ManagedIdentityRequestFailed,
                            MsalErrorMessage.CredentialResponseMissingHeader,
                            _sourceType);
                    }

                    _requestContext.Logger.Verbose(() => $"[Managed Identity] Response received from credential endpoint. " +
                    $"Status code: {response.StatusCode}");

                    CredentialResponse credentialResponse = GetCredentialResponse(response);

                    //Create the second leg request
                    //temporarily form the authority sine IMDS does not return this yet 
                    string baseUrl = "https://centraluseuap.mtlsauth.microsoft.com/";
                    string tenantId = credentialResponse.TenantId;
                    string tokenEndpoint = "/oauth2/v2.0/token?slice=testslice";
                    Uri url = new(string.Join("", baseUrl, tenantId, tokenEndpoint));

                    ManagedIdentityRequest request = new(HttpMethod.Post, url);
                    var scope = parameters.Resource + "/.default";
                    request.Headers.Add("x-ms-client-request-id", _requestContext.CorrelationId.ToString("D"));
                    request.BodyParameters.Add("grant_type", "client_credentials");
                    request.BodyParameters.Add("client_id", credentialResponse.ClientId);
                    request.BodyParameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
                    request.BodyParameters.Add("client_assertion", credentialResponse.Credential);
                    request.BodyParameters.Add("scope", scope);
                    request.BindingCertificate = _requestAssertionInfo.BindingCertificate;

                    var mergedclaims = ClaimsHelper.GetMergedClaimsAndClientCapabilities(
                        parameters.Claims, _requestContext.ServiceBundle.Config.ClientCapabilities);

                    request.BodyParameters.Add("claims", mergedclaims);

                    _requestContext.Logger.Verbose(() => $"[Managed Identity] Sending token request to mtls " +
                    $"endpoint : {credentialResponse.RegionalTokenUrl}.");

                    response = await _requestContext.ServiceBundle.HttpManager
                            .SendPostForceResponseAsync(
                                request.ComputeUri(),
                                request.Headers,
                                request.BodyParameters,
                                request.BindingCertificate,
                                _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
            }

            private CredentialResponse GetCredentialResponse(HttpResponse response)
            {
                CredentialResponse credentialResponse = JsonHelper.DeserializeFromJson<CredentialResponse>(response?.Body);

                if (credentialResponse == null || credentialResponse.Credential.IsNullOrEmpty())
                {
                    _requestContext.Logger.Error("[Managed Identity] Response is either null or insufficient for authentication.");
                    throw new MsalManagedIdentityException(
                        MsalError.ManagedIdentityRequestFailed,
                        MsalErrorMessage.ManagedIdentityInvalidResponse,
                        _sourceType);
                }

                return credentialResponse;
            }

            private static string CreateCredentialPayload(X509Certificate2 x509Certificate2)
            {
                string certificateBase64 = Convert.ToBase64String(x509Certificate2.Export(X509ContentType.Cert));

                return @"
                    {
                        ""cnf"": {
                            ""jwk"": {
                                ""kty"": ""RSA"", 
                                ""use"": ""sig"",
                                ""alg"": ""RS256"",
                                ""kid"": """ + x509Certificate2.Thumbprint + @""",
                                ""x5c"": [""" + certificateBase64 + @"""]
                            }
                        },
                        ""latch_key"": false    
                    }";
            }
        }

        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private CredentialSource _slc;
        public static CredentialBasedMsiAuthRequest TryCreate(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
        {
            // TODO: need static cahing on the state of SLC on the machine. Here or deeper down?
            CredentialSource slc =
                CredentialSource.TryCreate(authenticationRequestParameters.RequestContext);

            if (slc != null)
            {
                return new CredentialBasedMsiAuthRequest(
                    serviceBundle,
                    authenticationRequestParameters,
                    managedIdentityParameters);
            }

            return null;
        }

        private CredentialBasedMsiAuthRequest(
            IServiceBundle serviceBundle,
             AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _managedIdentityParameters = managedIdentityParameters;
        }

        protected override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            // TODO: don't rely on AbstractManagedIdentity for SLC, we need to use ConfidentialClientApplication

            // 1. 
            // slc.AuthenticateAsync(); // - call SLC endpoint to figure out the authority and the assertion.

            // 2. Create ConfidentialClientApplication cca 

            // 3. Call AcquireTokenSilent
            // We will need a new API on ConfidentialClientApplication called WithMtls(X509Certificate2) - internal
            // CCA will take care of caching, but in a different way than MSI and that's ok

            return Task.FromResult<AuthenticationResult>(null);
        }
    }
}
