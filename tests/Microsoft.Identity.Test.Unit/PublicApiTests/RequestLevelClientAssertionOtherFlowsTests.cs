using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class RequestLevelClientAssertionOtherFlowsTests : TestBase
    {
        [TestMethod]
        public async Task RequestLevelClientAssertion_AuthorizationCode_OverridesAppLevel()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientAssertion((AssertionRequestOptions options) => Task.FromResult("app_level_assertion"))  // App-level assertion
                    .BuildConcrete();

                // Act - override with request-level assertion
                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "auth_code")
                    .WithClientAssertion("request_level_assertion")  // Request-level override
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                // Verify that the request used the request-level assertion
                Assert.AreEqual("request_level_assertion", handler.ActualRequestPostData[OAuth2Parameter.ClientAssertion]);
            }
        }

        [TestMethod]
        public async Task RequestLevelClientAssertion_OnBehalfOf_OverridesAppLevel()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientAssertion((AssertionRequestOptions options) => Task.FromResult("app_level_assertion"))  // App-level assertion
                    .BuildConcrete();

                var userAssertion = new UserAssertion("user_assertion_jwt");

                // Act - override with request-level assertion
                var result = await app.AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithClientAssertion("request_level_assertion")  // Request-level override
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(result);
                // Verify that the request used the request-level assertion
                Assert.AreEqual("request_level_assertion", handler.ActualRequestPostData[OAuth2Parameter.ClientAssertion]);
            }
        }
    }
}