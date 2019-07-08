// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client;
using System;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class BrokerParametersTest : TestBase
    {
        public static readonly string s_canonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(MsalTestConstants.AuthorityTestTenant));

        [TestMethod]
        [Description("Test setting of the broker parameters in the BrokerInteractiveRequest constructor.")]
        public void BrokerInteractiveRequest_CreateBrokerParametersTest()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    MsalTestConstants.AuthorityTestTenant,
                    null,
                    null,
                    null,
                    MsalTestConstants.ExtraQueryParams);

                // Act
                BrokerFactory brokerFactory = new BrokerFactory();
                BrokerInteractiveRequest brokerInteractiveRequest = 
                    new BrokerInteractiveRequest(
                        parameters, 
                        null, 
                        harness.ServiceBundle, 
                        null, 
                        brokerFactory.Create(harness.ServiceBundle));

                brokerInteractiveRequest.CreateRequestParametersForBroker();

                // Assert
                Assert.AreEqual(10, brokerInteractiveRequest.BrokerPayload.Count);

                Assert.AreEqual(s_canonicalizedAuthority, brokerInteractiveRequest.BrokerPayload[BrokerParameter.Authority]);
                Assert.AreEqual(MsalTestConstants.ScopeStr, brokerInteractiveRequest.BrokerPayload[BrokerParameter.Scope]);
                Assert.AreEqual(MsalTestConstants.ClientId, brokerInteractiveRequest.BrokerPayload[BrokerParameter.ClientId]);

                Assert.IsFalse(string.IsNullOrEmpty(brokerInteractiveRequest.BrokerPayload[BrokerParameter.CorrelationId]));
                Assert.AreNotEqual(Guid.Empty.ToString(), brokerInteractiveRequest.BrokerPayload[BrokerParameter.CorrelationId]);
                Assert.AreEqual(MsalIdHelper.GetMsalVersion(), brokerInteractiveRequest.BrokerPayload[BrokerParameter.ClientVersion]);
                Assert.AreEqual("NO", brokerInteractiveRequest.BrokerPayload[BrokerParameter.Force]);
                Assert.AreEqual(string.Empty, brokerInteractiveRequest.BrokerPayload[BrokerParameter.Username]);

                Assert.AreEqual(MsalTestConstants.RedirectUri, brokerInteractiveRequest.BrokerPayload[BrokerParameter.RedirectUri]);

                Assert.AreEqual(MsalTestConstants.BrokerExtraQueryParameters, brokerInteractiveRequest.BrokerPayload[BrokerParameter.ExtraQp]);

                //Assert.AreEqual(MsalTestConstants.BrokerClaims, brokerInteractiveRequest._brokerPayload[BrokerParameter.Claims]); //TODO
                Assert.AreEqual(BrokerParameter.OidcScopesValue, brokerInteractiveRequest.BrokerPayload[BrokerParameter.ExtraOidcScopes]);
            }
        }
    }
}
