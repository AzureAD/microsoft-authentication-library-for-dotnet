// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class MsalExceptionFactoryTests
    {
        private const string ExCode = "exCode";
        private const string ExMessage = "exMessage";

        private const string jsonError = @"{ ""error"":""invalid_tenant"", ""suberror"":""some_suberror"",
            ""claims"":""some_claims"",
            ""error_description"":""AADSTS90002: Tenant 'x' not found. "", ""error_codes"":[90002],""timestamp"":""2019-01-28 14:16:04Z"",
            ""trace_id"":""43f14373-8d7d-466e-a5f1-6e3889291e00"",
            ""correlation_id"":""6347d33d-941a-4c35-9912-a9cf54fb1b3e""}";

    [TestInitialize]
        public void Init()
        {
        }

        [TestMethod]
        public void ParamValidation()
        {
            AssertException.Throws<ArgumentNullException>(() => new MsalClientException(null, ExMessage));
            AssertException.Throws<ArgumentNullException>(() => new MsalClientException(string.Empty, ExMessage));

            AssertException.Throws<ArgumentNullException>(
                () => new MsalServiceException(ExCode, string.Empty));

            AssertException.Throws<ArgumentNullException>(
                () => new MsalServiceException(ExCode, null));
        }

        [TestMethod]
        public void MsalClientException_FromMessageAndCode()
        {
            // Act
            var msalException = new MsalClientException(ExCode, ExMessage);

            // Assert
            var msalClientException = msalException as MsalClientException;
            Assert.AreEqual(ExCode, msalClientException.ErrorCode);
            Assert.AreEqual(ExMessage, msalClientException.Message);
            Assert.IsNull(msalClientException.InnerException);

            // Act
            string piiMessage = MsalLogger.GetPiiScrubbedExceptionDetails(msalException);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
                piiMessage.Contains(typeof(MsalClientException).Name),
                "The pii message should contain the exception type");
            Assert.IsTrue(piiMessage.Contains(ExCode));
            Assert.IsFalse(piiMessage.Contains(ExMessage));
        }

        [TestMethod]
        public void MsalServiceException_Oauth2Response_Only()
        {
            // Arrange
            HttpResponse httpResponse = new HttpResponse()
            {
                Body = jsonError,
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            OAuth2ResponseBase oAuth2Response =
              JsonHelper.TryToDeserializeFromJson<OAuth2ResponseBase>(httpResponse?.Body);

            // Act
            var msalException = new MsalServiceException(ExCode, ExMessage)
            {
                OAuth2Response = oAuth2Response
            };

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual("some_claims", msalServiceException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalServiceException.CorrelationId);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);
        }

        [TestMethod]
        public void MsalServiceException_HttpResponse_OAuthResponse()
        {
            // Arrange
            int statusCode = 400;
            string innerExMsg = "innerExMsg";
            var innerException = new NotImplementedException(innerExMsg);

            HttpResponse httpResponse = new HttpResponse()
            {
                Body = jsonError,
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = new MsalServiceException(ExCode, ExMessage, innerException)
            {
                HttpResponse = httpResponse
            };

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(innerException, msalServiceException.InnerException);
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(jsonError, msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual(statusCode, msalServiceException.StatusCode);

            Assert.AreEqual("some_claims", msalServiceException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalServiceException.CorrelationId);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);

            // Act
            string piiMessage = MsalLogger.GetPiiScrubbedExceptionDetails(msalException);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
                piiMessage.Contains(typeof(MsalServiceException).Name),
                "The pii message should contain the exception type");
            Assert.IsTrue(
                piiMessage.Contains(typeof(NotImplementedException).Name),
                "The pii message should have the inner exception type");
            Assert.IsTrue(piiMessage.Contains(ExCode));
            Assert.IsTrue(piiMessage.Contains("6347d33d-941a-4c35-9912-a9cf54fb1b3e")); // Correlation Id

            Assert.IsFalse(piiMessage.Contains(ExMessage));
            Assert.IsFalse(piiMessage.Contains(innerExMsg));

        }

        [TestMethod]
        public void MsalUiRequiredException()
        {
            // Arrange
            string innerExMsg = "innerExMsg";
            var innerException = new NotImplementedException(innerExMsg);

            // Act
            var msalException = new MsalUiRequiredException(
                ExCode,
                ExMessage,
                innerException);

            // Assert
            var msalServiceException = msalException as MsalUiRequiredException;
            Assert.AreEqual(innerException, msalServiceException.InnerException);
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.IsNull(msalServiceException.Claims);
            Assert.IsNull(msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual(0, msalServiceException.StatusCode);

            // Act
            string piiMessage = MsalLogger.GetPiiScrubbedExceptionDetails(msalException);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(piiMessage));
            Assert.IsTrue(
                piiMessage.Contains(typeof(MsalUiRequiredException).Name),
                "The pii message should contain the exception type");
            Assert.IsTrue(
                piiMessage.Contains(typeof(NotImplementedException).Name),
                "The pii message should have the inner exception type");
            Assert.IsTrue(piiMessage.Contains(ExCode));
            Assert.IsFalse(piiMessage.Contains(ExMessage));
            Assert.IsFalse(piiMessage.Contains(innerExMsg));
        }

        [TestMethod]
        public void MsalServiceException_FromHttpResponse()
        {
            // Arrange
            string responseBody = jsonError;
            var statusCode = HttpStatusCode.BadRequest;
            var retryAfterSpan = new TimeSpan(3600);

            var httpResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            };

            httpResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfterSpan);
            HttpResponse msalhttpResponse = HttpManager.CreateResponseAsync(httpResponse).Result;

            // Act
            var msalException = new MsalServiceException(ExCode, ExMessage)
            {
                HttpResponse = msalhttpResponse
            };

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(responseBody, msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual((int)statusCode, msalServiceException.StatusCode);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);

            Assert.AreEqual(retryAfterSpan, msalServiceException.Headers.RetryAfter.Delta);
        }
    }
}
