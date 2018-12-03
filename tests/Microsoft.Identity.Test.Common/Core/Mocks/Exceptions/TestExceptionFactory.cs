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
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;

namespace Microsoft.Identity.Test.Common.Core.Mocks.Exceptions
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

        public override Exception GetServiceException(string errorCode, string errorMessage, IHttpWebResponse response)
        {
            return GetServiceException(errorCode, errorMessage, null, ExceptionDetail.FromHttpResponse(response));
        }
        
        public override Exception GetServiceException(
            string errorCode,
            string errorMessage,
            ExceptionDetail exceptionDetail)
        {
            return GetServiceException(errorCode, errorMessage, null, null);
        }

        public override Exception GetServiceException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ExceptionDetail exceptionDetail)
        {
            return new TestServiceException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                StatusCode = exceptionDetail?.StatusCode ?? 0,
                ResponseBody = exceptionDetail?.ResponseBody,
                IsUiRequired = false
            };
        }

        public override Exception GetUiRequiredException(
            string errorCode,
            string errorMessage,
            Exception innerException,
            ExceptionDetail exceptionDetail)
        {
            return new TestServiceException(errorCode, errorMessage, innerException)
            {
                Claims = exceptionDetail?.Claims,
                StatusCode = exceptionDetail?.StatusCode ?? 0,
                ResponseBody = exceptionDetail?.ResponseBody,
                IsUiRequired = true
            };
        }
    }
}