// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET6_0 || NET6_WIN
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class CredentialSource : AbstractManagedIdentity
    {
        private readonly Uri _credentialEndpoint;
        private readonly TokenRequestAssertionInfo _requestAssertionInfo;

        public static AbstractManagedIdentity TryCreate(RequestContext requestContext)
        {
            return IsCredentialKeyAvailable(requestContext, requestContext.Logger, out Uri credentialEndpointUri) ?
                new CredentialSource(requestContext, credentialEndpointUri) :
                null;
        }

        private CredentialSource(RequestContext requestContext, Uri credentialEndpoint)
            : base(requestContext, ManagedIdentitySource.Credential)
        {
            _credentialEndpoint = credentialEndpoint;
            _requestAssertionInfo = TokenRequestAssertionInfo.GetCredentialInfo(requestContext);
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new(HttpMethod.Post, _credentialEndpoint);
            request.Headers.Add("Metadata", "true");
            request.Headers.Add("x-ms-client-request-id", _requestContext.CorrelationId.ToString("D"));

            string jsonPayload = TokenRequestAssertionInfo.CreateCredentialPayload(_requestAssertionInfo.BindingCertificate);
            request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            return request;
        }

        private static bool IsCredentialKeyAvailable(
            RequestContext requestContext, ILoggerAdapter logger, out Uri credentialEndpointUri)
        {
            credentialEndpointUri = null;

            if (requestContext.ServiceBundle.Config.KeyMaterialInfo.CryptoKeyType == CryptoKeyType.None)
            {
                logger.Verbose(() => "[Managed Identity] Credential based managed identity is unavailable.");
                return false;
            }

            string credentialUri = requestContext.ServiceBundle.Config.KeyMaterialInfo.CredentialEndpoint;

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

            logger.Info($"[Managed Identity] Creating Credential based managed identity.");
            return true;
        }

        protected override async Task<ManagedIdentityResponse> HandleResponseAsync(
            AcquireTokenForManagedIdentityParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            _requestContext.Logger.Verbose(() => $"[Managed Identity] Response received. Status code: {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (!response.HeadersAsDictionary.TryGetValue("Server", out string imdsVersionInfo))
                {
                    _requestContext.Logger.Error("[Managed Identity] Credential endpoint response failed.");
                    throw new MsalManagedIdentityException(MsalError.ManagedIdentityRequestFailed,
                        MsalErrorMessage.CredentialEndpointNoResponseReceived,
                        _sourceType);
                }

                CredentialResponse credentialResponse = GetCredentialResponse(response);

                //Create the second leg request
                ManagedIdentityRequest request = CreateRequest(parameters, credentialResponse);

                _requestContext.Logger.Verbose(() => "[Managed Identity] Sending request to mtls endpoint.");

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
            CredentialResponse credentialResponse2 = JsonHelper.DeserializeFromJson<CredentialResponse>(response?.Body);

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

        protected ManagedIdentityRequest CreateRequest(
            AcquireTokenForManagedIdentityParameters parameters, CredentialResponse credentialResponse)
        {
            //temporarily form the authority sine IMDS does not return this yet 
            string baseUrl = "https://mtlsauth.microsoft.com/";
            string tenantId = credentialResponse.TenantId;
            string tokenEndpoint = "/oauth2/v2.0/token?dc=ESTS-PUB-WUS2-AZ1-FD000-TEST1";
            Uri url = new (string.Join("", baseUrl, tenantId, tokenEndpoint));

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

            return request;
        }
    }
}
#endif
