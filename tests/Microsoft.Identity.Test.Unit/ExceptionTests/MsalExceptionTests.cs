// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{

    [TestClass]
    [DeploymentItem(@"Resources\RSATestCertDotNet.pfx")]
    public class MsalExceptionTests : TestBase
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
            Assert.ThrowsExactly<ArgumentNullException>(() => new MsalClientException(null, ExMessage));
            Assert.ThrowsExactly<ArgumentNullException>(() => new MsalClientException(string.Empty, ExMessage));

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new MsalServiceException(ExCode, string.Empty));

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new MsalServiceException(ExCode, null));
        }

        [TestMethod]
        public void MsalClientException_FromMessageAndCode()
        {
            // Act
            var msalException = new MsalClientException(ExCode, ExMessage);

            // Assert
            var msalClientException = msalException;
            Assert.AreEqual(ExCode, msalClientException.ErrorCode);
            Assert.AreEqual(ExMessage, msalClientException.Message);
            Assert.IsNull(msalClientException.InnerException);

            // Act
            string piiMessage = LoggerHelper.GetPiiScrubbedExceptionDetails(msalException);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(piiMessage));
            Assert.Contains(
typeof(MsalClientException).Name,
                piiMessage, "The pii message should contain the exception type");
            Assert.Contains(ExCode, piiMessage);
            Assert.DoesNotContain(ExMessage, piiMessage);
        }

        [TestMethod]        
        public void IsRetryable()
        {
            MsalClientException msalClientException = new MsalClientException("code");
            Assert.IsFalse(msalClientException.IsRetryable);

            foreach (var code in new[] { 429, 408, 500, 501, 502, 503, 504, 505 })
            {
                HttpResponse httpResponse1 = new HttpResponse()
                {
                    Body = "body",
                    StatusCode = (HttpStatusCode)code
                };

                var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse1);

                Assert.IsTrue(msalException.IsRetryable);
            }

            foreach (var code in new[] { 200, 300, 400, 401 })
            {
                HttpResponse httpResponse2 = new HttpResponse()
                {
                    Body = "body",
                    StatusCode = (HttpStatusCode)code
                };

                var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse2);

                Assert.IsFalse(msalException.IsRetryable);
            }

            var ex = new MsalServiceException("request_timeout", "message");
            Assert.IsTrue(ex.IsRetryable);
            ex = new MsalServiceException("temporarily_unavailable", "message");
            Assert.IsTrue(ex.IsRetryable);

            ex = new MsalServiceException("other_error", "message");
            Assert.IsFalse(ex.IsRetryable);

        }

        [TestMethod]
        public void MsalServiceException_Classification_Only()
        {
            ValidateClassification(null, UiRequiredExceptionClassification.None);
            ValidateClassification(string.Empty, UiRequiredExceptionClassification.None);
            ValidateClassification("new_value", UiRequiredExceptionClassification.None);

            ValidateClassification(MsalError.BasicAction, UiRequiredExceptionClassification.BasicAction);
            ValidateClassification(MsalError.AdditionalAction, UiRequiredExceptionClassification.AdditionalAction);
            ValidateClassification(MsalError.MessageOnly, UiRequiredExceptionClassification.MessageOnly);
            ValidateClassification(MsalError.ConsentRequired, UiRequiredExceptionClassification.ConsentRequired);
            ValidateClassification(MsalError.UserPasswordExpired, UiRequiredExceptionClassification.UserPasswordExpired);

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
            Assert.Contains(ExMessage, msalException.Message);
            Assert.AreEqual("some_claims", msalException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalException.CorrelationId);
            Assert.AreEqual(suberror ?? "", msalException.SubError);

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

            Assert.Contains(msalProductName, exceptionString, "Exception should contain the msalProductName");
            Assert.Contains(msalVersion, exceptionString, "Exception should contain the msalVersion");
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
            var msalServiceException = msalException;
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(ExMessage + " " + MsalErrorMessage.ClaimsChallenge, msalServiceException.Message);
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
                Body = JsonError.Replace("invalid_grant", "invalid_client"),
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse);

            // Assert
            var msalServiceException = msalException;
            Assert.AreEqual(MsalError.InvalidClient, msalServiceException.ErrorCode);
            Assert.Contains(MsalErrorMessage.InvalidClient, msalServiceException.Message);
            ValidateExceptionProductInformation(msalException);
        }

        [TestMethod]
        public void MsalServiceException_Throws_MsalUIRequiredException_When_Throttled()
        {
            // Arrange
            HttpResponse httpResponse = new HttpResponse()
            {
                Body = JsonError.Replace("AADSTS90002", Constants.AadThrottledErrorCode),
                StatusCode = HttpStatusCode.BadRequest, // 400
            };

            // Act
            var msalException = MsalServiceExceptionFactory.FromHttpResponse(ExCode, ExMessage, httpResponse);

            // Assert
            Assert.AreEqual(typeof(MsalClaimsChallengeException), msalException.GetType());
            Assert.AreEqual(MsalErrorMessage.AadThrottledError + " " + MsalErrorMessage.ClaimsChallenge, msalException.Message);
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
            var msalServiceException = msalException;
            Assert.AreEqual(innerException, msalServiceException.InnerException);
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(JsonError, msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage + " " + MsalErrorMessage.ClaimsChallenge, msalServiceException.Message);
            Assert.AreEqual(statusCode, msalServiceException.StatusCode);

            Assert.AreEqual("some_claims", msalServiceException.Claims);
            Assert.AreEqual("6347d33d-941a-4c35-9912-a9cf54fb1b3e", msalServiceException.CorrelationId);
            Assert.AreEqual("some_suberror", msalServiceException.SubError);
            ValidateExceptionProductInformation(msalException);

            // Act
            string piiMessage = LoggerHelper.GetPiiScrubbedExceptionDetails(msalException);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(piiMessage));
            Assert.Contains(
typeof(MsalClaimsChallengeException).Name,
                piiMessage, "The pii message should contain the exception type");
            Assert.Contains(
typeof(NotImplementedException).Name,
                piiMessage, "The pii message should have the inner exception type");
            Assert.Contains(ExCode, piiMessage);
            Assert.Contains("6347d33d-941a-4c35-9912-a9cf54fb1b3e", piiMessage); // Correlation Id

            Assert.DoesNotContain(ExMessage, piiMessage);
            Assert.DoesNotContain(innerExMsg, piiMessage);
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
            var msalUiRequiredException = msalException;
            Assert.AreEqual(innerException, msalUiRequiredException.InnerException);
            Assert.AreEqual(ExCode, msalUiRequiredException.ErrorCode);
            Assert.IsNull(msalUiRequiredException.Claims);
            Assert.IsNull(msalUiRequiredException.ResponseBody);
            Assert.AreEqual(ExMessage, msalUiRequiredException.Message);
            Assert.AreEqual(0, msalUiRequiredException.StatusCode);
            Assert.AreEqual(UiRequiredExceptionClassification.None, msalUiRequiredException.Classification);
            ValidateExceptionProductInformation(msalException);

            // Act
            string piiMessage = LoggerHelper.GetPiiScrubbedExceptionDetails(msalException);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(piiMessage));
            Assert.Contains(
typeof(MsalUiRequiredException).Name,
                piiMessage, "The pii message should contain the exception type");
            Assert.Contains(
typeof(NotImplementedException).Name,
                piiMessage, "The pii message should have the inner exception type");
            Assert.Contains(ExCode, piiMessage);
            Assert.DoesNotContain(ExMessage, piiMessage);
            Assert.DoesNotContain(innerExMsg, piiMessage);
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
            var msalServiceException = msalException;
            Assert.AreEqual(ExCode, msalServiceException.ErrorCode);
            Assert.AreEqual(responseBody, msalServiceException.ResponseBody);
            Assert.AreEqual(ExMessage + " " + MsalErrorMessage.ClaimsChallenge, msalServiceException.Message);
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

            Assert.Contains("errCode", ex.ToString());
            Assert.Contains("errMessage", ex.ToString());
            Assert.Contains("innerMsg", ex.ToString());

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
            Assert.Contains("errCode", ex.ToString());
            Assert.Contains("errMessage", ex.ToString());
            Assert.Contains("innerExMsg", ex.ToString());
            Assert.Contains("invalid_tenant", ex.ToString());
            Assert.Contains("MySuberror", ex.ToString());
            Assert.Contains("some_claims", ex.ToString());
            Assert.Contains("AADSTS90002", ex.ToString());
            Assert.IsFalse(ex is MsalUiRequiredException);

            ValidateExceptionProductInformation(ex);
        }

        [TestMethod]
        public void ExceptionsPropertiesHavePublicSetters()
        {
            AssertPropertyHasPublicGetAndSet(typeof(MsalServiceException), "Headers");
            AssertPropertyHasPublicGetAndSet(typeof(MsalServiceException), "ResponseBody");
            AssertPropertyHasPublicGetAndSet(typeof(MsalServiceException), "CorrelationId");
        }

        [TestMethod]
        public async Task CorrelationIdInServiceExceptions()
        {
            var app = PublicClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithDefaultRedirectUri()
                        .Build();
            var ex = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(async () =>
                {
                    await app.AcquireTokenSilent(TestConstants.s_graphScopes, TestConstants.s_user).ExecuteAsync().ConfigureAwait(false);
                }
            ).ConfigureAwait(false);

            Assert.IsFalse(string.IsNullOrEmpty(ex.CorrelationId));
            Assert.IsFalse(string.IsNullOrEmpty(((MsalException)ex).CorrelationId));

            Guid guid = Guid.NewGuid();
            ex = await Assert.ThrowsExactlyAsync<MsalUiRequiredException>(async () =>
                {
                    await app.AcquireTokenSilent(TestConstants.s_graphScopes, TestConstants.s_user).WithCorrelationId(guid).ExecuteAsync().ConfigureAwait(false);
                }
            ).ConfigureAwait(false);

            Assert.AreEqual(guid.ToString(), ex.CorrelationId);
            Assert.AreEqual(guid.ToString(), ((MsalException)ex).CorrelationId);
        }

        [TestMethod]
        public async Task CorrelationIdInClientExceptions()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.AuthorityCommonTenant + TestConstants.DiscoveryEndPoint));

                ConfidentialClientApplication app = null;
                using (var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("RSATestCertDotNet.pfx")))
                {
                    app = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                        .WithRedirectUri(TestConstants.RedirectUri)
                        .WithHttpManager(harness.HttpManager)
                        .WithCertificate(certificate)
                        .BuildConcrete();
                }

                var ex = await Assert.ThrowsAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                             .ExecuteAsync(CancellationToken.None)
                             .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.IsFalse(string.IsNullOrEmpty(ex.CorrelationId));
                Assert.IsFalse(string.IsNullOrEmpty(((MsalException)ex).CorrelationId));

                Guid guid = Guid.NewGuid();
                ex = await Assert.ThrowsExactlyAsync<MsalClientException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                                                 .WithCorrelationId(guid)
                                                 .ExecuteAsync(CancellationToken.None)
                                                 .ConfigureAwait(false);
                }
                ).ConfigureAwait(false);

                Assert.AreEqual(guid.ToString(), ex.CorrelationId);
                Assert.AreEqual(guid.ToString(), ((MsalException)ex).CorrelationId);
            }
        }

        [TestMethod]
        public async Task AuthorityInNotFoundExceptions()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.AuthorityCommonTenant + TestConstants.DiscoveryEndPoint));
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Post);

                ConfidentialClientApplication app = null;
                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("RSATestCertDotNet.pfx"));

                app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var ex = await Assert.ThrowsAsync<MsalServiceException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                             .ExecuteAsync(CancellationToken.None)
                             .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.Contains("Authority used: https://login.microsoftonline.com/common/", ex.Message);
                Assert.Contains("Token Endpoint: https://login.microsoftonline.com/common/oauth2/v2.0/token", ex.Message);

                //Validate that the authority is not appended with extra query parameters
                //Validate that the region is also captured
                Environment.SetEnvironmentVariable("REGION_NAME", TestConstants.Region);

                harness.HttpManager.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.AuthorityCommonTenant + TestConstants.DiscoveryEndPoint));
                harness.HttpManager.AddMockHandlerContentNotFound(HttpMethod.Post);

                app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(TestConstants.AuthorityNotKnownTenanted + "extra=qp"), true)
                    .WithAzureRegion(TestConstants.Region)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                ex = await Assert.ThrowsExactlyAsync<MsalServiceException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                                                 .ExecuteAsync(CancellationToken.None)
                                                 .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.Contains("Authority used: https://sts.access.edu/my-utid/", ex.Message);
                Assert.Contains("Token Endpoint: https://centralus.sts.access.edu/my-utid/oauth2/v2.0/token", ex.Message);
                Assert.Contains($"Region Used: {TestConstants.Region}", ex.Message);

                //harness.HttpManager.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.AuthorityCommonTenant + TestConstants.DiscoveryEndPoint));
                harness.HttpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);
                harness.HttpManager.AddRequestTimeoutResponseMessageMockHandler(HttpMethod.Post);

                //Ensure non 404 error codes do not trigger message
                ex = await Assert.ThrowsExactlyAsync<MsalServiceException>(async () =>
                {
                    await app.AcquireTokenForClient(TestConstants.s_scope)
                                                 .WithForceRefresh(true)
                                                 .ExecuteAsync(CancellationToken.None)
                                                 .ConfigureAwait(false);
                }).ConfigureAwait(false);

                Assert.DoesNotContain("Authority used:", ex.Message);
                Assert.DoesNotContain("Token Endpoint:", ex.Message);
                Assert.DoesNotContain($"Region Used:", ex.Message);
            }
        }

        private void AssertPropertyHasPublicGetAndSet(Type t, string propertyName)
        {
            var prop = t.GetProperty(propertyName);

            var getProp = prop.GetGetMethod(false);
            var setProp = prop.GetSetMethod(false);

            Assert.IsNotNull(getProp, $"Getter is not public for {propertyName}");
            Assert.IsNotNull(setProp, $"Setter is not public for {propertyName}");
        }
    }
}

