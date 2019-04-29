// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    public static class TestExtensions
    {
        internal static void AssertItemCount(
            this ITokenCacheAccessor accessor,
            int expectedAtCount,
            int expectedRtCount,
            int expectedIdtCount,
            int expectedAccountCount,
            int expectedAppMetadataCount = 0)
        {
            Assert.AreEqual(expectedAtCount, accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(expectedRtCount, accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(expectedIdtCount, accessor.GetAllIdTokens().Count());
            Assert.AreEqual(expectedAccountCount, accessor.GetAllAccounts().Count());
            Assert.AreEqual(expectedAppMetadataCount, accessor.GetAllAppMetadata().Count());
        }

        public static void InitializeTokenCacheFromFile(this IPublicClientApplication pca, string resourceFile, bool updateATExpiry = false)
        {
            string tokenCacheAsString = File.ReadAllText(resourceFile);

            if (updateATExpiry)
            {
                var cacheJson = JObject.Parse(tokenCacheAsString);

                

                JEnumerable<JToken> tokens = cacheJson["AccessToken"].Children();
                foreach (JToken token in tokens)
                {
                    var obj = token.Children().Single() as JObject;
                    //obj["cached_at"] = DateTime.UtcNow + TimeSpan.FromMinutes(100);
                    //CoreHelpers.DateTimeToUnixTimestamp(DateTimeOffset.Now.AddMinutes(100))
                    obj["expires_on"] = CoreHelpers.DateTimeToUnixTimestamp(DateTimeOffset.Now.AddMinutes(100));
                    obj["extended_expires_on"] = CoreHelpers.DateTimeToUnixTimestamp(DateTimeOffset.Now.AddMinutes(100));
                 //   obj["ext_expires_on"] = "foo";
                }

                tokenCacheAsString = cacheJson.ToString();
            
            }


            byte[] tokenCacheBlob = new UTF8Encoding().GetBytes(tokenCacheAsString);

            pca.UserTokenCache.DeserializeMsalV3(tokenCacheBlob);
        }

    }
}
