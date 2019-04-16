// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class ApplicationGrantIntegrationTest
    {
        public const string Authority = "";
        public const string ClientId = "";
        public const string RedirectUri = "http://localhost";
        private readonly string[] _msalScopes = { "https://graph.microsoft.com/.default" };
        private readonly string _password = "";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

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

            var res = await confidentialClient
                .AcquireTokenForClient(_msalScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ITokenCacheInternal userCache = confidentialClient.UserTokenCacheInternal;
            ITokenCacheInternal appCache = confidentialClient.AppTokenCacheInternal;

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.AccessToken);
            Assert.IsNull(res.IdToken);
            Assert.IsNull(res.Account);

            // make sure user cache is empty
            Assert.AreEqual(0, userCache.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(0, userCache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, userCache.Accessor.GetAllIdTokens().Count());
            Assert.AreEqual(0, userCache.Accessor.GetAllAccounts().Count());

            // make sure nothing was written to legacy cache
            Assert.IsNull(userCache.LegacyPersistence.LoadCache());

            // make sure only AT entity was stored in the App msal cache
            Assert.AreEqual(1, userCache.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(0, appCache.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, appCache.Accessor.GetAllIdTokens().Count());
            Assert.AreEqual(0, appCache.Accessor.GetAllAccounts().Count());

            Assert.IsNull(appCache.LegacyPersistence.LoadCache());

            // passing empty password to make sure that AT returned from cache
            confidentialClient = ConfidentialClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(new Uri(Authority), true)
                .WithRedirectUri(RedirectUri)
                .WithClientSecret("wrong_password")
                .BuildConcrete();

            confidentialClient.AppTokenCacheInternal.DeserializeMsalV3(appCache.SerializeMsalV3());
            confidentialClient.UserTokenCacheInternal.DeserializeMsalV3(userCache.SerializeMsalV3());

            res = await confidentialClient
                .AcquireTokenForClient(_msalScopes)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(res);
            Assert.IsNotNull(res.AccessToken);
            Assert.IsNull(res.IdToken);
            Assert.IsNull(res.Account);
        }
    }
}
