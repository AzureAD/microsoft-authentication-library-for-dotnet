// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;

#pragma warning disable CS0618 // Type or member is obsolete (CacheData)
namespace Microsoft.Identity.Client.Extensions.Msal.UnitTests
{
    internal class MockTokenCache : ITokenCache, ITokenCacheSerializer
    {
        private TokenCacheCallback _beforeAccess;
        private TokenCacheCallback _afterAccess;

        internal int DeserializeMsalV3_ClearCache { get; set; }
        internal int DeserializeMsalV3_MergeCache { get; set; }

        internal string LastDeserializedString { get; set; }

        public void Deserialize(byte[] msalV2State)
        {
            throw new NotImplementedException();
        }

        public void DeserializeAdalV3(byte[] adalV3State)
        {
            throw new NotImplementedException();
        }

        public void DeserializeMsalV2(byte[] msalV2State)
        {
            throw new NotImplementedException();
        }

        public void DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache = false)
        {
            LastDeserializedString = Encoding.UTF8.GetString(msalV3State);

            if (shouldClearExistingCache)
            {
                DeserializeMsalV3_ClearCache++;
            }
            else
            {
                DeserializeMsalV3_MergeCache++;
            }
        }

        public void DeserializeUnifiedAndAdalCache(CacheData cacheData)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeAdalV3()
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeMsalV2()
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeMsalV3()
        {
            return Encoding.UTF8.GetBytes(LastDeserializedString);
        }

        public CacheData SerializeUnifiedAndAdalCache()
        {
            throw new NotImplementedException();
        }

        public void SetAfterAccess(TokenCacheCallback afterAccess)
        {
            _afterAccess = afterAccess;
        }

        public void SetAfterAccessAsync(Func<TokenCacheNotificationArgs, Task> afterAccess) => throw new NotImplementedException();

        public void SetBeforeAccess(TokenCacheCallback beforeAccess)
        {
            _beforeAccess = beforeAccess;
        }

        public void SetBeforeAccessAsync(Func<TokenCacheNotificationArgs, Task> beforeAccess) => throw new NotImplementedException();

        public void SetBeforeWrite(TokenCacheCallback beforeWrite)
        {
            throw new NotImplementedException();
        }

        public void SetBeforeWriteAsync(Func<TokenCacheNotificationArgs, Task> beforeWrite) => throw new NotImplementedException();
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
