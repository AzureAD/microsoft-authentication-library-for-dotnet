using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    public class WamMsaPassthroughTests : TestBase
    {
        private MsaPassthroughHandler _msaPassthroughHandler;
        private ICoreLogger _logger;
        private IWamPlugin _msaPlugin;
        private IWamProxy _wamProxy;

        [TestInitialize]
        public void Init()
        {
            _logger = Substitute.For<ICoreLogger>();
            _msaPlugin = Substitute.For<IWamPlugin>();
            _wamProxy = Substitute.For<IWamProxy>();

            _msaPassthroughHandler = new MsaPassthroughHandler(
                _logger,
                _msaPlugin,
                _wamProxy,
                IntPtr.Zero);
        }

        [TestMethod]
        public void AddTransferTokenToRequest()
        {
            // Arrange
            const string TransferToken = "transfer_token";
            var provider = new WebAccountProvider("id", "user@contoso.com", null);
            WebTokenRequest request = new WebTokenRequest(provider, "scope", "client_id");

            // Act
            _msaPassthroughHandler.AddTransferTokenToRequest(request, TransferToken);

            // Assert
            Assert.AreEqual(TransferToken, request.Properties["SamlAssertion"]);
            Assert.AreEqual("SAMLV1", request.Properties["SamlAssertionType"]);
        }

        [TestMethod]
        public async Task FetchTransferTokenAsync()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var msaProvider = new WebAccountProvider("id", "user@contoso.com", null);

                Client.Internal.Requests.AuthenticationRequestParameters requestParams =
                    harness.CreateAuthenticationRequestParameters(
                        TestConstants.AuthorityHomeTenant,
                        validateAuthority: true);
                var msaRequest = new WebTokenRequest(msaProvider);

                // step 1 - msa request
                _msaPlugin.CreateWebTokenRequestAsync(msaProvider, requestParams, false, true, false)
                    .Returns(Task.FromResult(msaRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                WebAccount accountFromMsaProvider = new WebAccount(msaProvider, "user@outlook.com", WebAccountState.Connected);
                var webTokenResponse = new WebTokenResponse("v1_token", accountFromMsaProvider);
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });
                _wamProxy.RequestTokenForWindowAsync(IntPtr.Zero, msaRequest).Returns(webTokenResponseWrapper);

                // step 2 - we have v1 token and a WebAccount, now get a transfer token
                var transferTokenRequest = new WebTokenRequest(msaProvider);
                _msaPlugin
                    .CreateWebTokenRequestAsync(
                    msaProvider, 
                    TestConstants.ClientId, 
                    MsaPassthroughHandler.TransferTokenScopes)
                    .Returns(Task.FromResult(transferTokenRequest));
                var transferTokenRequestResult = Substitute.For<IWebTokenRequestResultWrapper>();
                transferTokenRequestResult.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                //WebAccount accountFromMsaProvider = new WebAccount(msaProvider, "user@outlook.com", WebAccountState.Connected);
                var transferTokenResponse = new WebTokenResponse("transfer_token");
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { transferTokenResponse });
                _msaPlugin.ParseSuccessfullWamResponse(Arg.Any<WebTokenResponse>(), out Arg.Any<Dictionary<string, string>>())
                    .Returns(x =>
                {
                    x[1] = new Dictionary<string, string>();
                    (x[1] as Dictionary<string, string>).Add("code", "actual_transfer_token");
                    return new MsalTokenResponse();
                });

                _wamProxy.RequestTokenForWindowAsync(IntPtr.Zero, transferTokenRequest).Returns(webTokenResponseWrapper);

                // Act
                var transferToken = await _msaPassthroughHandler.FetchTransferTokenAsync(requestParams, msaProvider)
                    .ConfigureAwait(false);

                // Assert
                Assert.AreEqual("actual_transfer_token", transferToken);
            }
        }
    }
}
