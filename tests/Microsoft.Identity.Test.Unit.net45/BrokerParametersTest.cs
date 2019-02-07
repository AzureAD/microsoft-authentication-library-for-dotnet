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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class BrokerParametersTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

        public static readonly string CanonicalizedAuthority = Client.AppConfig.AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(MsalTestConstants.AuthorityTestTenant));

        [TestMethod]
        [Description("Test setting of the broker parameters in the BrokerInteractiveRequest constructor.")]
        public void BrokerInteractiveRequest_CreateBrokerParametersTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityTestTenant,
                    null,
                    null,
                    null,
                    MsalTestConstants.ExtraQueryParams);

                // Act
                var brokerParameters = parameters.CreateRequestParametersForBroker();

                // Assert
                Assert.AreEqual(10, brokerParameters.Count);

                Assert.AreEqual(CanonicalizedAuthority, brokerParameters[BrokerParameter.Authority]);
                Assert.AreEqual(MsalTestConstants.ScopeStr, brokerParameters[BrokerParameter.RequestScopes]);
                Assert.AreEqual(MsalTestConstants.ClientId, brokerParameters[BrokerParameter.ClientId]);

                Assert.AreEqual(harness.ServiceBundle.DefaultLogger.CorrelationId.ToString(), brokerParameters[BrokerParameter.CorrelationId]);
                Assert.AreEqual(MsalIdHelper.GetMsalVersion(), brokerParameters[BrokerParameter.ClientVersion]);
                Assert.AreEqual("NO", brokerParameters[BrokerParameter.Force]);
                Assert.AreEqual(string.Empty, brokerParameters[BrokerParameter.Username]);

                Assert.AreEqual(MsalTestConstants.RedirectUri, brokerParameters[BrokerParameter.RedirectUri]);

                Assert.AreEqual(MsalTestConstants.BrokerExtraQueryParameters, brokerParameters[BrokerParameter.ExtraQp]);

                //Assert.AreEqual(MsalTestConstants.BrokerClaims, brokerParameters.BrokerPayload[BrokerParameter.Claims]); //TODO
                Assert.AreEqual(BrokerParameter.OidcScopesValue, brokerParameters[BrokerParameter.ExtraOidcScopes]);
            }
        }
    }
}