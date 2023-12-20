﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.ManagedIdentity;

#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json.Serialization;
using JObject = System.Text.Json.Nodes.JsonObject;
#else
using Microsoft.Identity.Json.Linq;
#endif
namespace Microsoft.Identity.Client
{

    /// <summary>
    /// Exception type thrown when service returns an error response or other networking errors occur.
    /// For more details, see https://aka.ms/msal-net-exceptions
    /// </summary>
    public class MsalServiceException : MsalException
    {
        private const string ClaimsKey = "claims";
        private const string ResponseBodyKey = "response_body";
        private const string CorrelationIdKey = "correlation_id";
        private const string SubErrorKey = "sub_error";
        private int _statusCode = 0;
        private string _responseBody;
        private HttpResponseHeaders _headers;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="errorCode">
        /// The protocol error code returned by the service or generated by client. This is the code you
        /// can rely on for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        public MsalServiceException(string errorCode, string errorMessage)
            : base(errorCode, errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }
            UpdateIsRetryable();
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="errorCode">
        /// The protocol error code returned by the service or generated by the client. This is the code you
        /// can rely on for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="statusCode">Status code of the response received from the service.</param>
        public MsalServiceException(string errorCode, string errorMessage, int statusCode)
            : this(errorCode, errorMessage)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="errorCode">
        /// The protocol error code returned by the service or generated by the client. This is the code you
        /// can rely on for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference if no inner
        /// exception is specified.
        /// </param>
        public MsalServiceException(string errorCode, string errorMessage,
            Exception innerException)
            : base(errorCode, errorMessage, innerException)
        {
            UpdateIsRetryable();
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="errorCode">
        /// The protocol error code returned by the service or generated by the client. This is the code you
        /// can rely on for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="statusCode">HTTP status code of the response received from the service.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference if no inner
        /// exception is specified.
        /// </param>
        public MsalServiceException(string errorCode, string errorMessage, int statusCode,
            Exception innerException)
            : base(
                errorCode, errorMessage, innerException)
        {
            StatusCode = statusCode;
            UpdateIsRetryable();
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="errorCode">
        /// The protocol error code returned by the service or generated by the client. This is the code you
        /// can rely on for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="statusCode">The status code of the request.</param>
        /// <param name="claims">The claims challenge returned back from the service.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception, or a null reference if no inner
        /// exception is specified.
        /// </param>
        public MsalServiceException(
            string errorCode,
            string errorMessage,
            int statusCode,
            string claims,
            Exception innerException)
            : this(errorCode, errorMessage, statusCode, innerException)
        {
            Claims = claims;
        }

        #endregion

        // Important: to allow developers to unit test MSAL, we need to ensure that all properties have public setters or can be set via
        // public constructors

        #region Public Properties
        /// <summary>
        /// Gets the status code returned from HTTP layer. This status code is either the <c>HttpStatusCode</c> in the inner
        /// <see cref="System.Net.Http.HttpRequestException"/> response or the NavigateError Event Status Code in a browser based flow (See
        /// http://msdn.microsoft.com/en-us/library/bb268233(v=vs.85).aspx).
        /// You can use this code for purposes such as implementing retry logic or error investigation.
        /// </summary>
        public int StatusCode
        {
            get { return _statusCode; }
            internal set
            {
                _statusCode = value;
                UpdateIsRetryable();
            }
        }
        /// <summary>
        /// Additional claims requested by the service. When this property is not null or empty, this means that the service requires the user to
        /// provide additional claims, such as doing two factor authentication. The are two cases:
        /// <list type="bullet">
        /// <item><description>
        /// If your application is a <see cref="IPublicClientApplication"/>, you should just call <see cref="IPublicClientApplication.AcquireTokenInteractive(System.Collections.Generic.IEnumerable{string})"/>
        /// and add the <see cref="AbstractAcquireTokenParameterBuilder{T}.WithClaims(string)"/> modifier.
        /// </description></item>
        /// <item>><description>If your application is a <see cref="IConfidentialClientApplication"/>, (therefore doing the On-Behalf-Of flow), you should throw an HTTP unauthorize
        /// exception with a message containing the claims</description></item>
        /// </list>
        /// For more details see https://aka.ms/msal-net-claim-challenge
        /// </summary>
#if SUPPORTS_SYSTEM_TEXT_JSON
        [JsonInclude]
#endif
        public string Claims { get; internal set; }

        /// <summary>
        /// Raw response body received from the server.
        /// </summary>
        public string ResponseBody
        {
            get => _responseBody;
            set
            {
                _responseBody = value;
                UpdateIsRetryable();
            }
        }

        /// <summary>
        /// Contains the HTTP headers from the server response that indicated an error.
        /// </summary>
        /// <remarks>
        /// When the server returns a 429 Too Many Requests error, a Retry-After should be set. It is important to read and respect the
        /// time specified in the Retry-After header to avoid a retry storm.
        /// </remarks>
        public HttpResponseHeaders Headers
        {
            get => _headers;
            set
            {
                _headers = value;
                UpdateIsRetryable();
            }
        }

        /// <summary>
        /// An ID that can used to piece up a single authentication flow.
        /// </summary>
        public string CorrelationId { get; set; }

        #endregion

        /// <remarks>
        /// The suberror should not be exposed for public consumption yet, as STS needs to do some work first.
        /// </remarks>
        internal string SubError { get; set; }

        /// <summary>
        /// A list of STS-specific error codes that can help in diagnostics.
        /// </summary>
        internal string[] ErrorCodes { get; set; }

        /// <summary>
        /// As per discussion with Evo, AAD 
        /// </summary>
        protected virtual void UpdateIsRetryable()
        {
            //To-Do : need to find a better way to do this
            if (ErrorCode.StartsWith("managed_identity"))
            {
                IsRetryable = StatusCode switch
                {
                    404 or 408 or 429 or 500 or 503 or 504 => true,
                    _ => false,
                };
            }
            else
            {
                IsRetryable =
                    (StatusCode >= 500 && StatusCode < 600) ||
                    StatusCode == 429 || // too many requests
                    StatusCode == (int)HttpStatusCode.RequestTimeout ||
                    string.Equals(ErrorCode, MsalError.RequestTimeout, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ErrorCode, "temporarily_unavailable", StringComparison.OrdinalIgnoreCase); // as per https://docs.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes#handling-error-codes-in-your-application
            }
        }

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            return base.ToString() + string.Format(
                CultureInfo.InvariantCulture,
                "\n\tStatusCode: {0} \n\tResponseBody: {1} \n\tHeaders: {2}",
                StatusCode,
                ResponseBody,
                Headers);
        }

        #region Serialization
        internal override void PopulateJson(JObject jObject)
        {
            base.PopulateJson(jObject);

            jObject[ClaimsKey] = Claims;
            jObject[ResponseBodyKey] = ResponseBody;
            jObject[CorrelationIdKey] = CorrelationId;
            jObject[SubErrorKey] = SubError;
        }

        internal override void PopulateObjectFromJson(JObject jObject)
        {
            base.PopulateObjectFromJson(jObject);

            Claims = JsonHelper.GetExistingOrEmptyString(jObject, ClaimsKey);
            ResponseBody = JsonHelper.GetExistingOrEmptyString(jObject, ResponseBodyKey);
            CorrelationId = JsonHelper.GetExistingOrEmptyString(jObject, CorrelationIdKey);
            SubError = JsonHelper.GetExistingOrEmptyString(jObject, SubErrorKey);
        }
        #endregion
    }
}
