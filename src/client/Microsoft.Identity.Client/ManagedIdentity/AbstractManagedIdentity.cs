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
    internal abstract class AbstractManagedIdentity
    {
        protected readonly RequestContext _requestContext;
        internal const string TimeoutError = "[Managed Identity] Authentication unavailable. The request to the managed identity endpoint timed out.";
        internal readonly ManagedIdentitySource _sourceType;
        private const string ManagedIdentityPrefix = "[Managed Identity] ";

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

            HttpResponse response;

            // Convert the scopes to a resource string.
            string resource = parameters.Resource;

            ManagedIdentityRequest request = CreateRequest(resource);

            _requestContext.Logger.Info("[Managed Identity] Sending request to managed identity endpoints.");

            try
            {
                if (request.Method == HttpMethod.Get)
                {
                    response = await _requestContext.ServiceBundle.HttpManager
                        .SendRequestAsync(
                            request.ComputeUri(),
                            request.Headers,
                            body: null,
                            HttpMethod.Get,
                            logger: _requestContext.Logger,
                            doNotThrow: true,
                            mtlsCertificate: null,
                            GetHttpClientWithSslValidation(_requestContext),
                            cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    response = await _requestContext.ServiceBundle.HttpManager
                        .SendRequestAsync(
                            request.ComputeUri(),
                            request.Headers,
                            body: new FormUrlEncodedContent(request.BodyParameters),
                            HttpMethod.Post,
                            logger: _requestContext.Logger,
                            doNotThrow: true,
                            mtlsCertificate: null,
                            GetHttpClientWithSslValidation(_requestContext),
                            cancellationToken)
                        .ConfigureAwait(false);

                }

                return await HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
        }

        // This method is internal for testing purposes.
        internal virtual HttpClient GetHttpClientWithSslValidation(RequestContext requestContext)
        {
            return null;
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

            try
            {
                ManagedIdentityErrorResponse managedIdentityErrorResponse = JsonHelper.DeserializeFromJson<ManagedIdentityErrorResponse>(response?.Body);
                return ExtractErrorMessageFromManagedIdentityErrorResponse(managedIdentityErrorResponse);
            }
            catch
            {
                return TryGetMessageFromNestedErrorResponse(response.Body);
            } 
        }

        private string ExtractErrorMessageFromManagedIdentityErrorResponse(ManagedIdentityErrorResponse managedIdentityErrorResponse)
        {
            StringBuilder stringBuilder = new StringBuilder(ManagedIdentityPrefix);

            if (!string.IsNullOrEmpty(managedIdentityErrorResponse.Error))
            {
                stringBuilder.Append($"Error Code: {managedIdentityErrorResponse.Error} ");
            }

            if (!string.IsNullOrEmpty(managedIdentityErrorResponse.Message))
            {
                stringBuilder.Append($"Error Message: {managedIdentityErrorResponse.Message} ");
            }

            if (!string.IsNullOrEmpty(managedIdentityErrorResponse.ErrorDescription))
            {
                stringBuilder.Append($"Error Description: {managedIdentityErrorResponse.ErrorDescription} ");
            }

            if (!string.IsNullOrEmpty(managedIdentityErrorResponse.CorrelationId))
            {
                stringBuilder.Append($"Managed Identity Correlation ID: {managedIdentityErrorResponse.CorrelationId} Use this Correlation ID for further investigation.");
            }

            if (stringBuilder.Length == ManagedIdentityPrefix.Length)
            {
                return $"{MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse}.";
            }

            return stringBuilder.ToString();
        }

        // Try to get the error message from the nested error response in case of cloud shell.
        private string TryGetMessageFromNestedErrorResponse(string response)
        {
            try
            {
                var json = JsonHelper.ParseIntoJsonObject(response);

                JsonHelper.TryGetValue(json, "error", out var error);

                StringBuilder errorMessage = new StringBuilder(ManagedIdentityPrefix);

                if (JsonHelper.TryGetValue(JsonHelper.ToJsonObject(error), "code", out var errorCode))
                {
                    errorMessage.Append($"Error Code: {errorCode} ");
                }

                if (JsonHelper.TryGetValue(JsonHelper.ToJsonObject(error), "message", out var message))
                {
                    errorMessage.Append($"Error Message: {message}");
                }

                if (message != null || errorCode != null)
                {
                    return errorMessage.ToString();
                }
            } catch
            {
                // Ignore any exceptions that occur during parsing and send the error message.
            }

            _requestContext.Logger.Error($"{MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse}. Error response received from the server: {response}.");
            return $"{MsalErrorMessage.ManagedIdentityUnexpectedErrorResponse}. Error response received from the server: {response}.";
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
