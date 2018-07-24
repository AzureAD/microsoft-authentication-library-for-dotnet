using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Implementation of the <see cref="CoreExceptionFactory"/> that throws <see cref="MsalException"/>
    /// </summary>
    internal class MsalExceptionFactory : CoreExceptionFactory
    {
        /// <summary>
        /// Throws an MsalClient exception
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">A user friendly message</param>
        /// <param name="innerException">Optionally an inner exception</param>
        /// <remarks>The error code should be made available in MSAL through a public constant</remarks>
        public override Exception GetClientException(
            string errorCode, 
            string errorMessage, 
            Exception innerException = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            return new MsalClientException(errorCode, errorMessage, innerException);
        }


        /// <summary>
        /// Throws an <see cref="MsalClientException"/> exception
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">A user friendly message</param>
        public override Exception GetServiceException(
            string errorCode,
            string errorMessage)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            return GetServiceException(errorCode, errorMessage, null, null);
        }

        /// <summary>
        /// Throws an <see cref="MsalClientException"/> exception
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <param name="errorMessage">A user friendly message</param>
        /// <param name="exceptionDetail">More exception params</param>
        public override Exception GetServiceException(
            string errorCode, 
            string errorMessage,
            ExceptionDetail exceptionDetail = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);
            return GetServiceException(errorCode, errorMessage, null, exceptionDetail);
        }

        /// <summary>
        /// Throw an <see cref="MsalServiceException"/> exception
        /// </summary>
        public override Exception GetServiceException(
            string errorCode, 
            string errorMessage, 
            Exception innerException = null, 
            ExceptionDetail exceptionDetail = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            return new MsalServiceException(
                 errorCode,
                 errorMessage,
                 innerException)
            {

                Claims = exceptionDetail?.Claims,
                ResponseBody = exceptionDetail?.ResponseBody,
                StatusCode = exceptionDetail != null ? exceptionDetail.StatusCode : 0
            };
        }

        /// <summary>
        /// Throw an <see cref="MsalUiRequiredException"/>
        /// </summary>
        public override Exception GetUiRequiredException(
            string errorCode, 
            string errorMessage, 
            Exception innerException,
            ExceptionDetail exceptionDetail = null)
        {
            ValidateRequiredArgs(errorCode, errorMessage);

            return new MsalUiRequiredException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                ResponseBody = exceptionDetail?.ResponseBody,
                StatusCode = exceptionDetail != null ? exceptionDetail.StatusCode : 0
            };
        }

        public override string GetPiiScrubbedDetails(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex != null)
            {
                sb.AppendLine(String.Format(CultureInfo.CurrentCulture, "Exception type: {0}", ex.GetType()));

                if (ex is MsalException)
                {
                    MsalException msalException = ex as MsalException;
                    sb.AppendLine(String.Format(
                        CultureInfo.CurrentCulture,
                        ", ErrorCode: {0}",
                        msalException.ErrorCode));
                }

                if (ex is MsalServiceException)
                {
                    MsalServiceException msalServiceException = ex as MsalServiceException;
                    sb.AppendLine(String.Format(CultureInfo.CurrentCulture, ", StatusCode: {0}",
                        msalServiceException.StatusCode));
                }

                if (ex.InnerException != null)
                {
                    sb.AppendLine("---> Inner Exception Details");
                    sb.AppendLine(GetPiiScrubbedDetails(ex.InnerException));
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
