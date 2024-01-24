// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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
            Assert.AreEqual(expectedAtCount, accessor.GetAllAccessTokens().Count, "Access Tokens");
            Assert.AreEqual(expectedRtCount, accessor.GetAllRefreshTokens().Count, "Refresh Tokens");
            Assert.AreEqual(expectedIdtCount, accessor.GetAllIdTokens().Count, "Id Tokens");
            Assert.AreEqual(expectedAccountCount, accessor.GetAllAccounts().Count, "Accounts");
            Assert.AreEqual(expectedAppMetadataCount, accessor.GetAllAppMetadata().Count, "App Metadata");
        }

        public static void InitializeTokenCacheFromFile(this IClientApplicationBase app, string resourceFile, bool updateATExpiry = false)
        {
            string tokenCacheAsString = File.ReadAllText(resourceFile);
            InitializeTokenCacheFromString(app, tokenCacheAsString, updateATExpiry);
        }

        public static void InitializeTokenCacheFromString(this IClientApplicationBase app, string content, bool updateATExpiry = false)
        {
            if (updateATExpiry)
            {
                var cacheJson = JObject.Parse(content);

                JEnumerable<JToken> tokens = cacheJson["AccessToken"].Children();
                foreach (JToken token in tokens)
                {
                    var obj = token.Children().Single() as JObject;

                    obj["expires_on"] = DateTimeHelpers.DateTimeToUnixTimestamp(DateTimeOffset.Now.AddMinutes(100));
                    obj["extended_expires_on"] = DateTimeHelpers.DateTimeToUnixTimestamp(DateTimeOffset.Now.AddMinutes(100));
                }

                content = cacheJson.ToString();

            }

            byte[] tokenCacheBlob = new UTF8Encoding().GetBytes(content);
            ((ITokenCacheSerializer)app.UserTokenCache).DeserializeMsalV3(tokenCacheBlob);
        }
    }
}
