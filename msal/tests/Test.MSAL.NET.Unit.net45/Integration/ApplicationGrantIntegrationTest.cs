//------------------------------------------------------------------------------
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

using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Microsoft.Identity.LabInfrastructure;

namespace Test.MSAL.NET.Unit.net45.Integration
{
    [TestClass]
    public class ApplicationGrantIntegrationTest
    {
        public const string Authority = "";
        public const string ClientId = "";
        public const string RedirectUri = "http://localhost";
        public string[] MsalScopes = { "https://graph.microsoft.com/.default" };
        private string _password = "";

        static ApplicationGrantIntegrationTest()
        {
            // init password here
            // Required lab API doesn't exist yet.
        }

        [TestMethod]
        [TestCategory("ApplicationGrant_IntegrationTests")]
        [Ignore]
        public async Task ApplicationGrantIntegrationTestAsync()
        {
            var appCache = new TokenCache();
            var userCache = new TokenCache();

            var confidentialClient = new ConfidentialClientApplication(ClientId, Authority, RedirectUri,
                new ClientCredential(_password), userCache, appCache);

            var res = await confidentialClient.AcquireTokenForClientAsync(MsalScopes).ConfigureAwait(false);

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.AccessToken);
            Assert.IsNull(res.IdToken);
            Assert.IsNull(res.Account);

            // make sure user cache is empty
            Assert.IsTrue(userCache.tokenCacheAccessor.GetAllAccessTokensAsString().Count == 0);
            Assert.IsTrue(userCache.tokenCacheAccessor.GetAllRefreshTokensAsString().Count == 0);
            Assert.IsTrue(userCache.tokenCacheAccessor.GetAllIdTokensAsString().Count == 0);
            Assert.IsTrue(userCache.tokenCacheAccessor.GetAllAccountsAsString().Count == 0);

            // make sure nothing was written to legacy cache
            Assert.IsNull(userCache.legacyCachePersistence.LoadCache());

            // make sure only AT entity was stored in the App msal cache
            Assert.IsTrue(appCache.tokenCacheAccessor.GetAllAccessTokensAsString().Count == 1);
            Assert.IsTrue(appCache.tokenCacheAccessor.GetAllRefreshTokensAsString().Count == 0);
            Assert.IsTrue(appCache.tokenCacheAccessor.GetAllIdTokensAsString().Count == 0);
            Assert.IsTrue(appCache.tokenCacheAccessor.GetAllAccountsAsString().Count == 0);

            Assert.IsNull(appCache.legacyCachePersistence.LoadCache());

            // passing empty password to make sure that AT returned from cache
            confidentialClient = new ConfidentialClientApplication(ClientId, Authority, RedirectUri,
                new ClientCredential("wrong_password"), userCache, appCache);

            res = await confidentialClient.AcquireTokenForClientAsync(MsalScopes).ConfigureAwait(false);

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.AccessToken);
            Assert.IsNull(res.IdToken);
            Assert.IsNull(res.Account);
        }
    }
}
