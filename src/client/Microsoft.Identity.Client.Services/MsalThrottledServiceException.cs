// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Exception type thrown when MSAL detects that an application is trying to acquire a token too often, as a result of: 
    /// - A previous request resulted in an HTTP response containing a Retry-After header which was not followed.
    /// - A previous request resulted in an HTTP 429 or 5xx, which indicates a problem with the server.
    ///     
    /// The properties of this exception are identical to the original exception
    /// 
    /// For more details see https://aka.ms/msal-net-throttling
    /// </summary>
    public class MsalThrottledServiceException : MsalServiceException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MsalThrottledServiceException(MsalServiceException originalException) : 
            base(
                originalException.ErrorCode, 
                originalException.Message, 
                originalException.InnerException)
        {
            SubError = originalException.SubError;
            StatusCode = originalException.StatusCode;
            Claims = originalException.Claims;
            CorrelationId = originalException.CorrelationId;
            ResponseBody = originalException.ResponseBody;
            Headers = originalException.Headers;

            OriginalServiceException = originalException;
        }

        /// <summary>
        /// The original service exception that triggered the throttling.
        /// </summary>
        public MsalServiceException OriginalServiceException { get; }
    }
}
