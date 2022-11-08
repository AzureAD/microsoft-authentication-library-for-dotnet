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
                await _requestContext.ServiceBundle.HttpManager.SendGetAsync(request.UriBuilder.Uri, request.Headers, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false) : 
                await _requestContext.ServiceBundle.HttpManager.SendPostAsync(request.UriBuilder.Uri, request.Headers, request.BodyParams, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);

            return await HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async Task<ManagedIdentityResponse> HandleResponseAsync(
            AppTokenProviderParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            string message;
            Exception exception = null;
            try
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);
                }

                message = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body).Message;
            }
            catch (Exception e)
            {
                message = AuthenticationResponseInvalidFormatError + "messagebody: " + response.Body;
                exception = e;
            }

            throw new MsalServiceException("msi-auth-failed", message, exception);
        }

        protected abstract ManagedIdentityRequest CreateRequest(string[] scopes);


        protected static string GetMessageFromResponse(HttpResponse response, CancellationToken cancellationToken)
        {
            if (response.Body == null)
            {
                return null;
            }

            return JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body).Message;
        }
    }
}
