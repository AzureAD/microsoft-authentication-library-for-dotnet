// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Exception type thrown when MSAL detects that an application is trying to acquire a token even 
    /// though an <see cref="MsalUiRequiredException"/> was recently thrown. 
    /// To mitigate this, when a <see cref="MsalUiRequiredException"/> is encountered,
    /// the application should switch to acquiring a token interactively. To better understand
    /// why the <see cref="MsalUiRequiredException" /> was thrown, inspect the <see cref="MsalUiRequiredException.Classification"/>
    /// property.
    /// 
    /// The properties of this exception are identical to the original exception
    /// 
    /// For more details see https://aka.ms/msal-net-throttling
    /// </summary>
    public class MsalThrottledUiRequiredException : MsalUiRequiredException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MsalThrottledUiRequiredException(MsalUiRequiredException originalException) : 
            base(
                originalException.ErrorCode, 
                originalException.Message, 
                originalException.InnerException, 
                originalException.Classification)
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
        /// The original exception that triggered the throttling.
        /// </summary>
        public MsalUiRequiredException OriginalServiceException { get; }
    }
}
