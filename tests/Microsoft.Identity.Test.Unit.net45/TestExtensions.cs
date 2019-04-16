// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
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

        public static void InitializeTokenCacheFromFile(this IPublicClientApplication pca, string resourceFile)
        {
            string tokenCacheAsString = File.ReadAllText(resourceFile);
            byte[] tokenCacheBlob = new UTF8Encoding().GetBytes(tokenCacheAsString);

            pca.UserTokenCache.DeserializeMsalV3(tokenCacheBlob);

        }

    }
}
