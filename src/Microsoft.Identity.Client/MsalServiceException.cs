﻿//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Exception type thrown when service returns an error response or other networking errors occur.
    /// For more details, see https://aka.ms/msal-net-exceptions
    /// </summary>
    public class MsalServiceException : MsalException
    {
        /// <summary>
        /// Service is unavailable and returned HTTP error code within the range of 500-599
        /// <para>Mitigation</para> you can retry after a delay. Note that the retry-after header is not yet
        /// surfaced in MSAL.NET (on the backlog)
        /// </summary>
        public const string ServiceNotAvailable = "service_not_available";

        /// <summary>
        /// The Http Request to the STS timed out.
        /// <para>Mitigation</para> you can retry after a delay.
        /// </summary>
        public const string RequestTimeout = "request_timeout";

        /// <summary>
        /// Upn required
        /// <para>What happens?</para> An override of a token acquisition operation was called in <see cref="T:PublicClientApplication"/> which
        /// takes a <c>loginHint</c> as a parameters, but this login hint was not using the UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c> 
        /// expected by the service
        /// <para>Remediation</para> Make sure in your code that you enforce <c>loginHint</c> to be a UPN
        /// </summary>
        public const string UpnRequired = "upn_required";

        /// <summary>
        /// No passive auth endpoint was found in the OIDC configuration of the authority
        /// <para>What happens?</para> When the libraries go to the authority and get its open id connect configuration
        /// it expects to find a Passive Auth Endpoint entry, and could not find it.
        /// <para>remediation</para> Check that the authority configured for the application, or passed on some overrides of token acquisition tokens
        /// supporting authority override is correct
        /// </summary>
        public const string MissingPassiveAuthEndpoint = "missing_passive_auth_endpoint";

        /// <summary>
        /// Invalid authority
        /// <para>What happens</para> When the library attempts to discover the authority and get the endpoints it needs to
        /// acquire a token, it got an un-authorize HTTP code or an unexpected response
        /// <para>remediation</para> Check that the authority configured for the application, or passed on some overrides of token acquisition tokens
        /// supporting authority override is correct
        /// </summary>
        public const string InvalidAuthority = "invalid_authority";

        /// <summary>
        /// Error code used when the http response returns HttpStatusCode.NotFound
        /// </summary>
        public const string HttpStatusNotFound = "not_found";

        /// <summary>
        /// ErrorCode used when the http response returns something different from 200 (OK)
        /// </summary>
        /// <remarks>
        /// HttpStatusCode.NotFound have a specific error code. <see cref="MsalServiceException.HttpStatusNotFound"/>
        /// </remarks>
        public const string HttpStatusCodeNotOk = "http_status_not_200";

        /// <summary>
        /// Broker response returned an error
        /// </summary>
        public const string BrokerResponseReturnedError = "broker_response_returned_error";

        /// <summary>
        /// An error response was returned by the OAuth2 server and it could not be parsed
        /// </summary>
        public const string NonParsableOAuthError = "non_parsable_oauth_error";

        /// <summary>
        /// Federated service returned error.
        /// </summary>
        public const string FederatedServiceReturnedError = "federated_service_returned_error";

        /// <summary>
        /// Accessing WS Metadata Exchange Failed.
        /// </summary>
        public const string AccessingWsMetadataExchangeFailed = "accessing_ws_metadata_exchange_failed";

        /// <summary>
        /// Authority validation failed.
        /// </summary>
        public const string AuthorityValidationFailed = "authority_validation_failed";

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
            : base(
                errorCode, errorMessage)
        {
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
        /// <param name="statusCode">Status code of the resposne received from the service.</param>
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
            : base(
                errorCode, errorMessage, innerException)
        {
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
        /// <param name="statusCode">HTTP status code of the resposne received from the service.</param>
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
        public MsalServiceException(string errorCode, string errorMessage, int statusCode, string claims,
            Exception innerException)
            : this(
                errorCode, errorMessage, statusCode, innerException)
        {
            Claims = claims;
        }

        /// <summary>
        /// Gets the status code returned from http layer. This status code is either the <c>HttpStatusCode</c> in the inner
        /// <see cref="T:System.Net.Http.HttpRequestException"/> response or the the NavigateError Event Status Code in a browser based flow (See
        /// http://msdn.microsoft.com/en-us/library/bb268233(v=vs.85).aspx).
        /// You can use this code for purposes such as implementing retry logic or error investigation.
        /// </summary>
        public int StatusCode { get; internal set; } = 0;

#if !DESKTOP
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
        /// <summary>
        /// Additional claims requested by the service. When this property is not null or empty, this means that the service requires the user to 
        /// provide additional claims, such as doing two factor authentication. The are two cases:
        /// <list type="bullent">
        /// <item><description>
        /// If your application is a <see cref="PublicClientApplication"/>, you should just call an override of <see cref="PublicClientApplication.AcquireTokenAsync(System.Collections.Generic.IEnumerable{string}, string, Prompt, string, System.Collections.Generic.IEnumerable{string}, string)"/>
        /// in <see cref="PublicClientApplication"/> having an <c>extraQueryParameter</c> argument, and add the following string <c>$"claims={ex.Claims}"</c>
        /// to the extraQueryParameters, where ex is an instance of this exception.
        /// </description></item>
        /// <item>><description>If your application is a <see cref="ConfidentialClientApplication"/>, (therefore doing the On-Behalf-Of flow), you should throw an Http unauthorize 
        /// exception with a message containing the claims</description></item>
        /// </list>
        /// For more details see https://aka.ms/msal-net-claim-challenge
        /// </summary>
        public string Claims { get; internal set; }
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        
        /// <summary>
        /// Raw response body received from the server.
        /// </summary>
        public string ResponseBody { get; internal set; }

        /// <summary>
        /// Contains the http headers from the server response that indicated an error. 
        /// </summary>
        /// <remarks>
        /// When the server returns a 429 Too Many Requests error, a Retry-After should be set. It is important to read and respect the 
        /// time specified in the Retry-After header to avoid a retry storm. 
        /// </remarks>
        public HttpResponseHeaders Headers { get; internal set; }

        /// <summary>
        /// An ID that can used to piece up a single authentication flow.
        /// </summary>
        public string CorrelationId { get; internal set; }

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

        private const string ClaimsKey = "claims";
        private const string ResponseBodyKey = "response_body";
        private const string CorrelationIdKey = "correlation_id";

        internal override void PopulateJson(JObject jobj)
        {
            base.PopulateJson(jobj);

            jobj[ClaimsKey] = Claims;
            jobj[ResponseBodyKey] = ResponseBody;
            jobj[CorrelationIdKey] = CorrelationId;
        }

        internal override void PopulateObjectFromJson(JObject jobj)
        {
            base.PopulateObjectFromJson(jobj);

            Claims = JsonUtils.GetExistingOrEmptyString(jobj, ClaimsKey);
            ResponseBody = JsonUtils.GetExistingOrEmptyString(jobj, ResponseBodyKey);
            CorrelationId = JsonUtils.GetExistingOrEmptyString(jobj, CorrelationIdKey);
        }
    }
}
