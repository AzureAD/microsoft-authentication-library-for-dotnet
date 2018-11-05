//----------------------------------------------------------------------
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

using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using System;
using System.Globalization;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Implementation of the <see cref="CoreExceptionFactory"/> that throws <see cref="AdalException"/>
    /// </summary>
    /// <remarks>Does not currently throw <see cref="AdalSilentTokenAcquisitionException"/> and 
    /// <see cref="AdalUserMismatchException"/></remarks>
    internal class AdalExceptionFactory : CoreExceptionFactory
    {
        public override Exception GetClientException(
            string errorCode,
            string errorMessage,
            Exception innerException = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            return new AdalException(errorCode, errorMessage, innerException);
        }

        public override Exception GetServiceException(string errorCode, string errorMessage, IHttpWebResponse httpResponse)
        {
            return GetServiceException(errorCode, errorMessage, null, ExceptionDetail.FromHttpResponse(httpResponse));
        }

        public override Exception GetServiceException(string errorCode, string errorMessage, ExceptionDetail exceptionDetail)
        {
            return GetServiceException(errorCode, errorMessage, null, exceptionDetail);
        }

        public override Exception GetServiceException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ExceptionDetail exceptionDetail)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            if (exceptionDetail?.Claims != null)
            {
                return new AdalClaimChallengeException(errorCode, errorMessage, innerException, exceptionDetail.Claims)
                {
                    StatusCode = exceptionDetail != null ? exceptionDetail.StatusCode : 0,
                    ServiceErrorCodes = exceptionDetail?.ServiceErrorCodes,
                    Headers = exceptionDetail?.HttpResponseHeaders
                };
            }

            return new AdalServiceException(
                errorCode,
                errorMessage,
                exceptionDetail?.ServiceErrorCodes,
                innerException)
            {
                StatusCode = exceptionDetail != null ? exceptionDetail.StatusCode : 0,
                ServiceErrorCodes = exceptionDetail?.ServiceErrorCodes,
                Headers = exceptionDetail?.HttpResponseHeaders
            };
        }

        public override Exception GetUiRequiredException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ExceptionDetail exceptionDetail)
        {
            // Adal does not define a specific ui required exception
            return GetClientException(errorCode, errorMessage, innerException);
        }

        public override string GetPiiScrubbedDetails(Exception ex)
        {
            return GetPiiScrubbedExceptionDetails(ex);
        }

        public static string GetPiiScrubbedExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex != null)
            {
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Exception type: {0}", ex.GetType()));

                if (ex is AdalException)
                {
                    var adalException = (AdalException)ex;
                    sb.AppendLine(string.Format(CultureInfo.CurrentCulture, ", ErrorCode: {0}", adalException.ErrorCode));
                }

                if (ex is AdalServiceException)
                {
                    var adalServiceException = (AdalServiceException)ex;
                    sb.AppendLine(string.Format(CultureInfo.CurrentCulture, ", StatusCode: {0}", adalServiceException.StatusCode));
                }

                if (ex.InnerException != null)
                {
                    sb.AppendLine("---> Inner Exception Details");
                    sb.AppendLine(GetPiiScrubbedExceptionDetails(ex.InnerException));
                    sb.AppendLine("=== End of inner exception stack trace ===");
                }
                if (ex.StackTrace != null)
                {
                    sb.AppendLine(Environment.NewLine + ex.StackTrace);
                }
            }

            return sb.ToString();
        }


        private static void ValidateRequiredArgs(string errorCode, string errorMessage)
        {
            if (String.IsNullOrEmpty(errorCode))
            {
                throw new ArgumentNullException(nameof(errorCode));
            }

            if (String.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }
        }
    }
}
