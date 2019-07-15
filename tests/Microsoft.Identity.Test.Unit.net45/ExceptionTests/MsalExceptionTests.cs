// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class MsalExceptionTests
    {
        private const string ExCode = "exCode";
        private const string ExMessage = "exMessage";

        private const string JsonError = @"{ ""error"":""invalid_grant"", ""suberror"":""some_suberror"",
            ""claims"":""some_claims"",
            ""error_description"":""AADSTS90002: Tenant 'x' not found. "", ""error_codes"":[90002],""timestamp"":""2019-01-28 14:16:04Z"",
            ""trace_id"":""43f14373-8d7d-466e-a5f1-6e3889291e00"",
            ""correlation_id"":""6347d33d-941a-4c35-9912-a9cf54fb1b3e""}";


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
        public void MsalServiceException_Classification_Only()
        {
            ValidateClassification(null, UiRequiredExceptionClassification.None);
            ValidateClassification(string.Empty, UiRequiredExceptionClassification.None);
            ValidateClassification("new_value", UiRequiredExceptionClassification.None);

            ValidateClassification(MsalError.BasicAction,          UiRequiredExceptionClassification.BasicAction);
            ValidateClassification(MsalError.AdditionalAction,     UiRequiredExceptionClassification.AdditionalAction);
            ValidateClassification(MsalError.MessageOnly,          UiRequiredExceptionClassification.MessageOnly);
            ValidateClassification(MsalError.ConsentRequired,      UiRequiredExceptionClassification.ConsentRequired);
            ValidateClassification(MsalError.UserPasswordExpired,  UiRequiredExceptionClassification.UserPasswordExpired);
                                   
            ValidateClassification(MsalError.BadToken, UiRequiredExceptionClassification.None);
            ValidateClassification(MsalError.TokenExpired, UiRequiredExceptionClassification.None);
            ValidateClassification(MsalError.ProtectionPolicyRequired, UiRequiredExceptionClassification.None, false);
            ValidateClassification(MsalError.ClientMismatch, UiRequiredExceptionClassification.None, false);
            ValidateClassification(MsalError.DeviceAuthenticationFailed, UiRequiredExceptionClassification.None);
        }

        private static void ValidateClassification(
            string suberror, 
            UiRequiredExceptionClassification expectedClassification,
            bool expectUiRequiredException = true)
        {
            var newJsonError = JsonError.Replace("some_suberror", suberror);

            // Arrange
            HttpResponse httpResponse = new HttpResponse()
            {
                Body = newJsonError,
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse);

            Assert.AreEqual(ExCode, msalException.ErrorCode);
            Assert.AreEqual(ExMessage, msalException.Message);
            Assert.AreEqual("some_claims", msalException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalException.CorrelationId);
            Assert.AreEqual(suberror ?? "", msalException.SubError );


            if (expectUiRequiredException)
            {
                Assert.AreEqual(expectedClassification, (msalException as MsalUiRequiredException).Classification);
            }

            ValidateExceptionProductInformation(msalException);
        }

        private static void ValidateExceptionProductInformation(MsalException exception)
        {
            string exceptionString = exception.ToString();

            string msalProductName = PlatformProxyFactory.CreatePlatformProxy(null).GetProductName();
            string msalVersion = MsalIdHelper.GetMsalVersion();

            Assert.IsTrue(exceptionString.Contains(msalProductName), "Exception should contain the msalProductName");
            Assert.IsTrue(exceptionString.Contains(msalVersion), "Exception should contain the msalVersion");
        }

        [TestMethod]
        public void MsalUiRequiredException_Oauth2Response()
        {
            // Arrange
            HttpResponse httpResponse = new HttpResponse()
            {
                Body = JsonError,
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse);

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual("some_claims", msalServiceException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalServiceException.CorrelationId);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);

            ValidateExceptionProductInformation(msalException);
        }

        [TestMethod]
        public void InvalidClientException_IsRepackaged()
        {
            // Arrange
            HttpResponse httpResponse = new HttpResponse()
            {
                Body =  JsonError.Replace("invalid_grant", "invalid_client"),
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse);

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(MsalError.InvalidClient, msalServiceException.ErrorCode);
            Assert.IsTrue(msalServiceException.Message.Contains(MsalErrorMessage.InvalidClient));
            ValidateExceptionProductInformation(msalException);
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
                Body = JsonError,
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse, innerException);

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(innerException, msalServiceException.InnerException);
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(JsonError, msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual(statusCode, msalServiceException.StatusCode);

            Assert.AreEqual("some_claims", msalServiceException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalServiceException.CorrelationId);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);
            ValidateExceptionProductInformation(msalException);

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
            Assert.IsTrue(piiMessage.Contains("6347d33d-941a-4c35-9912-a9cf54fb1b3e")); // Correlation Id

            Assert.IsFalse(piiMessage.Contains(ExMessage));
            Assert.IsFalse(piiMessage.Contains(innerExMsg));
        }

        [TestMethod]
        public void MsalUiRequiredExceptionProperties()
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
            var msalUiRequiredException = msalException as MsalUiRequiredException;
            Assert.AreEqual(innerException, msalUiRequiredException.InnerException);
            Assert.AreEqual(ExCode, msalUiRequiredException.ErrorCode);
            Assert.IsNull(msalUiRequiredException.Claims);
            Assert.IsNull(msalUiRequiredException.ResponseBody);
            Assert.AreEqual(ExMessage, msalUiRequiredException.Message);
            Assert.AreEqual(0, msalUiRequiredException.StatusCode);
            Assert.AreEqual(UiRequiredExceptionClassification.None, msalUiRequiredException.Classification);
            ValidateExceptionProductInformation(msalException);

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
            string responseBody = JsonError;
            var statusCode = HttpStatusCode.BadRequest;
            var retryAfterSpan = new TimeSpan(3600);

            var httpResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            };

            httpResponse.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfterSpan);
            HttpResponse msalhttpResponse = HttpManager.CreateResponseAsync(httpResponse).Result;

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, msalhttpResponse);

            // Assert
            var msalServiceException = msalException as MsalServiceException;
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(responseBody, msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage, msalServiceException.Message);
            Assert.AreEqual((int)statusCode, msalServiceException.StatusCode);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);

            Assert.AreEqual(retryAfterSpan, msalServiceException.Headers.RetryAfter.Delta);
            ValidateExceptionProductInformation(msalException);
        }

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
            var ex = new MsalUiRequiredException("code", "message");

            Assert.IsNull(ex.InnerException);

            ValidateExceptionProductInformation(ex);
        }

        [TestMethod]
        public void Exception_ToString()
        {
            var innerException = new InvalidOperationException("innerMsg");
            MsalException ex = new MsalException("errCode", "errMessage", innerException);

            Assert.IsTrue(ex.ToString().Contains("errCode"));
            Assert.IsTrue(ex.ToString().Contains("errMessage"));
            Assert.IsTrue(ex.ToString().Contains("innerMsg"));

            ValidateExceptionProductInformation(ex);
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
            var ex = MsalServiceExceptionFactory.FromHttpResponse("errCode",
                "errMessage", httpResponse, innerException);

            // Assert
            Assert.IsTrue(ex.ToString().Contains("errCode"));
            Assert.IsTrue(ex.ToString().Contains("errMessage"));
            Assert.IsTrue(ex.ToString().Contains("innerExMsg"));
            Assert.IsTrue(ex.ToString().Contains("invalid_tenant"));
            Assert.IsTrue(ex.ToString().Contains("MySuberror"));
            Assert.IsTrue(ex.ToString().Contains("some_claims"));
            Assert.IsTrue(ex.ToString().Contains("AADSTS90002"));
            Assert.IsFalse(ex is MsalUiRequiredException);

            ValidateExceptionProductInformation(ex);
        }
    }
}
