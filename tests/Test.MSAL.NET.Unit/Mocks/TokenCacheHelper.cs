using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;

namespace Test.MSAL.NET.Unit.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;

        public static TokenCache CreateCacheWithItems()
        {
        TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                RootId = TestConstants.DefaultRootId
            };
            ex.Result.ScopeSet = TestConstants.DefaultScope;

            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            key = new TokenCacheKey(TestConstants.DefaultAuthorityGuestTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId + "more",
                RootId = TestConstants.DefaultRootId
            };
            ex.Result.ScopeSet = TestConstants.ScopeForAnotherResource;
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            return cache;
        }

        public static void ExpireCacheItems(TokenCache cache)
        {
            foreach (var value in cache.tokenCacheDictionary.Values)
            {
                value.Result.ExpiresOn =  DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            }
        }
    }
}
