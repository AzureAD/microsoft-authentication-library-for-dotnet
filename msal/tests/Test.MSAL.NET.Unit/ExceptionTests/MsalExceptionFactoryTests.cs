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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Core.Http;
using System.Net;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class MsalExceptionFactoryTests
    {
        private const string exCode = "exCode";
        private const string exMessage = "exMessage";
        private MsalExceptionFactory msalExceptionService;

        [TestInitialize]
        public void Init()
        {
            msalExceptionService = new MsalExceptionFactory();
        }


        [TestMethod]
        public void ParamValidation()
        {
            AssertException.Throws<ArgumentNullException>(
                () => msalExceptionService.GetClientException(null, exMessage));

            AssertException.Throws<ArgumentNullException>(
                () => msalExceptionService.GetClientException("", exMessage));

            AssertException.Throws<ArgumentNullException>(
                () => msalExceptionService.GetServiceException(exCode, "", new ExceptionDetail()));

            AssertException.Throws<ArgumentNullException>(
                () => msalExceptionService.GetServiceException(exCode, null, new ExceptionDetail()));
        }

        [TestMethod]
        public void MsalClientException_FromCoreException()
        {
            // Act
            Exception msalException = msalExceptionService.GetClientException(exCode, exMessage);

            // Assert
            var msalClientException = msalException as MsalClientException;
            Assert.AreEqual(exCode, msalClientException.ErrorCode);
            Assert.AreEqual(exMessage, msalClientException.Message);
            Assert.IsNull(msalClientException.InnerException);

            // Act
            string piiMessage = msalExceptionService.GetPiiScrubbedDetails(msalException);

            // Assert
            Assert.IsFalse(String.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
                piiMessage.Contains(typeof(MsalClientException).Name),
                "The pii message should contain the exception type");
            Assert.IsTrue(piiMessage.Contains(exCode));
            Assert.IsFalse(piiMessage.Contains(exMessage));
        }


        [TestMethod]
        public void MsalServiceException()
        {
            // Arrange
            string claims = "claims";
            string reponseBody = "responseBody";
            int statusCode = 509;
            string innerExMsg = "innerExMsg";
            NotImplementedException innerException = new NotImplementedException(innerExMsg);

            // Act
            Exception msalException =
                msalExceptionService.GetServiceException(
                    exCode,
                    exMessage,
                    innerException,
                    new ExceptionDetail
                    {
                        Claims = claims,
                        ResponseBody = reponseBody,
                        StatusCode = statusCode
                    });

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(innerException, msalServiceException.InnerException);
            Assert.AreEqual(exCode, msalServiceException.ErrorCode);
            Assert.AreEqual(claims, msalServiceException.Claims);
            Assert.AreEqual(reponseBody, msalServiceException.ResponseBody);
            Assert.AreEqual(exMessage, msalServiceException.Message);
            Assert.AreEqual(statusCode, msalServiceException.StatusCode);

            // Act
            string piiMessage = msalExceptionService.GetPiiScrubbedDetails(msalException);

            // Assert
            Assert.IsFalse(String.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
                piiMessage.Contains(typeof(MsalServiceException).Name),
                "The pii message should contain the exception type");
            Assert.IsTrue(
               piiMessage.Contains(typeof(NotImplementedException).Name),
               "The pii message should have the inner exception type");
            Assert.IsTrue(piiMessage.Contains(exCode));
            Assert.IsFalse(piiMessage.Contains(exMessage));
            Assert.IsFalse(piiMessage.Contains(innerExMsg));

        }

        [TestMethod]
        public void MsalUiRequiredException()
        {
            // Arrange
            string innerExMsg = "innerExMsg";
            NotImplementedException innerException = new NotImplementedException(innerExMsg);

            // Act
            Exception msalException =
                 msalExceptionService.GetUiRequiredException(exCode, exMessage, innerException, null);

             // Assert
            var msalServiceException = msalException as MsalUiRequiredException;
            Assert.AreEqual(innerException, msalServiceException.InnerException);
            Assert.AreEqual(exCode, msalServiceException.ErrorCode);
            Assert.IsNull(msalServiceException.Claims);
            Assert.IsNull(msalServiceException.ResponseBody);
            Assert.AreEqual(exMessage, msalServiceException.Message);
            Assert.AreEqual(0, msalServiceException.StatusCode);

            // Act
            string piiMessage = msalExceptionService.GetPiiScrubbedDetails(msalException);

            // Assert
            Assert.IsFalse(String.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
                piiMessage.Contains(typeof(MsalUiRequiredException).Name),
                "The pii message should contain the exception type");
            Assert.IsTrue(
               piiMessage.Contains(typeof(NotImplementedException).Name),
               "The pii message should have the inner exception type");
            Assert.IsTrue(piiMessage.Contains(exCode));
            Assert.IsFalse(piiMessage.Contains(exMessage));
            Assert.IsFalse(piiMessage.Contains(innerExMsg));
        }

        [TestMethod]
        public void MsalServiceException_FromHttpResponse()
        {
            // Arrange
            string reponseBody = "body";
            var statusCode = HttpStatusCode.BadRequest;
            var retryAfterSpan = new TimeSpan(3600); 

            HttpResponseMessage httpResponse = new HttpResponseMessage(statusCode);
            httpResponse.Content = new StringContent(reponseBody);
            httpResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfterSpan);
            HttpResponse coreResponse = HttpManager.CreateResponseAsync(httpResponse).Result;

            // Act
            Exception msalException =
                msalExceptionService.GetServiceException(
                    exCode,
                    exMessage,
                    coreResponse);

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(exCode, msalServiceException.ErrorCode);
            Assert.AreEqual(reponseBody, msalServiceException.ResponseBody);
            Assert.AreEqual(exMessage, msalServiceException.Message);
            Assert.AreEqual((int)statusCode, msalServiceException.StatusCode);

            Assert.AreEqual(retryAfterSpan, msalServiceException.Headers.RetryAfter.Delta);
        }
    }
}
