using System;
using System.Collections.Generic;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class TokenClientTests : TestBase
    {
        [TestMethod]
        public async Task SendTokenRequest_MissingAccessTokenTypeInResponse_Throws_Async()
        {
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityCommonTenant);
                var tokenClient = new TokenClient(requestParams);

                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var fakeResponse = TestConstants.CreateMsalTokenResponse();
                fakeResponse.TokenType = null;
                harness.HttpManager.AddResponseMockHandlerForPost(MockHelpers.CreateSuccessResponseMessage(JsonHelper.SerializeToJson(fakeResponse)));
                await requestParams.AuthorityManager.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

                var ex = await Assert.ThrowsExceptionAsync<MsalClientException>(
                    () => tokenClient.SendTokenRequestAsync(new Dictionary<string, string>())).ConfigureAwait(false);
                Assert.AreEqual(MsalError.AccessTokenTypeMissing, ex.ErrorCode);
            }
        }
    }
}
