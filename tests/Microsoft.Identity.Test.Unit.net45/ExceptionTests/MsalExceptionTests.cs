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

using System;
using System.Net;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class MsalExceptionTests
    {
        [TestMethod]
        public void ExceptionsArePubliclyCreatable_MsalException()
        {
            var innerEx = new InvalidOperationException();
            var ex = new MsalException("code1", "msg1", innerEx);

            Assert.AreEqual("code1", ex.ErrorCode);
            Assert.AreEqual("msg1", ex.Message);
            Assert.AreSame(innerEx, ex.InnerException);
        }

        [TestMethod]
        public void ExceptionsArePubliclyCreatable_MsalSilentTokenAcquisitionException()
        {
            var ex = new MsalUiRequiredException(null, null);

            Assert.IsNull(ex.InnerException);
        }

        [TestMethod]
        public void Exception_ToString()
        {
            var innerException = new InvalidOperationException("innerMsg");
            MsalException ex = new MsalException("errCode", "errMessage", innerException);

            Assert.IsTrue(ex.ToString().Contains("errCode"));
            Assert.IsTrue(ex.ToString().Contains("errMessage"));
            Assert.IsTrue(ex.ToString().Contains("innerMsg"));
        }

        [TestMethod]
        public void ServiceException_ToString()
        {
            // Arrange
            const string jsonError = @"{ ""error"":""invalid_tenant"", ""suberror"":""MySuberror"",
            ""claims"":""some_claims"",
            ""error_description"":""AADSTS90002: Tenant 'x' not found. "", ""error_codes"":[90002],""timestamp"":""2019-01-28 14:16:04Z"",
            ""trace_id"":""43f14373-8d7d-466e-a5f1-6e3889291e00"",
            ""correlation_id"":""6347d33d-941a-4c35-9912-a9cf54fb1b3e""}";

            string innerExMsg = "innerExMsg";
            var innerException = new NotImplementedException(innerExMsg);

            HttpResponse httpResponse = new HttpResponse()
            {
                Body = jsonError,
                StatusCode = HttpStatusCode.BadRequest, // 400                
            };

            // Act
            var ex = new MsalServiceException(
                "errCode",
                "errMessage",
                innerException)
            {
                HttpResponse = httpResponse
            };

            // Assert
            Assert.IsTrue(ex.ToString().Contains("errCode"));
            Assert.IsTrue(ex.ToString().Contains("errMessage"));
            Assert.IsTrue(ex.ToString().Contains("innerExMsg"));
            Assert.IsTrue(ex.ToString().Contains("invalid_tenant"));
            Assert.IsTrue(ex.ToString().Contains("MySuberror"));
            Assert.IsTrue(ex.ToString().Contains("some_claims"));
            Assert.IsTrue(ex.ToString().Contains("AADSTS90002"));

        }
    }
}
