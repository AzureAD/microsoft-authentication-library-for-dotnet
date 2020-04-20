// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client;
using System;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class BrokerParametersTest : TestBase
    {
        public static readonly string s_canonicalizedAuthority = AuthorityInfo.CanonicalizeAuthorityUri(CoreHelpers.UrlDecode(TestConstants.AuthorityTestTenant));

        [TestMethod]
        [Description("Test setting of the broker parameters in the BrokerInteractiveRequest constructor.")]
        public void BrokerInteractiveRequest_CreateBrokerParametersTest()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    null, 
                    TestConstants.ExtraQueryParameters);

                AcquireTokenInteractiveParameters interactiveParameters = new AcquireTokenInteractiveParameters();

                // Act
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                BrokerInteractiveRequestComponent brokerInteractiveRequest = 
                    new BrokerInteractiveRequestComponent(
                        parameters,
                        interactiveParameters, 
                        broker, 
                        null);

                brokerInteractiveRequest.CreateRequestParametersForBroker();

                // Assert
                Assert.AreEqual(11, brokerInteractiveRequest.BrokerPayload.Count);

                Assert.AreEqual(s_canonicalizedAuthority, brokerInteractiveRequest.BrokerPayload[BrokerParameter.Authority]);
                Assert.AreEqual(TestConstants.ScopeStr, brokerInteractiveRequest.BrokerPayload[BrokerParameter.Scope]);
                Assert.AreEqual(TestConstants.ClientId, brokerInteractiveRequest.BrokerPayload[BrokerParameter.ClientId]);

                Assert.IsFalse(string.IsNullOrEmpty(brokerInteractiveRequest.BrokerPayload[BrokerParameter.CorrelationId]));
                Assert.AreNotEqual(Guid.Empty.ToString(), brokerInteractiveRequest.BrokerPayload[BrokerParameter.CorrelationId]);
                Assert.AreEqual(MsalIdHelper.GetMsalVersion(), brokerInteractiveRequest.BrokerPayload[BrokerParameter.ClientVersion]);
                Assert.AreEqual("NO", brokerInteractiveRequest.BrokerPayload[BrokerParameter.Force]);
                Assert.AreEqual(string.Empty, brokerInteractiveRequest.BrokerPayload[BrokerParameter.Username]);

                Assert.AreEqual(TestConstants.RedirectUri, brokerInteractiveRequest.BrokerPayload[BrokerParameter.RedirectUri]);

                Assert.AreEqual(TestConstants.BrokerExtraQueryParameters, brokerInteractiveRequest.BrokerPayload[BrokerParameter.ExtraQp]);

                //Assert.AreEqual(TestConstants.BrokerClaims, brokerInteractiveRequest._brokerPayload[BrokerParameter.Claims]); //TODO
                Assert.AreEqual(BrokerParameter.OidcScopesValue, brokerInteractiveRequest.BrokerPayload[BrokerParameter.ExtraOidcScopes]);


            }
        }

        [TestMethod]
        [Description("Test setting of the broker parameters in the BrokerSilentRequest constructor.")]
        public void BrokerSilentRequest_CreateBrokerParametersTest()
        {
            using (var harness = CreateTestHarness())
            {
                // Arrange
                var parameters = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityTestTenant,
                    TestConstants.s_scope,
                    new TokenCache(harness.ServiceBundle, false),
                    null,
                    TestConstants.ExtraQueryParameters);
                AcquireTokenSilentParameters acquireTokenSilentParameters = new AcquireTokenSilentParameters();

                // Act
                IBroker broker = harness.ServiceBundle.PlatformProxy.CreateBroker(null);
                BrokerSilentRequest brokerSilentRequest =
                    new BrokerSilentRequest(
                        parameters,
                        acquireTokenSilentParameters,
                        harness.ServiceBundle,
                        broker);

                brokerSilentRequest.CreateRequestParametersForBroker();

                // Assert
                Assert.AreEqual(13, brokerSilentRequest.BrokerPayload.Count);
                Assert.AreEqual(s_canonicalizedAuthority, brokerSilentRequest.BrokerPayload[BrokerParameter.Authority]);
                Assert.AreEqual(TestConstants.ScopeStr, brokerSilentRequest.BrokerPayload[BrokerParameter.Scope]);
                Assert.AreEqual(TestConstants.ClientId, brokerSilentRequest.BrokerPayload[BrokerParameter.ClientId]);
                Assert.IsFalse(string.IsNullOrEmpty(brokerSilentRequest.BrokerPayload[BrokerParameter.CorrelationId]));
                Assert.AreNotEqual(Guid.Empty.ToString(), brokerSilentRequest.BrokerPayload[BrokerParameter.CorrelationId]);
                Assert.AreEqual(MsalIdHelper.GetMsalVersion(), brokerSilentRequest.BrokerPayload[BrokerParameter.ClientVersion]);
                Assert.AreEqual(TestConstants.RedirectUri, brokerSilentRequest.BrokerPayload[BrokerParameter.RedirectUri]);
                Assert.AreEqual(TestConstants.BrokerExtraQueryParameters, brokerSilentRequest.BrokerPayload[BrokerParameter.ExtraQp]);
                Assert.AreEqual(TestConstants.BrokerOIDCScopes, brokerSilentRequest.BrokerPayload[BrokerParameter.ExtraOidcScopes]);
                Assert.IsTrue(string.IsNullOrEmpty(brokerSilentRequest.BrokerPayload[BrokerParameter.Username]));
                Assert.AreEqual("False", brokerSilentRequest.BrokerPayload[BrokerParameter.ForceRefresh]);
            }
        }
    }
}
