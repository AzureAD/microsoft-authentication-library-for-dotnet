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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Features.DeviceCode;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class DeviceCodeRequestTests
    {
        private const string ExpectedDeviceCode =
            "BAQABAAEAAADXzZ3ifr-GRbDT45zNSEFEfU4P-bZYS1vkvv8xiXdb1_zX2xAcdcfEoei1o-t9-zTB9sWyTcddFEWahP1FJJJ_YVA1zvPM2sV56d_8O5G23ti5uu0nRbIsniczabYYEr-2ZsbgRO62oZjKlB1zF3EkuORg2QhMOjtsk-KP0aw8_iAA";

        private const string ExpectedUserCode = "B6SUYU5PL";
        private const int ExpectedExpiresIn = 900;
        private const int ExpectedInterval = 1;
        private const string ExpectedVerificationUrl = "https://microsoft.com/devicelogin";
        private TokenCache _cache;
        private IValidatedAuthoritiesCache _validatedAuthoritiesCache;

        private string ExpectedMessage =>
            $"To sign in, use a web browser to open the page {ExpectedVerificationUrl} and enter the code {ExpectedUserCode} to authenticate.";

        private string ExpectedResponseMessage =>
            $"{{" +
            $"\"user_code\":\"{ExpectedUserCode}\"," +
            $"\"device_code\":\"{ExpectedDeviceCode}\"," +
            $"\"verification_url\":\"{ExpectedVerificationUrl}\"," +
            $"\"expires_in\":\"{ExpectedExpiresIn}\"," +
            $"\"interval\":\"{ExpectedInterval}\"," +
            $"\"message\":\"{ExpectedMessage}\"," +
            $"}}";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
            _validatedAuthoritiesCache = new ValidatedAuthoritiesCache();
            _cache = new TokenCache();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cache.TokenCacheAccessor.ClearAccessTokens();
            _cache.TokenCacheAccessor.ClearRefreshTokens();
        }

        private HttpResponseMessage CreateDeviceCodeResponseSuccessMessage()
        {
            return MockHelpers.CreateSuccessResponseMessage(ExpectedResponseMessage);
        }

        [TestMethod]
        [TestCategory("DeviceCodeRequestTests")]
        public void TestDeviceCodeAuthSuccess()
        {
            const int NumberOfAuthorizationPendingRequestsToInject = 1;

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                var parameters = CreateAuthenticationParametersAndSetupMocks(
                    httpManager,
                    NumberOfAuthorizationPendingRequestsToInject,
                    out HashSet<string> expectedScopes);

                _cache.ServiceBundle = serviceBundle;
                
                // Check that cache is empty
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(0, _cache.TokenCacheAccessor.AccountCount);
                Assert.AreEqual(0, _cache.TokenCacheAccessor.IdTokenCount);
                Assert.AreEqual(0, _cache.TokenCacheAccessor.RefreshTokenCount);

                DeviceCodeResult actualDeviceCodeResult = null;
                var request = new DeviceCodeRequest(
                    serviceBundle,
                    parameters,
                    ApiEvent.ApiIds.None,
                    result =>
                    {
                        actualDeviceCodeResult = result;
                        return Task.FromResult(0);
                    });
                Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                task.Wait();
                var authenticationResult = task.Result;
                Assert.IsNotNull(authenticationResult);
                Assert.IsNotNull(actualDeviceCodeResult);

                Assert.AreEqual(MsalTestConstants.ClientId, actualDeviceCodeResult.ClientId);
                Assert.AreEqual(ExpectedDeviceCode, actualDeviceCodeResult.DeviceCode);
                Assert.AreEqual(ExpectedInterval, actualDeviceCodeResult.Interval);
                Assert.AreEqual(ExpectedMessage, actualDeviceCodeResult.Message);
                Assert.AreEqual(ExpectedUserCode, actualDeviceCodeResult.UserCode);
                Assert.AreEqual(ExpectedVerificationUrl, actualDeviceCodeResult.VerificationUrl);

                CoreAssert.AreScopesEqual(expectedScopes.AsSingleString(), actualDeviceCodeResult.Scopes.AsSingleString());

                // Validate that entries were added to cache
                Assert.AreEqual(1, _cache.TokenCacheAccessor.AccessTokenCount);
                Assert.AreEqual(1, _cache.TokenCacheAccessor.AccountCount);
                Assert.AreEqual(1, _cache.TokenCacheAccessor.IdTokenCount);
                Assert.AreEqual(1, _cache.TokenCacheAccessor.RefreshTokenCount);
            }
        }

        [TestMethod]
        [TestCategory("DeviceCodeRequestTests")]
        public void TestDeviceCodeCancel()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                const int NumberOfAuthorizationPendingRequestsToInject = 0;
                var parameters = CreateAuthenticationParametersAndSetupMocks(
                    httpManager,
                    NumberOfAuthorizationPendingRequestsToInject,
                    out HashSet<string> expectedScopes);

                _cache.ServiceBundle = serviceBundle;

                var cancellationSource = new CancellationTokenSource();

                DeviceCodeResult actualDeviceCodeResult = null;
                var request = new DeviceCodeRequest(
                    serviceBundle,
                    parameters,
                    ApiEvent.ApiIds.None,
                    async result =>
                    {
                        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
                        actualDeviceCodeResult = result;
                    });

                // We setup the cancel before calling the RunAsync operation since we don't check the cancel
                // until later and the mock network calls run insanely fast for us to timeout for them.
                cancellationSource.Cancel();
                AssertException.TaskThrows<OperationCanceledException>(() => request.RunAsync(cancellationSource.Token));
            }
        }


        [TestMethod]
        [TestCategory("DeviceCodeRequestTests")]
        public void VerifyAuthorizationPendingErrorDoesNotLogError()
        {
            // When calling DeviceCodeFlow, we poll for the authorization and if the user hasn't entered the code in yet
            // then we receive an error for authorization_pending.  This is thrown as an exception and logged as
            // errors.  This error is noisy and so it should be suppressed for this one case.
            // This test verifies that the error for authorization_pending is not logged as an error.

            var logCallbacks = new List<_LogData>();

            Logger.LogCallback = (level, message, pii) =>
            {
                if (level == LogLevel.Error)
                {
                    Assert.Fail(
                        "Received an error message {0} and the stack trace is {1}",
                        message,
                        new StackTrace(true));
                }

                logCallbacks.Add(
                    new _LogData
                    {
                        Level = level,
                        Message = message,
                        IsPii = pii
                    });
            };

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                try
                {
                    const int NumberOfAuthorizationPendingRequestsToInject = 2;
                    var parameters = CreateAuthenticationParametersAndSetupMocks(
                        httpManager,
                        NumberOfAuthorizationPendingRequestsToInject,
                        out HashSet<string> expectedScopes);

                    var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                    _cache.ServiceBundle = serviceBundle;

                    var request = new DeviceCodeRequest(
                        serviceBundle,
                        parameters,
                        ApiEvent.ApiIds.None,
                        result => Task.FromResult(0));

                    Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                    task.Wait();

                    // Ensure we got logs so the log callback is working.
                    Assert.IsTrue(logCallbacks.Count > 0, "There should be data in logCallbacks");

                    // Ensure we have authorization_pending data in the logs
                    List<_LogData> authPendingLogs =
                        logCallbacks.Where(x => x.Message.Contains(OAuth2Error.AuthorizationPending)).ToList();
                    Assert.AreEqual(2, authPendingLogs.Count, "authorization_pending logs should exist");

                    // Ensure the authorization_pending logs are Info level and not Error
                    Assert.AreEqual(
                        2,
                        authPendingLogs.Where(x => x.Level == LogLevel.Info).ToList().Count,
                        "authorization_pending logs should be INFO");

                    // Ensure we don't have Error level logs in this scenario.
                    string errorLogs = string.Join(
                        "--",
                        logCallbacks
                            .Where(x => x.Level == LogLevel.Error)
                            .Select(x => x.Message)
                            .ToArray());



                    Assert.IsFalse(
                        logCallbacks.Any(x => x.Level == LogLevel.Error),
                        "Error level logs should not exist but got: " + errorLogs);
                }
                finally
                {
                    Logger.LogCallback = null;
                }
            }
        }

        private AuthenticationRequestParameters CreateAuthenticationParametersAndSetupMocks(
            MockHttpManager httpManager,
            int numAuthorizationPendingResults,
            out HashSet<string> expectedScopes)
        {
            var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

            var authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityHomeTenant, false);
            _cache = new TokenCache()
            {
                ClientId = MsalTestConstants.ClientId,
                ServiceBundle = serviceBundle
            };

            var parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientId = MsalTestConstants.ClientId,
                Scope = MsalTestConstants.Scope,
                TokenCache = _cache,
                RequestContext = new RequestContext(null, new MsalLogger(Guid.NewGuid(), null))
            };

            TestCommon.MockInstanceDiscoveryAndOpenIdRequest(httpManager);

            expectedScopes = new HashSet<string>();
            expectedScopes.UnionWith(MsalTestConstants.Scope);
            expectedScopes.Add(OAuth2Value.ScopeOfflineAccess);
            expectedScopes.Add(OAuth2Value.ScopeProfile);
            expectedScopes.Add(OAuth2Value.ScopeOpenId);

            // Mock Handler for device code request
            httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    Method = HttpMethod.Post,
                    PostData = new Dictionary<string, string>()
                    {
                        {OAuth2Parameter.ClientId, MsalTestConstants.ClientId},
                        {OAuth2Parameter.Scope, expectedScopes.AsSingleString()}
                    },
                    ResponseMessage = CreateDeviceCodeResponseSuccessMessage()
                });

            for (int i = 0; i < numAuthorizationPendingResults; i++)
            {
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        Url = "https://login.microsoftonline.com/home/oauth2/v2.0/token",
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                            HttpStatusCode.Forbidden,
                            "{\"error\":\"authorization_pending\"," +
                            "\"error_description\":\"AADSTS70016: Pending end-user authorization." +
                            "\\r\\nTrace ID: f6c2c73f-a21d-474e-a71f-d8b121a58205\\r\\nCorrelation ID: " +
                            "36fe3e82-442f-4418-b9f4-9f4b9295831d\\r\\nTimestamp: 2015-09-24 19:51:51Z\"," +
                            "\"error_codes\":[70016],\"timestamp\":\"2015-09-24 19:51:51Z\",\"trace_id\":" +
                            "\"f6c2c73f-a21d-474e-a71f-d8b121a58205\",\"correlation_id\":" +
                            "\"36fe3e82-442f-4418-b9f4-9f4b9295831d\"}")
                    });
            }

            if (numAuthorizationPendingResults > 0)
            {
                // Mock Handler for devicecode->token exchange request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        PostData = new Dictionary<string, string>()
                        {
                            {OAuth2Parameter.ClientId, MsalTestConstants.ClientId},
                            {OAuth2Parameter.Scope, expectedScopes.AsSingleString()}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                    });
            }

            return parameters;
        }

        private class _LogData
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
            public bool IsPii { get; set; }
        }
    }
}