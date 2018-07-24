using Microsoft.Identity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.Unit.Mocks
{
    internal class TestExceptionFactory : CoreExceptionFactory
    {
        public override Exception GetClientException(string errorCode, string errorMessage, Exception innerException = null)
        {
            return new TestClientException(errorCode, errorMessage, innerException);
        }

        public override string GetPiiScrubbedDetails(Exception exception)
        {
            return exception.ToString();
        }

        public override Exception GetServiceException(string errorCode, string errorMessage)
        {
            return GetServiceException(errorCode, errorMessage, null);
        }

        public override Exception GetServiceException(
            string errorCode, 
            string errorMessage, 
            ExceptionDetail exceptionDetail = null)
        {
            return GetServiceException(errorCode, errorMessage, null, null);
        }

        public override Exception GetServiceException(
            string errorCode, 
            string errorMessage, 
            Exception innerException = null, 
            ExceptionDetail exceptionDetail = null)
        {
            return new TestServiceException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                StatusCode = exceptionDetail != null ? exceptionDetail.StatusCode : 0,
                ResponseBody = exceptionDetail?.ResponseBody,
                IsUiRequired = false
            };
        }

        public override Exception GetUiRequiredException(string errorCode, string errorMessage, Exception innerException = null, ExceptionDetail exceptionDetail = null)
        {
            return new TestServiceException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                StatusCode = exceptionDetail != null ? exceptionDetail.StatusCode : 0,
                ResponseBody = exceptionDetail?.ResponseBody,
                IsUiRequired = true
            };
        }
    }
}
