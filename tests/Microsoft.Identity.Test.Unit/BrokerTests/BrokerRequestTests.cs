// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Internal.Requests.Silent;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    [TestCategory(TestCategories.Broker)]
    public class BrokerTests : TestBase
    {
        private BrokerInteractiveRequestComponent _brokerInteractiveRequest;
        private BrokerSilentStrategy _brokerSilentAuthStrategy;
        private AuthenticationRequestParameters _parameters;
        private AcquireTokenSilentParameters _acquireTokenSilentParameters;
        private HttpResponse _brokerHttpResponse;

        [TestMethod]
        public void BrokerResponseTest()
        {
            // Arrange
            using (CreateBrokerHelper())
            {
                var response = new MsalTokenResponse
                {
                    IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                    AccessToken = "access-token",
                    ClientInfo = MockHelpers.CreateClientInfo(),
                    ExpiresIn = 3599,
                    CorrelationId = "correlation-id",
                    RefreshToken = "refresh-token",
                    Scope = TestConstants.s_scope.AsSingleString(),
                    TokenType = "Bearer"
                };

                // Act
                _brokerInteractiveRequest.ValidateResponseFromBroker(response);

                // Assert
                Assert.IsNotNull(response);
                Assert.AreEqual("access-token", response.AccessToken);
                Assert.AreEqual(MockHelpers.CreateClientInfo(), response.ClientInfo);
            }
        }

        [TestMethod]
        public void BrokerErrorResponseTest()
        {
            using (CreateBrokerHelper())
            {
                var response = new MsalTokenResponse
                {
                    Error = "MSALErrorDomain",
                    ErrorDescription = "error_description: Server returned less scopes than requested"
                };

                ValidateBrokerResponse(
                    response,
                    exception =>
                    {
                        var exc = exception as MsalServiceException;
                        Assert.IsNotNull(exc);
                        Assert.AreEqual(response.Error, exc.ErrorCode);
                        Assert.AreEqual(MsalErrorMessage.BrokerResponseError + response.ErrorDescription, exc.Message);
                    });

            }
        }

        [TestMethod]
        public void BrokerInteractionRequiredErrorResponseTest()
        {
            using (CreateBrokerHelper())
            {

                var response = new MsalTokenResponse
                {
                    Error = MsalError.InteractionRequired,
                    ErrorDescription = MsalError.InteractionRequired,
                    HttpResponse = _brokerHttpResponse
                };

                ValidateBrokerResponse(
                    response,
                    exception =>
                    {
                        var exc = exception as MsalUiRequiredException;
                        Assert.IsNotNull(exc);
                        Assert.AreEqual(MsalError.InteractionRequired, exc.ErrorCode);
                        Assert.AreEqual(MsalErrorMessage.BrokerResponseError + MsalError.InteractionRequired, exc.Message);
                        Assert.AreEqual(exc.StatusCode, (int)HttpStatusCode.Unauthorized);
                        Assert.AreEqual(exc.ResponseBody, "SomeBody");
                        Assert.IsNotNull(exc.Headers);
                    });
            }
        }

        [TestMethod]
        public void BrokerInvalidGrantErrorResponseTest()
        {
            using (CreateBrokerHelper())
            {

                var response = new MsalTokenResponse
                {
                    Error = MsalError.InvalidGrantError,
                    ErrorDescription = MsalError.InvalidGrantError,
                    HttpResponse = _brokerHttpResponse
                };

                ValidateBrokerResponse(
                    response,
                    exception =>
                    {
                        var exc = exception as MsalUiRequiredException;
                        Assert.IsNotNull(exc);
                        Assert.AreEqual(MsalError.InvalidGrantError, exc.ErrorCode);
                        Assert.AreEqual(MsalErrorMessage.BrokerResponseError + MsalError.InvalidGrantError, exc.Message);
                        Assert.AreEqual(exc.StatusCode, (int)HttpStatusCode.Unauthorized);
                        Assert.AreEqual(exc.ResponseBody, "SomeBody");
                        Assert.IsNotNull(exc.Headers);
                    });
            }
        }

        [TestMethod]
        public void BrokerUnknownErrorResponseTest()
        {
            using (CreateBrokerHelper())
            {

                var response = new MsalTokenResponse
                {
                    Error = null,
                    ErrorDescription = null
                };

                ValidateBrokerResponse(
                    response,
                    exception =>
                    {
                        var exc = exception as MsalServiceException;
                        Assert.IsNotNull(exc);
                        Assert.AreEqual(MsalError.BrokerResponseReturnedError, exc.ErrorCode);
                        Assert.AreEqual(MsalErrorMessage.BrokerResponseReturnedError, exc.Message);
                    });
            }
        }

        [TestMethod]
        public void BrokerInteractiveRequestTest()
        {
            string CanonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(TestConstants.AuthorityTestTenant));

            using (var harness = CreateTestHarness())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    null,
                    TestConstants.ExtraQueryParameters);

                // Act
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(harness.ServiceBundle.Config, null);
                _brokerInteractiveRequest =
                    new BrokerInteractiveRequestComponent(
                        parameters,
                        null,
                        broker,
                        "install_url");
#if NET6_WIN 
                Assert.AreEqual(true, _brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Aad));
#else
                Assert.AreEqual(false, _brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Aad));
#endif
            }
        }

        [TestMethod]
        public void BrokerSilentRequestTest()
        {
            string CanonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(TestConstants.AuthorityTestTenant));

            using (var harness = CreateBrokerHelper())
            {
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(harness.ServiceBundle.Config, null);
                _brokerSilentAuthStrategy =
                    new BrokerSilentStrategy(
                        new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters),
                        harness.ServiceBundle,
                        _parameters,
                        _acquireTokenSilentParameters,
                        broker);
                Assert.IsFalse(_brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Adfs));
                Assert.IsFalse(_brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.B2C));
                Assert.IsFalse(_brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Generic));
                Assert.IsFalse(_brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Dsts));

#if NET6_WIN || NET7_0
                Assert.IsTrue(_brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Aad));
#else
                Assert.AreEqual(false, _brokerInteractiveRequest.Broker.IsBrokerInstalledAndInvokable(AuthorityType.Aad));
#endif
            }
        }

        [TestMethod]
        public async Task NullBrokerUsernamePasswordRequestTest_Async()
        {
            using (var harness = CreateTestHarness())
            {
                var builder = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager);

                builder.Config.BrokerCreatorFunc = (_, _, logger) => { return new NullBroker(logger); };

                var app = builder.WithBroker(true).BuildConcrete();

                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                harness.HttpManager.AddWsTrustMockHandler();
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                // Act
                var result = await app.AcquireTokenByUsernamePassword(new[] { "User.Read" }, "username", "password")
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public void BrokerGetAccountsAsyncOnUnsupportedPlatformTest()
        {
            using (var harness = CreateTestHarness())
            {
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(harness.ServiceBundle.Config, null);

                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(
                    () => broker.GetAccountsAsync(
                        TestConstants.ClientId,
                        TestConstants.RedirectUri,
                        null,
                        null,
                        null))
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void BrokerRemoveAccountAsyncOnUnsupportedPlatformTest()
        {
            using (var harness = CreateTestHarness())
            {
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(harness.ServiceBundle.Config, null);

                AssertException.TaskThrowsAsync<PlatformNotSupportedException>(() => broker.RemoveAccountAsync(
                    harness.ServiceBundle.Config, new Account("test", "test", "test"))).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void GetAccountWithDuplicateBrokerAccountsTest()
        {
            // Arrange
            var app = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .BuildConcrete();

            var broker = Substitute.For<IBroker>();
            var expectedAccount = TestConstants.s_user;
            broker.GetAccountsAsync(
                TestConstants.ClientId,
                TestConstants.RedirectUri,
                Arg.Any<AuthorityInfo>(),
                Arg.Any<ICacheSessionManager>(),
                Arg.Any<IInstanceDiscoveryManager>()).Returns(new[] { expectedAccount, expectedAccount });
            broker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);

            var platformProxy = Substitute.For<IPlatformProxy>();
            platformProxy.CanBrokerSupportSilentAuth().Returns(true);
            platformProxy.CreateBroker(Arg.Any<ApplicationConfiguration>(), Arg.Any<CoreUIParent>()).ReturnsForAnyArgs(broker);

            app.ServiceBundle.SetPlatformProxyForTest(platformProxy);
            app.ServiceBundle.Config.IsBrokerEnabled = true;

            // Act
            var account = app.GetAccountAsync(TestConstants.HomeAccountId).GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual(TestConstants.HomeAccountId, account.HomeAccountId.Identifier);
        }

        [TestMethod]
        public void CreateFromiOSBroker_HandlesAllErrorFields()
        {
            // Arrange
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
            responseDictionary[TestConstants.iOSBrokerErrorMetadata] = TestConstants.iOSBrokerErrorMetadataValue;
            responseDictionary[BrokerResponseConst.BrokerErrorCode] = TestConstants.TestErrCode;
            responseDictionary[OAuth2ResponseBaseClaim.SubError] = TestConstants.iOSBrokerSuberrCode;
            responseDictionary[BrokerResponseConst.BrokerErrorDescription] = TestConstants.iOSBrokerErrDescr;

            // act
            var token = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

            // assert
            Assert.AreEqual(TestConstants.TestErrCode, token.Error);
            Assert.AreEqual(TestConstants.iOSBrokerSuberrCode, token.SubError);
            Assert.AreEqual(TestConstants.iOSBrokerErrDescr, token.ErrorDescription);
            Assert.AreEqual("test_home", token.AccountUserId);
            Assert.AreEqual(TestConstants.Username, token.Upn);
        }

        [TestMethod]
        public void CreateFromiOSBroker_HandlesNoSuberror()
        {
            // Arrange
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
            responseDictionary[TestConstants.iOSBrokerErrorMetadata] = TestConstants.iOSBrokerErrorMetadataValue;
            responseDictionary[BrokerResponseConst.BrokerErrorCode] = TestConstants.TestErrCode;
            responseDictionary[BrokerResponseConst.BrokerErrorDescription] = TestConstants.iOSBrokerErrDescr;

            // act
            var token = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

            // assert
            Assert.AreEqual(TestConstants.TestErrCode, token.Error);
            Assert.AreEqual(string.Empty, token.SubError);
            Assert.AreEqual(TestConstants.iOSBrokerErrDescr, token.ErrorDescription);
            Assert.AreEqual("test_home", token.AccountUserId);
            Assert.AreEqual(TestConstants.Username, token.Upn);
        }

        [TestMethod]
        public void CreateFromiOSBroker_HandlesNoErrorDescription()
        {
            // Arrange
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
            responseDictionary[TestConstants.iOSBrokerErrorMetadata] = TestConstants.iOSBrokerErrorMetadataValue;
            responseDictionary[BrokerResponseConst.BrokerErrorCode] = TestConstants.TestErrCode;
            responseDictionary[OAuth2ResponseBaseClaim.SubError] = TestConstants.iOSBrokerSuberrCode;

            // act
            var token = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

            // assert
            Assert.AreEqual(TestConstants.TestErrCode, token.Error);
            Assert.AreEqual(TestConstants.iOSBrokerSuberrCode, token.SubError);
            Assert.AreEqual(string.Empty, token.ErrorDescription);
            Assert.AreEqual("test_home", token.AccountUserId);
            Assert.AreEqual(TestConstants.Username, token.Upn);
        }

        [TestMethod]
        public void CreateFromiOSBroker_HandlesNoErrorMetadata()
        {
            // Arrange
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
            responseDictionary[BrokerResponseConst.BrokerErrorCode] = TestConstants.TestErrCode;
            responseDictionary[OAuth2ResponseBaseClaim.SubError] = TestConstants.iOSBrokerSuberrCode;
            responseDictionary[BrokerResponseConst.BrokerErrorDescription] = TestConstants.iOSBrokerErrDescr;

            // act
            var token = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

            // assert
            Assert.AreEqual(TestConstants.TestErrCode, token.Error);
            Assert.AreEqual(TestConstants.iOSBrokerSuberrCode, token.SubError);
            Assert.AreEqual(TestConstants.iOSBrokerErrDescr, token.ErrorDescription);
            Assert.AreEqual(null, token.AccountUserId);
            Assert.AreEqual(null, token.TenantId);
            Assert.AreEqual(null, token.Upn);
        }

        [TestMethod]
        public void CreateFromiOSBroker_HandlesNoAccountId()
        {
            // Arrange
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
            responseDictionary[TestConstants.iOSBrokerErrorMetadata] = @"{""username"" : """ + TestConstants.Username + @""" }";
            responseDictionary[BrokerResponseConst.BrokerErrorCode] = TestConstants.TestErrCode;
            responseDictionary[BrokerResponseConst.BrokerErrorDescription] = TestConstants.iOSBrokerErrDescr;

            // act
            var token = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

            // assert
            Assert.AreEqual(TestConstants.TestErrCode, token.Error);
            Assert.AreEqual(string.Empty, token.SubError);
            Assert.AreEqual(TestConstants.iOSBrokerErrDescr, token.ErrorDescription);
            Assert.AreEqual(null, token.AccountUserId);
            Assert.AreEqual(TestConstants.Username, token.Upn);
        }

        [TestMethod]
        public void CreateFromiOSBroker_HandlesNoUpn()
        {
            // Arrange
            Dictionary<string, string> responseDictionary = new Dictionary<string, string>();
            responseDictionary["error_metadata"] = @"{""home_account_id"":""test_home"" }";
            responseDictionary[BrokerResponseConst.BrokerErrorCode] = TestConstants.TestErrCode;
            responseDictionary[BrokerResponseConst.BrokerErrorDescription] = TestConstants.iOSBrokerErrDescr;

            // act
            var token = MsalTokenResponse.CreateFromiOSBrokerResponse(responseDictionary);

            // assert
            Assert.AreEqual(TestConstants.TestErrCode, token.Error);
            Assert.AreEqual(string.Empty, token.SubError);
            Assert.AreEqual(TestConstants.iOSBrokerErrDescr, token.ErrorDescription);
            Assert.AreEqual("test_home", token.AccountUserId);
            Assert.AreEqual(null, token.Upn);
        }

        [DataTestMethod]
        [DataRow(typeof(NullBroker))]
        [DataRow(typeof(IosBrokerMock))]
        [TestCategory(TestCategories.Regression)] //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2706
        public async Task NullAndIosBroker_GetAccounts_Async(Type brokerType)
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                var broker = CreateBroker(brokerType);
                var builder = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager);

                builder.Config.BrokerCreatorFunc = (_, _, logger) => { return new NullBroker(logger); };

                var app = builder.WithBroker(true).BuildConcrete();

                // Act
                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);

                // Assert
                Assert.IsFalse(accounts.Any());
            }
        }

        [DataTestMethod]
        [DataRow(typeof(NullBroker))]
        [DataRow(typeof(IosBrokerMock))]
        [TestCategory(TestCategories.Regression)] //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2706
        public async Task NullAndIosBroker_RemoveAccounts_Async(Type brokerType)
        {
            using (var harness = CreateTestHarness())
            {
                var broker = CreateBroker(brokerType);

                var builder = PublicClientApplicationBuilder
                     .Create(TestConstants.ClientId)
                     .WithHttpManager(harness.HttpManager);

                builder.Config.BrokerCreatorFunc = (_, _, _) => { return broker; };

                var app = builder.WithBroker(true).BuildConcrete();

                TokenCacheHelper.PopulateCache(app.UserTokenCacheInternal.Accessor);

                // Act
                var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                await app.RemoveAsync(accounts.Single()).ConfigureAwait(false);
                var accounts2 = await app.GetAccountsAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(1, accounts.Count());
                Assert.IsFalse(accounts2.Any());
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)] //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2706
        public async Task NullBroker_AcquireSilentInteractive_Async()
        {
            using (var harness = CreateTestHarness())
            {
                var builder = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager);

                builder.Config.BrokerCreatorFunc = (_, _, logger) => { return new NullBroker(logger); };

                var app = builder.WithBroker(true).BuildConcrete();

                // Act
                try
                {
                    await app.AcquireTokenSilent(new[] { "User.Read" }, new Account("id", "upn", "env"))
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }
                catch (MsalUiRequiredException e)
                {
                    Assert.AreEqual("no_tokens_found", e.ErrorCode);

                    harness.HttpManager.AddInstanceDiscoveryMockHandler();
                    harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost();

                    app.ServiceBundle.ConfigureMockWebUI();
                    var result = await
                        app.AcquireTokenInteractive(new[] { "User.Read" }).ExecuteAsync().ConfigureAwait(false);

                    Assert.IsNotNull(result, "Broker is not installed, so MSAL will get a token using the browser");
                }
            }
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)] //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2706
        public async Task iosBroker_AcquireSilentInteractive_Async()
        {
            using (var harness = CreateTestHarness())
            {
                var platformProxy = Substitute.For<IPlatformProxy>();
                platformProxy.CanBrokerSupportSilentAuth().Returns(false);
                platformProxy.CreateTokenCacheAccessor(Arg.Any<CacheOptions>(), true)
                    .Returns(new InMemoryPartitionedAppTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null));
                platformProxy.CreateTokenCacheAccessor(Arg.Any<CacheOptions>(), false)
                    .Returns(new InMemoryPartitionedUserTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null));

                harness.ServiceBundle.SetPlatformProxyForTest(platformProxy);

                var builder = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager);

                builder.Config.BrokerCreatorFunc = (_, _, logger) => { return new IosBrokerMock(logger); };
                builder.Config.PlatformProxy = platformProxy;

                var app = builder.WithBroker(true).BuildConcrete();

                // Act
                try
                {
                    await app.AcquireTokenSilent(new[] { "User.Read" }, new Account("id", "upn", "env"))
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }
                catch (MsalUiRequiredException e)
                {
                    Assert.AreEqual("no_tokens_found", e.ErrorCode);
                }
            }
        }

#if NET6_WIN
        [TestMethod]
        public async Task BrokerGetAccountsWithBrokerInstalledTestAsync()
        {
            // Arrange

            var platformProxy = Substitute.For<IPlatformProxy>();
            platformProxy.CanBrokerSupportSilentAuth().Returns(true);
            platformProxy.CreateTokenCacheAccessor(Arg.Any<CacheOptions>(), true)
                .Returns(new InMemoryPartitionedAppTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null));
            platformProxy.CreateTokenCacheAccessor(Arg.Any<CacheOptions>(), false)
                .Returns(new InMemoryPartitionedUserTokenCacheAccessor(Substitute.For<ILoggerAdapter>(), null));

            var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithExperimentalFeatures(true)
                .WithBroker(true)
                .WithPlatformProxy(platformProxy)
                .Build();

            var mockBroker = Substitute.For<IBroker>();
            var expectedAccount = new Account("a.b", "user", "login.windows.net");
            mockBroker.GetAccountsAsync(
                TestConstants.ClientId,
                TestConstants.RedirectUri,
                (pca.AppConfig as ApplicationConfiguration).Authority.AuthorityInfo,
                Arg.Any<ICacheSessionManager>(),
                Arg.Any<IInstanceDiscoveryManager>())
                .Returns(new[] { expectedAccount });
            mockBroker.IsBrokerInstalledAndInvokable((pca.AppConfig as ApplicationConfiguration).Authority.AuthorityInfo.AuthorityType).Returns(true);

            platformProxy.CreateBroker(null, null).ReturnsForAnyArgs(mockBroker);

            // Act
            var actualAccount = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Assert that MSAL acquires an account from the broker cache
            Assert.AreSame(expectedAccount, actualAccount.Single());
        }       
#endif

        [TestMethod]
        public async Task SilentAuthStrategyFallbackTestAsync()
        {
            using (var harness = CreateBrokerHelper())
            {
                //SilentRequest should always get an exception from the local client strategy and use the broker strategy instead when the right error codes
                //are thrown.

                // Arrange
                var mockBroker = Substitute.For<IBroker>();
                var expectedAccount = Substitute.For<IAccount>();
                mockBroker.GetAccountsAsync(
                    TestConstants.ClientId,
                    TestConstants.RedirectUri,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    Arg.Any<ICacheSessionManager>(),
                    Arg.Any<IInstanceDiscoveryManager>()).Returns(new[] { expectedAccount });
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(false);

                var platformProxy = Substitute.For<IPlatformProxy>();
                platformProxy.CanBrokerSupportSilentAuth().Returns(true);
                platformProxy.CreateBroker(null, null).ReturnsForAnyArgs(mockBroker);

                harness.ServiceBundle.SetPlatformProxyForTest(platformProxy);

                var mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                var mockBrokerStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                var brokerAuthenticationResult = new AuthenticationResult();

                var invalidGrantException = new MsalClientException(MsalError.InvalidGrantError);
                var noAccountException = new MsalClientException(MsalError.NoAccountForLoginHint);
                var noTokensException = new MsalClientException(MsalError.NoTokensFoundError);

                mockBrokerStrategy.ExecuteAsync(default).Returns(brokerAuthenticationResult);
                mockClientStrategy.ExecuteAsync(default).Throws(invalidGrantException);
                _acquireTokenSilentParameters.Account = new Account("a.b", "user", "lmo");

                //Execute silent request with invalid grant
                var silentRequest = new SilentRequest(
                    harness.ServiceBundle,
                    _parameters,
                    _acquireTokenSilentParameters,
                    mockClientStrategy,
                    mockBrokerStrategy);

                var result = await silentRequest.ExecuteTestAsync(default).ConfigureAwait(false);
                Assert.AreEqual(result, brokerAuthenticationResult);

                //Execute silent request with no accounts exception
                mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                mockClientStrategy.ExecuteAsync(new CancellationToken()).Throws(noAccountException);
                silentRequest = new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters, mockClientStrategy, mockBrokerStrategy);
                result = silentRequest.ExecuteTestAsync(new CancellationToken()).Result;
                Assert.AreEqual(result, brokerAuthenticationResult);

                //Execute silent request with no tokens exception
                mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                mockClientStrategy.ExecuteAsync(new CancellationToken()).Throws(noTokensException);
                silentRequest = new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters, mockClientStrategy, mockBrokerStrategy);
                result = silentRequest.ExecuteTestAsync(new CancellationToken()).Result;
                Assert.AreEqual(result, brokerAuthenticationResult);
            }
        }

        [TestMethod]
        public void SpecialAccount_CallsBrokerSilentAuth()
        {
            using (var harness = CreateBrokerHelper())
            {
                // Arrange
                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(false);

                var platformProxy = Substitute.For<IPlatformProxy>();
                platformProxy.CanBrokerSupportSilentAuth().Returns(true);
                platformProxy.CreateBroker(null, null).ReturnsForAnyArgs(mockBroker);

                harness.ServiceBundle.SetPlatformProxyForTest(platformProxy);

                var mockClientStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                var mockBrokerStrategy = Substitute.For<ISilentAuthRequestStrategy>();
                var ar = new AuthenticationResult();

                mockClientStrategy.ExecuteAsync(default).ThrowsForAnyArgs(
                    new MsalUiRequiredException(MsalError.CurrentBrokerAccount, "msg"));
                mockBrokerStrategy.ExecuteAsync(default).Returns(ar);
                _acquireTokenSilentParameters.Account = PublicClientApplication.OperatingSystemAccount;
                var silentRequest = new SilentRequest(
                    harness.ServiceBundle,
                    _parameters,
                    _acquireTokenSilentParameters,
                    mockClientStrategy,
                    mockBrokerStrategy);

                // Act
                var result = silentRequest.ExecuteTestAsync(new CancellationToken()).Result;

                // Assert
                Assert.AreEqual(ar, result);
            }
        }

        [TestMethod]
        public async Task BrokerSilentStrategy_DefaultAccountAsync()
        {
            using (var harness = CreateBrokerHelper())
            {
                var tokenResponse = CreateTokenResponseForTest();
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                IBroker broker = Substitute.For<IBroker>();
                _acquireTokenSilentParameters.Account = PublicClientApplication.OperatingSystemAccount;
                var brokerSilentAuthStrategy =
                    new BrokerSilentStrategy(
                        new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters),
                        harness.ServiceBundle,
                        _parameters,
                        _acquireTokenSilentParameters,
                        broker);
                _parameters.Account = PublicClientApplication.OperatingSystemAccount;
                broker.AcquireTokenSilentDefaultUserAsync(_parameters, _acquireTokenSilentParameters)
                    .Returns(Task.FromResult(tokenResponse));
                broker.IsBrokerInstalledAndInvokable(_parameters.Authority.AuthorityInfo.AuthorityType).Returns(true);

                // Act
                var result = await brokerSilentAuthStrategy.ExecuteAsync(default).ConfigureAwait(false);

                // Assert
                Assert.AreSame(tokenResponse.AccessToken, result.AccessToken);
            }
        }

        [TestMethod]
        public void NoTokenFoundThrowsUIRequiredTest()
        {
            using (var harness = CreateBrokerHelper())
            {
                try
                {
                    _brokerSilentAuthStrategy.ValidateResponseFromBroker(CreateErrorResponse(BrokerResponseConst.AndroidNoTokenFound));
                }
                catch (MsalUiRequiredException ex)
                {
                    Assert.IsTrue(ex.ErrorCode == BrokerResponseConst.AndroidNoTokenFound);
                    return;
                }

                Assert.Fail("Wrong Exception thrown. ");
            }
        }

        [TestMethod]
        public void NoAccountFoundThrowsUIRequiredTest()
        {
            using (var harness = CreateBrokerHelper())
            {
                try
                {
                    _brokerSilentAuthStrategy.ValidateResponseFromBroker(CreateErrorResponse(BrokerResponseConst.AndroidNoAccountFound));
                }
                catch (MsalUiRequiredException ex)
                {
                    Assert.IsTrue(ex.ErrorCode == BrokerResponseConst.AndroidNoAccountFound);
                    return;
                }

                Assert.Fail("Wrong Exception thrown. ");
            }
        }

        [TestMethod]
        public void InvalidRefreshTokenUsedThrowsUIRequiredTest()
        {
            using (var harness = CreateBrokerHelper())
            {
                try
                {
                    _brokerSilentAuthStrategy.ValidateResponseFromBroker(CreateErrorResponse(BrokerResponseConst.AndroidInvalidRefreshToken));
                }
                catch (MsalUiRequiredException ex)
                {
                    Assert.IsTrue(ex.ErrorCode == BrokerResponseConst.AndroidInvalidRefreshToken);
                    return;
                }

                Assert.Fail("Wrong Exception thrown. ");
            }
        }

        [TestMethod]
        public void InteractiveStrategy_ProtectionPolicyNotEnabled_Throws_Exception()
        {
            ProtectionPolicyNotEnabled_Throws_Exception_Common((msalToken) =>
            {
                _brokerInteractiveRequest.ValidateResponseFromBroker(msalToken);
            });
        }

        [TestMethod]
        public void SilentStrategy_ProtectionPolicyNotEnabled_Throws_Exception()
        {
            ProtectionPolicyNotEnabled_Throws_Exception_Common((msalToken) =>
            {
                _brokerSilentAuthStrategy.ValidateResponseFromBroker(msalToken);
            });
        }

        [TestMethod]
        public async Task MultiCloud_WithBroker_Async()
        {
            using (var harness = CreateBrokerHelper())
            {
                // Arrange
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var builder = PublicClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithAuthority("https://login.microsoftonline.com/common")
                   .WithMultiCloudSupport(true)
                   .WithHttpManager(harness.HttpManager);

                var broker = Substitute.For<IBroker>();
                builder.Config.BrokerCreatorFunc = (_, _, _) => broker;

                var globalPca = builder.WithBroker(true).BuildConcrete();

                // Setup the broker to return AuthorityUrl in the MsalTokenResponse as different cloud
                broker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);

                var tokenResponse = CreateTokenResponseForTest();
                tokenResponse.AuthorityUrl = "https://login.microsoftonline.us/organizations";
                broker.AcquireTokenInteractiveAsync(null, null).ReturnsForAnyArgs(Task.FromResult(tokenResponse));

                // Act - interactive flow logs-in Arlighton user
                var result = await globalPca.AcquireTokenInteractive(TestConstants.s_graphScopes).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual("login.microsoftonline.us", result.Account.Environment);
                Assert.AreEqual(TestConstants.Utid, result.TenantId);

                var account = (await globalPca.GetAccountsAsync().ConfigureAwait(false)).Single();
                Assert.AreEqual("login.microsoftonline.us", account.Environment);
                Assert.AreEqual(TestConstants.Utid, result.TenantId);

                await Assert.ThrowsExceptionAsync<MsalUiRequiredException>(
                    async () => await globalPca.AcquireTokenSilent(TestConstants.s_graphScopes, PublicClientApplication.OperatingSystemAccount).ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        private void ProtectionPolicyNotEnabled_Throws_Exception_Common(Action<MsalTokenResponse> action)
        {
            using (var harness = CreateBrokerHelper())
            {
                try
                {
                    // Arrange
                    MsalTokenResponse msalTokenResponse = CreateErrorResponse(BrokerResponseConst.AndroidUnauthorizedClient);
                    msalTokenResponse.SubError = BrokerResponseConst.AndroidProtectionPolicyRequired;
                    msalTokenResponse.TenantId = TestConstants.TenantId;
                    msalTokenResponse.Upn = TestConstants.Username;
                    msalTokenResponse.AccountUserId = TestConstants.LocalAccountId;
                    msalTokenResponse.AuthorityUrl = TestConstants.AuthorityUtid2Tenant;

                    // Act
                    action(msalTokenResponse);
                }
                catch (MsalServiceException ex) // Since IntuneAppProtectionPolicyRequiredException is throw only on Android and iOS platforms, this is the workaround
                {
                    // Assert
                    Assert.AreEqual(BrokerResponseConst.AndroidUnauthorizedClient, ex.ErrorCode);
                    Assert.AreEqual(BrokerResponseConst.AndroidProtectionPolicyRequired, ex.SubError);

                    return;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Wrong Exception thrown {ex.Message}.");
                }
            }
        }

        private static MsalTokenResponse CreateErrorResponse(string errorCode)
        {
            return new MsalTokenResponse
            {
                Scope = TestConstants.s_scope.AsSingleString(),
                TokenType = TestConstants.Bearer,
                Error = errorCode
            };
        }

        private void ValidateBrokerResponse(MsalTokenResponse msalTokenResponse, Action<Exception> validationHandler)
        {
            try
            {
                //Testing interactive response
                _brokerInteractiveRequest.ValidateResponseFromBroker(msalTokenResponse);

                Assert.Fail("MsalServiceException should have been thrown here");
            }
            catch (MsalServiceException exc)
            {
                try
                {
                    //Testing silent response
                    _brokerSilentAuthStrategy.ValidateResponseFromBroker(msalTokenResponse);

                    Assert.Fail("MsalServiceException should have been thrown here");
                }
                catch (MsalServiceException exc2)
                {
                    validationHandler(exc2);
                }
                validationHandler(exc);
            }

        }

        private MockHttpAndServiceBundle CreateBrokerHelper()
        {
            MockHttpAndServiceBundle harness = CreateTestHarness();

            _parameters = harness.CreateAuthenticationRequestParameters(
                TestConstants.AuthorityHomeTenant,
                TestConstants.s_scope,
                new TokenCache(harness.ServiceBundle, false),
                extraQueryParameters: TestConstants.ExtraQueryParameters,
                claims: TestConstants.Claims);

            _parameters.AppConfig.IsBrokerEnabled = true;

            AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters();
            _acquireTokenSilentParameters = new AcquireTokenSilentParameters();

            IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(harness.ServiceBundle.Config, null);
            _brokerInteractiveRequest =
                new BrokerInteractiveRequestComponent(
                    _parameters,
                    interactiveParameters,
                    broker,
                    "install_url");

            _brokerSilentAuthStrategy =
                new BrokerSilentStrategy(
                    new SilentRequest(harness.ServiceBundle, _parameters, _acquireTokenSilentParameters),
                    harness.ServiceBundle,
                    _parameters,
                    _acquireTokenSilentParameters,
                    broker);

            _brokerHttpResponse = new HttpResponse();
            _brokerHttpResponse.Body = "SomeBody";
            _brokerHttpResponse.StatusCode = HttpStatusCode.Unauthorized;
            _brokerHttpResponse.Headers = new HttpResponseMessage().Headers;

            return harness;
        }

        private MsalTokenResponse CreateTokenResponseForTest()

        {
            return new MsalTokenResponse()
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = null, // brokers don't return RT
                Scope = TestConstants.s_scope.AsSingleString(),
                TokenType = "Bearer",
                WamAccountId = "wam_account_id",
            };
        }

       
        internal static IBroker CreateBroker(Type brokerType)
        {
            if (brokerType == typeof(NullBroker))
            {
                return new NullBroker(null);
            }

            if (brokerType == typeof(IosBrokerMock))
            {
                return new IosBrokerMock(null);
            }

            throw new NotImplementedException();
        }
    }

}
