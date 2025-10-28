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
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Microsoft.Identity.Client.Http.Retry;
using System.Collections.Generic;
using System.Linq;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal abstract class AbstractManagedIdentity
    {
        private const string ManagedIdentityPrefix = "[Managed Identity] ";

        protected readonly RequestContext _requestContext;

        protected bool _isMtlsPopRequested;

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

            HttpResponse response;

            // Convert the scopes to a resource string.
            string resource = parameters.Resource;

            _isMtlsPopRequested = parameters.IsMtlsPopRequested;

            ManagedIdentityRequest request = await CreateRequestAsync(resource).ConfigureAwait(false);

            // When IMDSv2 mints a binding certificate during this request (via CSR),
            // it's exposed via request.MtlsCertificate. Bubble it up so the request
            // layer can set the mtls_pop scheme
            if (parameters.IsMtlsPopRequested && request?.MtlsCertificate != null)
            {
                parameters.MtlsCertificate = request.MtlsCertificate;
            }

            // Automatically add claims / capabilities if this MI source supports them
            if (_sourceType.SupportsClaimsAndCapabilities())
            {
                request.AddClaimsAndCapabilities(
                    _requestContext.ServiceBundle.Config.ClientCapabilities,
                    parameters,
                    _requestContext.Logger);
            }

            request.AddExtraQueryParams(
                _requestContext.ServiceBundle.Config.ExtraQueryParameters,
                _requestContext.Logger);

            _requestContext.Logger.Info("[Managed Identity] Sending request to managed identity endpoints.");

            IRetryPolicy retryPolicy = _requestContext.ServiceBundle.Config.RetryPolicyFactory.GetRetryPolicy(request.RequestType);

            try
            {
                if (request.Method == HttpMethod.Get)
                {
                    response = await _requestContext.ServiceBundle.HttpManager
                        .SendRequestAsync(
                            request.ComputeUri(),
                            request.Headers,
                            body: null,
                            method: HttpMethod.Get,
                            logger: _requestContext.Logger,
                            doNotThrow: true,
                            mtlsCertificate: request.MtlsCertificate,
                            validateServerCertificate: GetValidationCallback(),
                            cancellationToken: cancellationToken,
                            retryPolicy: retryPolicy).ConfigureAwait(false);
                }
                else
                {
                    response = await _requestContext.ServiceBundle.HttpManager
                        .SendRequestAsync(
                            request.ComputeUri(),
                            request.Headers,
                            body: new FormUrlEncodedContent(request.BodyParameters),
                            method: HttpMethod.Post,
                            logger: _requestContext.Logger,
                            doNotThrow: true,
                            mtlsCertificate: request.MtlsCertificate,
                            validateServerCertificate: GetValidationCallback(),
                            cancellationToken: cancellationToken,
                            retryPolicy: retryPolicy)
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

        /// <summary>
        /// Method to be overridden in the derived classes to provide a custom validation callback for the server certificate.
        /// This validation is needed for service fabric managed identity endpoints.
        /// </summary>
        /// <returns>Callback to validate the server certificate.</returns>
        internal virtual Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> GetValidationCallback()
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

        protected abstract Task<ManagedIdentityRequest> CreateRequestAsync(string resource);

        protected ManagedIdentityResponse GetSuccessfulResponse(HttpResponse response)
        {
            ManagedIdentityResponse managedIdentityResponse;
            try
            {
                managedIdentityResponse = JsonHelper.DeserializeFromJson<ManagedIdentityResponse>(response.Body);
            }
            catch (JsonException ex)
            {
                _requestContext.Logger.Error("[Managed Identity] MSI json response failed to parse. " + ex);

                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityResponseParseFailure,
                    MsalErrorMessage.ManagedIdentityJsonParseFailure,
                    ex,
                    _sourceType,
                    (int)HttpStatusCode.OK);

                throw exception;
            }

            if (managedIdentityResponse == null ||
                managedIdentityResponse.AccessToken.IsNullOrEmpty() ||
                managedIdentityResponse.ExpiresOn.IsNullOrEmpty())
            {
                _requestContext.Logger.Error("[Managed Identity] Response is either null or insufficient for authentication.");

                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidResponse,
                    null,
                    _sourceType,
                    (int)HttpStatusCode.OK);

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
            }
            catch
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
