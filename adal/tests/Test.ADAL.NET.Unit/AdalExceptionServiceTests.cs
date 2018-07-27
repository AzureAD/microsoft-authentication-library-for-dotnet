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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AdalExceptionServiceTests
    {
        private const string exCode = "exCode";
        private const string exMessage = "exMessage";

        AdalEceptionFactory adalExceptionFactory;

        [TestInitialize]
        public void Init()
        {
            adalExceptionFactory = new AdalEceptionFactory();
        }

        [TestMethod]
        public void ParamValidation()
        {
            AssertException.Throws<ArgumentNullException>(
                () => adalExceptionFactory.GetClientException(null, exMessage));

            AssertException.Throws<ArgumentNullException>(
                () => adalExceptionFactory.GetClientException("", exMessage));

            AssertException.Throws<ArgumentNullException>(
                () => adalExceptionFactory.GetServiceException(exCode, ""));

            AssertException.Throws<ArgumentNullException>(
                () => adalExceptionFactory.GetServiceException(exCode, null));
        }

        [TestMethod]
        public void AdalExceptionAdapter_FromCoreException()
        {
            // Act
            Exception exception = adalExceptionFactory
                 .GetClientException(exCode, exMessage);

            // Assert
            var adalClientException = exception as AdalException;
            Assert.AreEqual(exCode, adalClientException.ErrorCode);
            Assert.AreEqual(exMessage, adalClientException.Message);
            Assert.IsNull(adalClientException.InnerException);
        }

        [TestMethod]
        public void AdalServiceException_FromCoreException()
        {
            // Arrange
            int statusCode = 511;
            string reponseBody = "responseBody";
            string exMessage = "innerExceptionMsg";

            NotImplementedException innerException = new NotImplementedException(exMessage);

            // Act
            Exception adalEx = adalExceptionFactory.GetServiceException(
                exCode,
                exMessage,
                innerException,
                new ExceptionDetail()
                {
                    StatusCode = statusCode,
                    ResponseBody = reponseBody
                });

            // Assert
            var adalServiceEx = adalEx as AdalServiceException;
            Assert.AreEqual(innerException, adalServiceEx.InnerException);
            Assert.AreEqual(exCode, adalServiceEx.ErrorCode);
            Assert.IsNull(adalServiceEx.ServiceErrorCodes);
            Assert.IsNull(adalServiceEx.Headers);
            Assert.AreEqual(exMessage, adalServiceEx.Message);

            // Act
            var piiMessage = adalExceptionFactory.GetPiiScrubbedDetails(adalEx);

            // Assert
            Assert.IsFalse(String.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
             piiMessage.Contains(typeof(NotImplementedException).Name),
             "The pii message should contain the exception type");
            Assert.IsTrue(
               piiMessage.Contains(typeof(AdalServiceException).Name),
               "The pii message should have the core the exception type");
            Assert.IsTrue(piiMessage.Contains(exCode));
            Assert.IsFalse(piiMessage.Contains(exMessage));
        }

        [TestMethod]
        public void AdalServiceExceptionn_WithClaims_FromCoreException()
        {
            // Arrange
            string claims = "claims";

            // Act
            Exception adalException = adalExceptionFactory.GetServiceException(
                exCode,
                exMessage,
                null,
                new ExceptionDetail()
                {
                    Claims = claims
                });

            // Assert
            var adalClaimsException = adalException as AdalClaimChallengeException;
            Assert.AreEqual(exCode, adalClaimsException.ErrorCode);
            Assert.AreEqual(claims, adalClaimsException.Claims);
            Assert.IsNull(adalClaimsException.InnerException);
            Assert.IsNull(adalClaimsException.ServiceErrorCodes);
            Assert.IsNull(adalClaimsException.Headers);
            Assert.AreEqual(0, adalClaimsException.StatusCode);
            Assert.AreEqual(exMessage, adalClaimsException.Message);
        }
    }
}
