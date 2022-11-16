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
    internal abstract class ManagedIdentitySource
    {
        protected readonly RequestContext _requestContext;

        protected ManagedIdentitySource(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        protected internal string ClientId { get; }

        public virtual async Task<ManagedIdentityResponse> AuthenticateAsync(AppTokenProviderParameters parameters, CancellationToken cancellationToken)
        {
            ManagedIdentityRequest request = CreateRequest(parameters.Scopes.ToArray());
            
            var response =
            HttpMethod.Get.Equals(request.Method) ?
            await _requestContext.ServiceBundle.HttpManager.SendGetAsync(request.UriBuilder.Uri, request.Headers, _requestContext.Logger, useManagedIdentity: true, cancellationToken: cancellationToken).ConfigureAwait(false) :
            await _requestContext.ServiceBundle.HttpManager.SendPostAsync(request.UriBuilder.Uri, request.Headers, request.BodyParameters, _requestContext.Logger, useManagedIdentity: true, cancellationToken: cancellationToken).ConfigureAwait(false);

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

                throw MsalServiceExceptionFactory.FromManagedIdentityResponse(MsalError.ManagedIdentityRequestFailed, response);
            }
            catch (Exception e) when (e is not MsalServiceException)
            {
                throw new MsalServiceException(MsalError.UnknownManagedIdentityError, MsalErrorMessage.UnexpectedResponse, e);
            }
        }

        protected abstract ManagedIdentityRequest CreateRequest(string[] scopes);

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);

            if (managedIdentityResponse == null || managedIdentityResponse.AccessToken.IsNullOrEmpty() || managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                throw new MsalServiceException(MsalError.InvalidManagedIdentityResponse, MsalErrorMessage.AuthenticationResponseInvalidFormatError);
            }

            return managedIdentityResponse;
        }
    }
}
