// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;
using System.Net;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using System.Text;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/ManagedIdentitySource.cs
    /// </summary>
    internal abstract class AbstractManagedIdentity
    {
        protected readonly RequestContext _requestContext;
        internal const string TimeoutError = "[Managed Identity] Authentication unavailable. The request to the managed identity endpoint timed out.";
        internal readonly ManagedIdentitySource _sourceType;

        protected AbstractManagedIdentity(RequestContext requestContext, ManagedIdentitySource sourceType)
        {
            _requestContext = requestContext;
            _sourceType = sourceType;
        }

        public virtual async Task<ManagedIdentityResponse> AuthenticateAsync(
            AcquireTokenForManagedIdentityParameters parameters, 
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _requestContext.Logger.Error(TimeoutError);
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Convert the scopes to a resource string.
            string resource = parameters.Resource;

            ManagedIdentityRequest request = CreateRequest(resource);

            try
            {
                HttpResponse response =
                    request.Method == HttpMethod.Get ?
                    await _requestContext.ServiceBundle.HttpManager
                        .SendGetForceResponseAsync(
                            request.ComputeUri(), 
                            request.Headers, 
                            _requestContext.Logger, 
                            cancellationToken: cancellationToken).ConfigureAwait(false) :
                    await _requestContext.ServiceBundle.HttpManager
                        .SendPostForceResponseAsync(
                            request.ComputeUri(), 
                            request.Headers, 
                            request.BodyParameters, 
                            _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);

                return await HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
        }

        protected virtual Task<ManagedIdentityResponse> HandleResponseAsync(
            AcquireTokenForManagedIdentityParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _requestContext.Logger.Info("[Managed Identity] Successful response received.");
                return Task.FromResult(GetSuccessfulResponse(response));
            }

            string message = GetMessageFromErrorResponse(response);
                
            _requestContext.Logger.Error($"[Managed Identity] request failed, HttpStatusCode: {response.StatusCode} Error message: {message}");

            MsalException exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                MsalError.ManagedIdentityRequestFailed,
                message,
                null,
                _sourceType,
                (int)response.StatusCode);

            throw exception;
        }

        protected abstract ManagedIdentityRequest CreateRequest(string resource);

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);

            if (managedIdentityResponse == null || managedIdentityResponse.AccessToken.IsNullOrEmpty() || managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                _requestContext.Logger.Error("[Managed Identity] Response is either null or insufficient for authentication.");

                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidResponse,
                    null, 
                    _sourceType, 
                    null); 

                    throw exception;
            }

            return managedIdentityResponse;
        }

        internal string GetMessageFromErrorResponse(HttpResponse response)
        {
            if (string.IsNullOrEmpty(response?.Body))
            {
                return MsalErrorMessage.ManagedIdentityNoResponseReceived;
            }

            ManagedIdentityErrorResponse managedIdentityErrorResponse = JsonHelper.TryToDeserializeFromJson<ManagedIdentityErrorResponse>(response?.Body);

            if (managedIdentityErrorResponse == null)
            {
                return TryGetMessageFromNestedErrorResponse(response.Body);
            }

            if (!string.IsNullOrEmpty(managedIdentityErrorResponse.Message))
            { 
                return $"[Managed Identity] Error Message: {managedIdentityErrorResponse.Message} Managed Identity Correlation ID: {managedIdentityErrorResponse.CorrelationId} Use this Correlation ID for further investigation.";
            }

            return $"[Managed Identity] Error Code: {managedIdentityErrorResponse.Error} Error Message: {managedIdentityErrorResponse.ErrorDescription}";
        }

        // Try to get the error message from the nested error response in case of cloud shell.
        private string TryGetMessageFromNestedErrorResponse(string response)
        {
            var json = JsonHelper.ParseIntoJsonObject(response);

            JsonHelper.TryGetValue(json, "error", out var error);
            JsonHelper.TryGetValue(JsonHelper.ToJsonObject(error), "message", out var message);
            JsonHelper.TryGetValue(JsonHelper.ToJsonObject(error), "code", out var errorCode);

            if (message == null && errorCode == null)
            {
                _requestContext.Logger.Error(MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse + $"Error response received from the server: {response}.");
                return MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse + $"Error response received from the server: {response}.";
            }

            StringBuilder errorMessage = new StringBuilder("[Managed Identity] ");

            if (errorCode != null)
            {
                errorMessage.Append($"Error Code: {errorCode} ");
            }

            if (message != null)
            {
                errorMessage.Append($"[Managed Identity] Error Message: {message}");
            }

            return errorMessage.ToString();
        }

        private void HandleException(Exception ex, 
            ManagedIdentitySource managedIdentitySource = ManagedIdentitySource.None, 
            string additionalInfo = null)
        {
            ManagedIdentitySource source = managedIdentitySource != ManagedIdentitySource.None ? managedIdentitySource : _sourceType;

            if (ex is HttpRequestException httpRequestException)
            {
                CreateAndThrowException(MsalError.ManagedIdentityUnreachableNetwork, httpRequestException.Message, httpRequestException, source);
            }
            else if (ex is TaskCanceledException)
            {
                _requestContext.Logger.Error(TimeoutError);
            }
            else if (ex is FormatException formatException)
            {
                string errorMessage = additionalInfo ?? formatException.Message;
                _requestContext.Logger.Error($"[Managed Identity] Format Exception: {errorMessage}");
                CreateAndThrowException(MsalError.InvalidManagedIdentityEndpoint, errorMessage, formatException, source);
            }
            else if (ex is not MsalServiceException or TaskCanceledException)
            {
                _requestContext.Logger.Error($"[Managed Identity] Exception: {ex.Message}");
                CreateAndThrowException(MsalError.ManagedIdentityRequestFailed, ex.Message, ex, source);
            }
        }

        private static void CreateAndThrowException(string errorCode, 
            string errorMessage, 
            Exception innerException, 
            ManagedIdentitySource source)
        {
            MsalException exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                errorCode,
                errorMessage,
                innerException,
                source,
                null);

            throw exception;
        }
    }
}
