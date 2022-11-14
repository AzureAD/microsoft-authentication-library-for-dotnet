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

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal abstract class ManagedIdentitySource
    {
        internal const string AuthenticationResponseInvalidFormatError = "Invalid response, the authentication response was not in the expected format.";
        internal const string UnexpectedResponse = "Managed Identity response was not in the expected format. See the inner exception for details.";
        internal RequestContext _requestContext;

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
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return GetSuccessfulResponse(response);
                }

                throw MsalServiceExceptionFactory.FromManagedIdentityResponse(MsalError.ManagedIdentityRequestFailed, response);
            }
            catch (Exception e)
            {
                if (e is MsalServiceException)
                    throw;

                throw new MsalServiceException(MsalError.UnknownManagedIdentityError, UnexpectedResponse, e);
            }
        }

        protected abstract ManagedIdentityRequest CreateRequest(string[] scopes);


        protected string GetMessageFromResponse(HttpResponse response)
        {
            if (response.Body.IsNullOrEmpty())
            {
                _requestContext.Logger.Info("The response body is empty.");
                return null;
            }

            return JsonHelper.DeserializeFromJson<ManagedIdentityErrorResponse>(response.Body).Message;
        }

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);
            if (managedIdentityResponse == null || managedIdentityResponse.AccessToken.IsNullOrEmpty() || managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                throw new MsalServiceException(MsalError.InvalidManagedIdentityResponse, AuthenticationResponseInvalidFormatError);
            }

            return managedIdentityResponse;
        }
    }
}
