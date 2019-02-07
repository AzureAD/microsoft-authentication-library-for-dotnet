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

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class ApplicationGrantIntegrationTest
    {
        public const string Authority = "";
        public const string ClientId = "";
        public const string RedirectUri = "http://localhost";
        public string[] MsalScopes = { "https://graph.microsoft.com/.default" };
        private readonly string _password = "";

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
            var confidentialClient = ConfidentialClientApplicationBuilder
                                     .Create(ClientId).WithAuthority(new Uri(Authority), true).WithRedirectUri(RedirectUri)
                                     .WithClientSecret(_password).BuildConcrete();

            var res = await confidentialClient.AcquireTokenForClientAsync(MsalScopes).ConfigureAwait(false);

            ITokenCacheInternal userCache = confidentialClient.UserTokenCacheInternal;
            ITokenCacheInternal appCache = confidentialClient.AppTokenCacheInternal;

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.AccessToken);
            Assert.IsNull(res.IdToken);
            Assert.IsNull(res.Account);

            // make sure user cache is empty
            Assert.AreEqual(0, userCache.Accessor.AccessTokenCount);
            Assert.AreEqual(0, userCache.Accessor.RefreshTokenCount);
            Assert.AreEqual(0, userCache.Accessor.IdTokenCount);
            Assert.AreEqual(0, userCache.Accessor.AccountCount);

            // make sure nothing was written to legacy cache
            Assert.IsNull(userCache.LegacyPersistence.LoadCache());

            // make sure only AT entity was stored in the App msal cache
            Assert.AreEqual(1, appCache.Accessor.AccessTokenCount);
            Assert.AreEqual(0, appCache.Accessor.RefreshTokenCount);
            Assert.AreEqual(0, appCache.Accessor.IdTokenCount);
            Assert.AreEqual(0, appCache.Accessor.AccountCount);

            Assert.IsNull(appCache.LegacyPersistence.LoadCache());

            // passing empty password to make sure that AT returned from cache
            confidentialClient = ConfidentialClientApplicationBuilder
                                 .Create(ClientId).WithAuthority(new Uri(Authority), true).WithRedirectUri(RedirectUri)
                                 .WithClientSecret("wrong_password").BuildConcrete();
            confidentialClient.AppTokenCacheInternal.Deserialize(appCache.Serialize());
            confidentialClient.UserTokenCacheInternal.Deserialize(userCache.Serialize());

            res = await confidentialClient.AcquireTokenForClientAsync(MsalScopes).ConfigureAwait(false);

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.AccessToken);
            Assert.IsNull(res.IdToken);
            Assert.IsNull(res.Account);
        }
    }
}
