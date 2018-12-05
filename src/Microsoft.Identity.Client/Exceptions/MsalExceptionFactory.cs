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

using System;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Client.Http;

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
        ///     Throws an <see cref="MsalClientException" /> exception
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">A user friendly message</param>
        /// <param name="httpResponse"></param>
        public static Exception GetServiceException(string errorCode, string errorMessage, IHttpWebResponse httpResponse)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            return GetServiceException(errorCode, errorMessage, null, ExceptionDetail.FromHttpResponse(httpResponse));
        }

        /// <summary>
        ///     Throws an <see cref="MsalClientException" /> exception
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">A user friendly message</param>
        /// <param name="exceptionDetail">More exception params</param>
        public static Exception GetServiceException(string errorCode, string errorMessage, ExceptionDetail exceptionDetail)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            return GetServiceException(errorCode, errorMessage, null, exceptionDetail);
        }

        /// <summary>
        ///     Throw an <see cref="MsalServiceException" /> exception
        /// </summary>
        public static Exception GetServiceException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ExceptionDetail exceptionDetail)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            return new MsalServiceException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                ResponseBody = exceptionDetail?.ResponseBody,
                StatusCode = exceptionDetail?.StatusCode ?? 0,
                Headers = exceptionDetail?.HttpResponseHeaders
            };
        }

        /// <summary>
        ///     Throw an <see cref="MsalUiRequiredException" />
        /// </summary>
        public static Exception GetUiRequiredException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ExceptionDetail exceptionDetail)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            return new MsalUiRequiredException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                ResponseBody = exceptionDetail?.ResponseBody,
                StatusCode = exceptionDetail?.StatusCode ?? 0
            };
        }

        public static string GetPiiScrubbedDetails(Exception ex)
        {
            return GetPiiScrubbedExceptionDetails(ex);
        }

        public static string GetPiiScrubbedExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex != null)
            {
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Exception type: {0}", ex.GetType()));

                if (ex is MsalException msalException)
                {
                    sb.AppendLine(string.Format(CultureInfo.CurrentCulture, ", ErrorCode: {0}", msalException.ErrorCode));
                }

                if (ex is MsalServiceException msalServiceException)
                {
                    sb.AppendLine(
                        string.Format(CultureInfo.CurrentCulture, ", StatusCode: {0}", msalServiceException.StatusCode));
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