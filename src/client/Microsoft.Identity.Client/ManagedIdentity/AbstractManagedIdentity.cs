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
using System.Collections.Generic;

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
            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _requestContext.Logger.Info("[Managed Identity] Successful response received.");
                    return Task.FromResult(GetSuccessfulResponse(response));
                }

                string message = GetMessageFromErrorResponse(response);
                _requestContext.Logger.Error($"[Managed Identity] request failed, HttpStatusCode: {response.StatusCode} Error message: {message}");
                ThrowManagedIdentityException(MsalError.ManagedIdentityRequestFailed, message, null, ManagedIdentitySource.None, (int)response.StatusCode);
            }
            catch (Exception e)
            {
                HandleException(e);
                throw;
            }

            return Task.FromResult<ManagedIdentityResponse>(null);
        }

        protected abstract ManagedIdentityRequest CreateRequest(string resource);

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);

            if (managedIdentityResponse == null || managedIdentityResponse.AccessToken.IsNullOrEmpty() || managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                _requestContext.Logger.Error("[Managed Identity] Response is either null or insufficient for authentication.");
                ThrowManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidResponse);
            }

            return managedIdentityResponse;
        }

        internal string GetMessageFromErrorResponse(HttpResponse response)
        {
            ManagedIdentityErrorResponse managedIdentityErrorResponse = JsonHelper.TryToDeserializeFromJson<ManagedIdentityErrorResponse>(response?.Body);

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

        internal void HandleException(Exception ex, ManagedIdentitySource source = ManagedIdentitySource.None, string additionalInfo = null)
        {
            if (ex is HttpRequestException httpRequestException)
            {
                ThrowManagedIdentityException(
                    MsalError.ManagedIdentityUnreachableNetwork,
                    httpRequestException.Message,
                    httpRequestException.InnerException);
            }
            else if (ex is TaskCanceledException taskCanceledException)
            {
                _requestContext.Logger.Error(TimeoutError);
                throw taskCanceledException;
            }
            else if (ex is FormatException formatException)
            {
                string errorMessage = additionalInfo ?? formatException.Message;
                _requestContext.Logger.Error($"[Managed Identity] Format Exception: {errorMessage}");
                ThrowManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    formatException,
                    source);
            }
            else if (ex is not MsalServiceException)
            {
                _requestContext.Logger.Error($"[Managed Identity] Exception: {ex.Message}");
                
                ThrowManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    ex.Message,
                    ex);
            }
            else
            {
                // If it's already a MsalServiceException, rethrow it
                throw ex;
            }
        }

        internal void ThrowManagedIdentityException(
            string errorCode,
            string errorMessage,
            Exception innerException = null,
            ManagedIdentitySource managedIdentitySource = ManagedIdentitySource.None,
            int? statusCode = null)
        {
            ManagedIdentitySource source = managedIdentitySource != ManagedIdentitySource.None ? managedIdentitySource : _sourceType;

            ThrowServiceException(errorCode, errorMessage, innerException, source, statusCode);
        }

        internal static void ThrowServiceException(
            string errorCode, 
            string errorMessage,
            Exception innerException,
            ManagedIdentitySource managedIdentitySource,
            int? statusCode = null)
        {
            MsalException ex;

            if (statusCode.HasValue)
            {
                ex = new MsalServiceException(errorCode, errorMessage, (int)statusCode, innerException);
            }
            else if (innerException != null)
            {
                ex = new MsalServiceException(errorCode, errorMessage, innerException);
            }
            else
            {
                ex = new MsalServiceException(errorCode, errorMessage);
            }

            ex = DecorateExceptionWithManagedIdentitySource(ex, managedIdentitySource);
            throw ex;
        }

        internal static MsalException DecorateExceptionWithManagedIdentitySource(
            MsalException exception, 
            ManagedIdentitySource managedIdentitySource)
        {
            var result = new Dictionary<string, string>()
            {
                { MsalException.ManagedIdentitySource, managedIdentitySource.ToString() }
            };

            exception.AdditionalExceptionData = result;

            return exception;
        }
    }
}
