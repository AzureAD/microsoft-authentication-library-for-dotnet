// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class BrokerParametersTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        public static readonly string CanonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(MsalTestConstants.AuthorityTestTenant));

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
                BrokerInteractiveRequest brokerInteractiveRequest = new BrokerInteractiveRequest(parameters, null, null, null);
                brokerInteractiveRequest.CreateRequestParametersForBroker();

                // Assert
                Assert.AreEqual(10, brokerInteractiveRequest._brokerPayload.Count);

                Assert.AreEqual(CanonicalizedAuthority, brokerInteractiveRequest._brokerPayload[BrokerParameter.Authority]);
                Assert.AreEqual(MsalTestConstants.ScopeStr, brokerInteractiveRequest._brokerPayload[BrokerParameter.RequestScopes]);
                Assert.AreEqual(MsalTestConstants.ClientId, brokerInteractiveRequest._brokerPayload[BrokerParameter.ClientId]);

                Assert.AreEqual(harness.ServiceBundle.DefaultLogger.CorrelationId.ToString(), brokerInteractiveRequest._brokerPayload[BrokerParameter.CorrelationId]);
                Assert.AreEqual(MsalIdHelper.GetMsalVersion(), brokerInteractiveRequest._brokerPayload[BrokerParameter.ClientVersion]);
                Assert.AreEqual("NO", brokerInteractiveRequest._brokerPayload[BrokerParameter.Force]);
                Assert.AreEqual(string.Empty, brokerInteractiveRequest._brokerPayload[BrokerParameter.Username]);

                Assert.AreEqual(MsalTestConstants.RedirectUri, brokerInteractiveRequest._brokerPayload[BrokerParameter.RedirectUri]);

                Assert.AreEqual(MsalTestConstants.BrokerExtraQueryParameters, brokerInteractiveRequest._brokerPayload[BrokerParameter.ExtraQp]);

                //Assert.AreEqual(MsalTestConstants.BrokerClaims, brokerInteractiveRequest._brokerPayload[BrokerParameter.Claims]); //TODO
                Assert.AreEqual(BrokerParameter.OidcScopesValue, brokerInteractiveRequest._brokerPayload[BrokerParameter.ExtraOidcScopes]);
            }
        }
    }
}
