// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client
{
    public sealed partial class TokenCache : ITokenCacheInternal
    {
        // Unkown token cache data support for forwards compatibility.
        private IDictionary<string, JToken> _unknownNodes;

        byte[] ITokenCacheSerializer.SerializeAdalV3()
        {
            return LegacyCachePersistence.LoadCache();
        }

        void ITokenCacheSerializer.DeserializeAdalV3(byte[] adalV3State)
        {
            LegacyCachePersistence.WriteCache(adalV3State);
        }

        byte[] ITokenCacheSerializer.SerializeMsalV2()
        {
            return new TokenCacheDictionarySerializer(_accessor).Serialize(_unknownNodes);
        }

        void ITokenCacheSerializer.DeserializeMsalV2(byte[] msalV2State)
        {
            _unknownNodes = new TokenCacheDictionarySerializer(_accessor).Deserialize(msalV2State, false);
        }

        byte[] ITokenCacheSerializer.SerializeMsalV3()
        {
            return new TokenCacheJsonSerializer(_accessor).Serialize(_unknownNodes);
        }

        void ITokenCacheSerializer.DeserializeMsalV3(byte[] msalV3State, bool shouldClearExistingCache)
        {
            if (msalV3State == null || msalV3State.Length == 0)
            {
                return;
            }
            _unknownNodes = new TokenCacheJsonSerializer(_accessor).Deserialize(msalV3State, shouldClearExistingCache);
        }
    }
}
