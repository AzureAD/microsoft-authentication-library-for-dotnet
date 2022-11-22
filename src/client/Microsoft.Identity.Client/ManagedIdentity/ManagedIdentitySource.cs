// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;
using System.Net;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/ManagedIdentitySource.cs
    /// </summary>
    internal abstract class ManagedIdentitySource
    {
        protected readonly RequestContext _requestContext;

        protected ManagedIdentitySource(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        public virtual async Task<ManagedIdentityResponse> AuthenticateAsync(AppTokenProviderParameters parameters, CancellationToken cancellationToken)
        {
            ManagedIdentityRequest request = CreateRequest(parameters.Scopes.ToArray());
            
            var response =
            HttpMethod.Get.Equals(request.Method) ?
            await _requestContext.ServiceBundle.HttpManager.SendGetForceResponseAsync(request.Endpoint, request.Headers, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false) :
            await _requestContext.ServiceBundle.HttpManager.SendPostAsync(request.Endpoint, request.Headers, request.BodyParameters, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);

            return HandleResponse(parameters, response);
            
        }

        protected virtual ManagedIdentityResponse HandleResponse(
            AppTokenProviderParameters parameters,
            HttpResponse response)
        {
            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _requestContext.Logger.Info("[Managed Identity] Successful response received.");
                    return GetSuccessfulResponse(response);
                }


                MsalServiceException msalServiceException = MsalServiceExceptionFactory.FromManagedIdentityResponse(MsalError.ManagedIdentityRequestFailed, response);
                _requestContext.Logger.Error($"[Managed Identity] request failed, HttpStatusCode: {response.StatusCode}. Error message: {msalServiceException.Message}");
                throw msalServiceException;
            }
            catch (Exception e) when (e is not MsalServiceException)
            {
                _requestContext.Logger.Error(MsalErrorMessage.UnexpectedResponse);
                throw new MsalServiceException(MsalError.ManagedIdentityRequestFailed, MsalErrorMessage.UnexpectedResponse, e);
            }
        }

        protected abstract ManagedIdentityRequest CreateRequest(string[] scopes);

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);

            if (managedIdentityResponse == null || managedIdentityResponse.AccessToken.IsNullOrEmpty() || managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                _requestContext.Logger.Error("[Managed Identity] Response is either null or insufficient for authentication.");
                throw new MsalServiceException(MsalError.ManagedIdentityRequestFailed, MsalErrorMessage.AuthenticationResponseInvalidFormatError);
            }

            return managedIdentityResponse;
        }
    }
}
