// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class DeviceCodeRequestTests : TestBase
    {
        private const string ExpectedDeviceCode =
            "BAQABAAEAAADXzZ3ifr-GRbDT45zNSEFEfU4P-bZYS1vkvv8xiXdb1_zX2xAcdcfEoei1o-t9-zTB9sWyTcddFEWahP1FJJJ_YVA1zvPM2sV56d_8O5G23ti5uu0nRbIsniczabYYEr-2ZsbgRO62oZjKlB1zF3EkuORg2QhMOjtsk-KP0aw8_iAA";

        private const string ExpectedUserCode = "B6SUYU5PL";
        private const int ExpectedExpiresIn = 900;
        private const int ExpectedInterval = 1;
        private const string ExpectedVerificationUrl = "https://microsoft.com/devicelogin";
        private const string ExpectedAdfsVerificationUrl = "https://fs.contoso.com/adfs/oauth2/deviceauth";

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

        private string ExpectedAdfsResponseMessage =>
            $"{{" +
            $"\"user_code\":\"{ExpectedUserCode}\"," +
            $"\"device_code\":\"{ExpectedDeviceCode}\"," +
            $"\"verification_url\":\"{ExpectedAdfsVerificationUrl}\"," +
            $"\"expires_in\":\"{ExpectedExpiresIn}\"," +
            $"\"interval\":\"{ExpectedInterval}\"," +
            $"\"message\":\"{ExpectedMessage}\"," +
            $"}}";

        private HttpResponseMessage CreateDeviceCodeResponseSuccessMessage()
        {
            return MockHelpers.CreateSuccessResponseMessage(ExpectedResponseMessage);
        }

        private HttpResponseMessage CreateAdfsDeviceCodeResponseSuccessMessage()
        {
            return MockHelpers.CreateSuccessResponseMessage(ExpectedAdfsResponseMessage);
        }

        [TestMethod]
        public async Task TestDeviceCodeAuthSuccessAsync()
        {
            const int NumberOfAuthorizationPendingRequestsToInject = 1;

            using (var harness = CreateTestHarness())
            {
                var parameters = CreateAuthenticationParametersAndSetupMocks(
                    harness,
                    NumberOfAuthorizationPendingRequestsToInject,
                    out HashSet<string> expectedScopes);

                var cache = parameters.CacheSessionManager.TokenCacheInternal;

                // Check that cache is empty
                Assert.AreEqual(0, cache.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, cache.Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(0, cache.Accessor.GetAllIdTokens().Count());
                Assert.AreEqual(0, cache.Accessor.GetAllAccounts().Count());

                DeviceCodeResult actualDeviceCodeResult = null;

                var deviceCodeParameters = new AcquireTokenWithDeviceCodeParameters
                {
                    DeviceCodeResultCallback = result =>
                    {
                        actualDeviceCodeResult = result;
                        return Task.FromResult(0);
                    }
                };

                var request = new DeviceCodeRequest(harness.ServiceBundle, parameters, deviceCodeParameters);
                var authenticationResult = await request.RunAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(authenticationResult);
                Assert.IsNotNull(actualDeviceCodeResult);

                Assert.AreEqual(TestConstants.ClientId, actualDeviceCodeResult.ClientId);
                Assert.AreEqual(ExpectedDeviceCode, actualDeviceCodeResult.DeviceCode);
                Assert.AreEqual(ExpectedInterval, actualDeviceCodeResult.Interval);
                Assert.AreEqual(ExpectedMessage, actualDeviceCodeResult.Message);
                Assert.AreEqual(ExpectedUserCode, actualDeviceCodeResult.UserCode);
                Assert.AreEqual(ExpectedVerificationUrl, actualDeviceCodeResult.VerificationUrl);

                CoreAssert.AreScopesEqual(expectedScopes.AsSingleString(), actualDeviceCodeResult.Scopes.AsSingleString());

                // Validate that entries were added to cache
                Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllIdTokens().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllAccounts().Count());
            }
        }

        [TestMethod]
        [WorkItem(1407)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1407
        public async Task DeviceCodeExceptionsOn200OKAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var handler = new MockHttpMessageHandler()
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateInvalidClientResponseMessage()
                };

                harness.HttpManager.AddMockHandler(handler);

                var parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    account: null);

                DeviceCodeResult actualDeviceCodeResult = null;

                var deviceCodeParameters = new AcquireTokenWithDeviceCodeParameters
                {
                    DeviceCodeResultCallback = result =>
                    {
                        actualDeviceCodeResult = result;
                        return Task.FromResult(0);
                    }
                };

                var request = new DeviceCodeRequest(harness.ServiceBundle, parameters, deviceCodeParameters);

                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => request.RunAsync(CancellationToken.None)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void TestDeviceCodeAuthSuccessWithAdfs()
        {
            const int NumberOfAuthorizationPendingRequestsToInject = 1;

            using (var harness = new MockHttpAndServiceBundle(authority: TestConstants.OnPremiseAuthority))
            {
                var parameters = CreateAuthenticationParametersAndSetupMocks(
                    harness,
                    NumberOfAuthorizationPendingRequestsToInject,
                    out HashSet<string> expectedScopes,
                    true);

                var cache = parameters.CacheSessionManager.TokenCacheInternal;

                // Check that cache is empty
                Assert.AreEqual(0, cache.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, cache.Accessor.GetAllAccounts().Count());
                Assert.AreEqual(0, cache.Accessor.GetAllIdTokens().Count());
                Assert.AreEqual(0, cache.Accessor.GetAllRefreshTokens().Count());

                DeviceCodeResult actualDeviceCodeResult = null;

                var deviceCodeParameters = new AcquireTokenWithDeviceCodeParameters
                {
                    DeviceCodeResultCallback = result =>
                    {
                        actualDeviceCodeResult = result;
                        return Task.FromResult(0);
                    }
                };

                var request = new DeviceCodeRequest(harness.ServiceBundle, parameters, deviceCodeParameters);

                Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                task.Wait();
                var authenticationResult = task.Result;
                Assert.IsNotNull(authenticationResult);
                Assert.IsNotNull(actualDeviceCodeResult);
                Assert.AreEqual(TestConstants.ClientId, actualDeviceCodeResult.ClientId);
                Assert.AreEqual(ExpectedDeviceCode, actualDeviceCodeResult.DeviceCode);
                Assert.AreEqual(ExpectedInterval, actualDeviceCodeResult.Interval);
                Assert.AreEqual(ExpectedMessage, actualDeviceCodeResult.Message);
                Assert.AreEqual(ExpectedUserCode, actualDeviceCodeResult.UserCode);
                Assert.AreEqual(ExpectedAdfsVerificationUrl, actualDeviceCodeResult.VerificationUrl);
                CoreAssert.AreScopesEqual(expectedScopes.AsSingleString(), actualDeviceCodeResult.Scopes.AsSingleString());
                // Validate that entries were added to cache
                Assert.AreEqual(1, cache.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllAccounts().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllIdTokens().Count());
                Assert.AreEqual(1, cache.Accessor.GetAllRefreshTokens().Count());
            }
        }

        [TestMethod]
        public async Task TestDeviceCodeCancelAsync()
        {
            using (var harness = CreateTestHarness())
            {
                const int NumberOfAuthorizationPendingRequestsToInject = 0;
                var parameters = CreateAuthenticationParametersAndSetupMocks(
                    harness,
                    NumberOfAuthorizationPendingRequestsToInject,
                    out HashSet<string> _);

                var cancellationSource = new CancellationTokenSource();

                DeviceCodeResult actualDeviceCodeResult = null;
                var deviceCodeParameters = new AcquireTokenWithDeviceCodeParameters
                {
                    DeviceCodeResultCallback = async result =>
                    {
                        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
                        actualDeviceCodeResult = result;
                    }
                };

                var request = new DeviceCodeRequest(harness.ServiceBundle, parameters, deviceCodeParameters);

                // We setup the cancel before calling the RunAsync operation since we don't check the cancel
                // until later and the mock network calls run insanely fast for us to timeout for them.
                cancellationSource.Cancel();
                await AssertException.TaskThrowsAsync<OperationCanceledException>(() => request.RunAsync(cancellationSource.Token)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task VerifyAuthorizationPendingErrorDoesNotLogError_Async()
        {
            // When calling DeviceCodeFlow, we poll for the authorization and if the user hasn't entered the code in yet
            // then we receive an error for authorization_pending.  This is thrown as an exception and logged as
            // errors.  This error is noisy and so it should be suppressed for this one case.
            // This test verifies that the error for authorization_pending is not logged as an error.

            var logCallbacks = new List<_LogData>();

            using (var harness = CreateTestHarness(logCallback: (level, message, pii) =>
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
            }))
            {
                const int NumberOfAuthorizationPendingRequestsToInject = 2;
                var parameters = CreateAuthenticationParametersAndSetupMocks(
                    harness,
                    NumberOfAuthorizationPendingRequestsToInject,
                    out HashSet<string> _);

                var deviceCodeParameters = new AcquireTokenWithDeviceCodeParameters
                {
                    DeviceCodeResultCallback = _ => Task.FromResult(0)
                };

                var request = new DeviceCodeRequest(harness.ServiceBundle, parameters, deviceCodeParameters);

                await request.RunAsync(CancellationToken.None).ConfigureAwait(false);

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
        }

        private AuthenticationRequestParameters CreateAuthenticationParametersAndSetupMocks(
            MockHttpAndServiceBundle harness,
            int numAuthorizationPendingResults,
            out HashSet<string> expectedScopes,
            bool isAdfs = false)
        {
            var cache = new TokenCache(harness.ServiceBundle, false);
            var parameters = harness.CreateAuthenticationRequestParameters(
                isAdfs ? TestConstants.OnPremiseAuthority : TestConstants.AuthorityHomeTenant,
                null,
                cache,
                null,
                extraQueryParameters: TestConstants.ExtraQueryParameters,
                claims: TestConstants.Claims);

            if (!isAdfs)
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
            }

            expectedScopes = new HashSet<string>();
            expectedScopes.UnionWith(TestConstants.s_scope);
            expectedScopes.Add(OAuth2Value.ScopeOfflineAccess);
            expectedScopes.Add(OAuth2Value.ScopeProfile);
            expectedScopes.Add(OAuth2Value.ScopeOpenId);

            // Mock Handler for device code request
            harness.HttpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ExpectedPostData = new Dictionary<string, string>()
                    {
                        { OAuth2Parameter.ClientId, TestConstants.ClientId },
                        { OAuth2Parameter.Scope, expectedScopes.AsSingleString() },
                        { OAuth2Parameter.Claims, TestConstants.Claims }
                    },
                    ResponseMessage = isAdfs ? CreateAdfsDeviceCodeResponseSuccessMessage() : CreateDeviceCodeResponseSuccessMessage(),
                    ExpectedQueryParams = TestConstants.ExtraQueryParameters,                     
                });

            for (int i = 0; i < numAuthorizationPendingResults; i++)
            {
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ExpectedUrl = isAdfs ? "https://fs.contoso.com/adfs/oauth2/token" : "https://login.microsoftonline.com/home/oauth2/v2.0/token",
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
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ExpectedPostData = new Dictionary<string, string>()
                        {
                            {OAuth2Parameter.ClientId, TestConstants.ClientId},
                            {OAuth2Parameter.Scope, expectedScopes.AsSingleString()}
                        },
                        ResponseMessage = isAdfs ? MockHelpers.CreateAdfsSuccessTokenResponseMessage() : MockHelpers.CreateSuccessTokenResponseMessage()
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
