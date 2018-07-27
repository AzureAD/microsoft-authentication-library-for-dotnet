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
    internal class AdalEceptionFactory : CoreExceptionFactory
    {
        public override Exception GetClientException(
            string errorCode, 
            string errorMessage, 
            Exception innerException = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            return new AdalException(errorCode, errorMessage);
        }
      
        public override Exception GetServiceException(string errorCode, string errorMessage)
        {
            return GetServiceException(errorCode, errorMessage, null);
        }

        public override Exception GetServiceException(string errorCode, string errorMessage, ExceptionDetail exceptionDetail = null)
        {
            return GetServiceException(errorCode, errorMessage, null, null);
        }

        public override Exception GetServiceException(
            string errorCode, 
            string errorMessage, 
            Exception innerException = null, 
            ExceptionDetail exceptionDetail = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            if (exceptionDetail?.Claims != null)
            {
                return new AdalClaimChallengeException(errorCode, errorMessage, innerException, exceptionDetail.Claims);
            }

            return new AdalServiceException(
                errorCode,
                errorMessage,
                exceptionDetail?.ServiceErrorCodes,
                innerException);
        }

        public override Exception GetUiRequiredException(string errorCode, string errorMessage, Exception innerException = null, ExceptionDetail exceptionDetail = null)
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
