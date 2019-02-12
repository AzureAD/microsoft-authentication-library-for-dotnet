// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Identity.Client.Exceptions
{
    /// <summary>
    ///     Factory to manage and throw proper exceptions for MSAL.
    /// </summary>
    internal static class MsalExceptionFactory
    {
        /// <summary>
        ///     Throws an MsalClient exception
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">A user friendly message</param>
        /// <param name="innerException">Optionally an inner exception</param>
        /// <remarks>The error code should be made available in MSAL through a public constant</remarks>
        public static Exception GetClientException(string errorCode, string errorMessage, Exception innerException = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            return new MsalClientException(errorCode, errorMessage, innerException);
        }

        /// <summary>
        ///     Throw an <see cref="MsalServiceException" /> exception. 
        ///     All details should be passed in if available.
        /// </summary>
        public static Exception GetServiceException(
            string errorCode,
            string errorMessage,
            HttpResponse httpResponse = null,
            Exception innerException = null,
            bool isUiRequiredException = false)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            MsalServiceException exception = isUiRequiredException ?
                new MsalUiRequiredException(errorCode, errorMessage, innerException) :
                new MsalServiceException(errorCode, errorMessage, innerException);

            // In most cases we can deserialize the body to get more details such as the suberror
            OAuth2ResponseBase oAuth2Response =
                JsonHelper.TryToDeserializeFromJson<OAuth2ResponseBase>(httpResponse?.Body);

            UpdateException(httpResponse, oAuth2Response, exception);

            return exception;
        }

        public static Exception GetServiceException(
            string errorCode,
            string errorMessage,
            OAuth2ResponseBase oAuth2Response
            )
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            MsalServiceException exception = new MsalServiceException(errorCode, errorMessage);
            UpdateException(null, oAuth2Response, exception);

            return exception;
        }

        private static void UpdateException(HttpResponse httpResponse, OAuth2ResponseBase oAuth2Response, MsalServiceException exception)
        {
            exception.ResponseBody = httpResponse?.Body;
            exception.StatusCode = httpResponse != null ? (int)httpResponse.StatusCode : 0;
            exception.Headers = httpResponse?.Headers;

            exception.Claims = oAuth2Response?.Claims;
            exception.SubError = oAuth2Response?.SubError;
            exception.CorrelationId = oAuth2Response?.CorrelationId;
        }

        public static string GetPiiScrubbedExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex != null)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Exception type: {0}", ex.GetType()));

                if (ex is MsalException msalException)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, ", ErrorCode: {0}", msalException.ErrorCode));
                }

                if (ex is MsalServiceException msalServiceException)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "HTTP StatusCode {0}", msalServiceException.StatusCode));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "SubError {0}", msalServiceException.SubError));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "CorrelationId {0}", msalServiceException.CorrelationId));
                }

                if (ex.InnerException != null)
                {
                    sb.AppendLine("---> Inner Exception Details");
                    sb.AppendLine(GetPiiScrubbedExceptionDetails(ex.InnerException));
                    sb.AppendLine("=== End of inner exception stack trace ===");
                }

                if (ex.StackTrace != null)
                {
                    sb.Append(Environment.NewLine + ex.StackTrace);
                }
            }

            return sb.ToString();
        }

        private static void ValidateRequiredArgs(string errorCode, string errorMessage)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                throw new ArgumentNullException(nameof(errorCode));
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }
        }
    }
}