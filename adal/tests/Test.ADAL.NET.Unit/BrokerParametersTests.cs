//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class BrokerParametersTests
    {
        private const string Authority = "https://login.microsoftonline.com/test";
        private static readonly string CanonicalizedAuthority = Authenticator.EnsureUrlEndsWithForwardSlash(Authority);

        private const string ExtraQueryParameters = "testQueryParameters";
        private const string Claims = "testClaims";
        private const string Resource = "testResource";

        private const string ClientId = "testClientId";
        private const string ClientSecret = "testClientSecret";

        private const string UniqueUserId = "testUniqueUserId";

        private readonly RequestData _requestData = new RequestData
        {
            Authenticator = new Authenticator(Authority, false),
            Resource = Resource,
            ClientKey = new ClientKey(new ClientCredential(ClientId, ClientSecret)),
            SubjectType = TokenSubjectType.Client,
            ExtendedLifeTimeEnabled = false
        };

        [TestMethod]
        [Description("Test setting of brokerParameters by AcquireTokenInteractiveHandler constructor")]
        public void AcquireTokenInteractiveHandlerConstructor_InitializeBrokerParameters()
        {
            var acquireTokenInteractiveHandler = new AcquireTokenInteractiveHandler(_requestData,
                TestConstants.DefaultRedirectUri, null, UserIdentifier.AnyUser,
                ExtraQueryParameters, null, Claims);

            Assert.AreEqual(11, acquireTokenInteractiveHandler.brokerParameters.Count);

            var brokerParams = acquireTokenInteractiveHandler.brokerParameters;

            Assert.AreEqual(CanonicalizedAuthority, brokerParams[BrokerParameter.Authority]);
            Assert.AreEqual(Resource, brokerParams[BrokerParameter.Resource]);
            Assert.AreEqual(ClientId, brokerParams[BrokerParameter.ClientId]);

            Assert.AreEqual(acquireTokenInteractiveHandler.RequestContext.Logger.CorrelationId.ToString(), brokerParams[BrokerParameter.CorrelationId]);
            Assert.AreEqual(AdalIdHelper.GetAdalVersion(), brokerParams[BrokerParameter.ClientVersion]);
            Assert.AreEqual("NO", brokerParams[BrokerParameter.Force]);
            Assert.AreEqual(string.Empty, brokerParams[BrokerParameter.Username]);
            Assert.AreEqual(UserIdentifierType.OptionalDisplayableId.ToString(), brokerParams[BrokerParameter.UsernameType]);

            Assert.AreEqual(TestConstants.DefaultRedirectUri, brokerParams[BrokerParameter.RedirectUri]);

            Assert.AreEqual(ExtraQueryParameters, brokerParams[BrokerParameter.ExtraQp]);
            Assert.AreEqual(Claims, brokerParams[BrokerParameter.Claims]);
        }

        [TestMethod]
        [Description("Test setting of brokerParameters by AcquireTokenSilentHandler constructor")]
        public void AcquireTokenSilentHandlerConstructor_InitializeBrokerParameters()
        {
            var acquireTokenSilentHandler = new AcquireTokenSilentHandler(_requestData, new UserIdentifier(UniqueUserId, UserIdentifierType.UniqueId), null);

            Assert.AreEqual(8, acquireTokenSilentHandler.brokerParameters.Count);

            var brokerParams = acquireTokenSilentHandler.brokerParameters;

            Assert.AreEqual(CanonicalizedAuthority, brokerParams[BrokerParameter.Authority]);
            Assert.AreEqual(Resource, brokerParams[BrokerParameter.Resource]);
            Assert.AreEqual(ClientId, brokerParams[BrokerParameter.ClientId]);
            Assert.AreEqual(acquireTokenSilentHandler.RequestContext.Logger.CorrelationId.ToString(), brokerParams[BrokerParameter.CorrelationId]);
            Assert.AreEqual(AdalIdHelper.GetAdalVersion(), brokerParams[BrokerParameter.ClientVersion]);
            Assert.AreEqual(UniqueUserId, brokerParams[BrokerParameter.Username]);
            Assert.AreEqual(UserIdentifierType.UniqueId.ToString(), brokerParams[BrokerParameter.UsernameType]);

            Assert.IsTrue(brokerParams.ContainsKey(BrokerParameter.SilentBrokerFlow));
        }
    }
}
