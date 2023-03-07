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
        internal const string TimeoutError = "[Managed Identity] Authentication unavailable. The request to the managed identity endpoint timed out.";

        protected ManagedIdentitySource(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        public virtual async Task<ManagedIdentityResponse> AuthenticateAsync(AppTokenProviderParameters parameters, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _requestContext.Logger.Error(TimeoutError);
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Convert the scopes to a resource string.
            string resource = ScopeHelper.ScopesToResource(parameters.Scopes.ToArray());

            ManagedIdentityRequest request = CreateRequest(resource);

            try
            {
                HttpResponse response =
            request.Method == HttpMethod.Get ?
            await _requestContext.ServiceBundle.HttpManager.SendGetForceResponseAsync(request.ComputeUri(), request.Headers, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false) :
            await _requestContext.ServiceBundle.HttpManager.SendPostForceResponseAsync(request.ComputeUri(), request.Headers, request.BodyParameters, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);

                return await HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
            }
            catch(TaskCanceledException)
            {
                _requestContext.Logger.Error(TimeoutError);
                throw;
            }
        }

        protected virtual Task<ManagedIdentityResponse> HandleResponseAsync(
            AppTokenProviderParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            string message;
            Exception exception = null;

            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _requestContext.Logger.Info("[Managed Identity] Successful response received.");
                    return Task.FromResult(GetSuccessfulResponse(response));
                }

                message = GetMessageFromErrorResponse(response);
                _requestContext.Logger.Error($"[Managed Identity] request failed, HttpStatusCode: {response.StatusCode} Error message: {message}");
            }
            catch (Exception e) when (e is not MsalServiceException)
            {
                _requestContext.Logger.Error($"[Managed Identity] Exception: {e.Message} Http status code: {response?.StatusCode}");
                exception = e;
                message = MsalErrorMessage.ManagedIdentityUnexpectedResponse;
            }

            throw new MsalServiceException(MsalError.ManagedIdentityRequestFailed, message, exception);
        }

        protected abstract ManagedIdentityRequest CreateRequest(string resource);

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);

            if (managedIdentityResponse == null || managedIdentityResponse.AccessToken.IsNullOrEmpty() || managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                _requestContext.Logger.Error("[Managed Identity] Response is either null or insufficient for authentication.");
                throw new MsalServiceException(MsalError.ManagedIdentityRequestFailed, MsalErrorMessage.ManagedIdentityInvalidResponse);
            }

            return managedIdentityResponse;
        }

        internal string GetMessageFromErrorResponse(HttpResponse response)
        {
            var managedIdentityErrorResponse = JsonHelper.TryToDeserializeFromJson<ManagedIdentityErrorResponse>(response?.Body);

            if (managedIdentityErrorResponse == null)
            {
                return MsalErrorMessage.ManagedIdentityNoResponseReceived;
            }

            if (!string.IsNullOrEmpty(managedIdentityErrorResponse.Message))
            { 
                return $"[Managed Identity] Error Message: {managedIdentityErrorResponse.Message} Managed Identity Correlation ID: {managedIdentityErrorResponse.CorrelationId} Use this Correlation ID for further investigation.";
            }

            return $"[Managed Identity] Error Code: {managedIdentityErrorResponse.Error} Error Message: {managedIdentityErrorResponse.ErrorDescription}";
        }
    }
}
