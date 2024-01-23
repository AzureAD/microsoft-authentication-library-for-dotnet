// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Cache
{
    /// <remarks>
    /// The dictionary serializer does not handle unknown nodes.
    /// </remarks>
    internal class TokenCacheDictionarySerializer : ITokenCacheSerializable
    {
        private const string AccessTokenKey = "access_tokens";
        private const string RefreshTokenKey = "refresh_tokens";
        private const string IdTokenKey = "id_tokens";
        private const string AccountKey = "accounts";
        private readonly ITokenCacheAccessor _accessor;

        public TokenCacheDictionarySerializer(ITokenCacheAccessor accessor)
        {
            _accessor = accessor;
        }

        public byte[] Serialize(IDictionary<string, JToken> unknownNodes)
        {
            var accessTokensAsString = new List<string>();
            var refreshTokensAsString = new List<string>();
            var idTokensAsString = new List<string>();
            var accountsAsString = new List<string>();

            foreach (var accessToken in _accessor.GetAllAccessTokens())
            {
                accessTokensAsString.Add(accessToken.ToJsonString());
            }

            foreach (var refreshToken in _accessor.GetAllRefreshTokens())
            {
                refreshTokensAsString.Add(refreshToken.ToJsonString());
            }

            foreach (var idToken in _accessor.GetAllIdTokens())
            {
                idTokensAsString.Add(idToken.ToJsonString());
            }

            foreach (var account in _accessor.GetAllAccounts())
            {
                accountsAsString.Add(account.ToJsonString());
            }

            // reads the underlying in-memory dictionary and dumps out the content as a JSON
            var cacheDict = new Dictionary<string, IEnumerable<string>>
            {
                [AccessTokenKey] = accessTokensAsString,
                [RefreshTokenKey] = refreshTokensAsString,
                [IdTokenKey] = idTokensAsString,
                [AccountKey] = accountsAsString
            };

            // Serializes as an array of Key Value pairs
            return JsonHelper.SerializeToJson(cacheDict.ToList())
                             .ToByteArray();
        }

        public IDictionary<string, JToken> Deserialize(byte[] bytes, bool clearExistingCacheData)
        {
            List<KeyValuePair<string, IEnumerable<string>>> cacheKvpList;

            try
            {
                cacheKvpList = JsonHelper.DeserializeFromJson<List<KeyValuePair<string, IEnumerable<string>>>>(bytes);
            }
            catch (Exception ex)
            {
                throw new MsalClientException(MsalError.JsonParseError, MsalErrorMessage.TokenCacheDictionarySerializerFailedParse, ex);
            }

            var cacheDict = cacheKvpList.ToDictionary(x => x.Key, x => x.Value);

            if (clearExistingCacheData)
            {
                _accessor.Clear();
            }

            if (cacheKvpList == null || cacheKvpList.Count == 0)
            {
                return null;
            }

            if (cacheDict.TryGetValue(AccessTokenKey, out IEnumerable<string> accessTokenKeys))
            {
                foreach (var atItem in accessTokenKeys)
                {
                    _accessor.SaveAccessToken(MsalAccessTokenCacheItem.FromJsonString(atItem));
                }
            }

            if (cacheDict.TryGetValue(RefreshTokenKey, out IEnumerable<string> refreshTokenKeys))
            {
                foreach (var rtItem in refreshTokenKeys)
                {
                    _accessor.SaveRefreshToken(MsalRefreshTokenCacheItem.FromJsonString(rtItem));
                }
            }

            if (cacheDict.TryGetValue(IdTokenKey, out IEnumerable<string> idTokenKeys))
            {
                foreach (var idItem in idTokenKeys)
                {
                    _accessor.SaveIdToken(MsalIdTokenCacheItem.FromJsonString(idItem));
                }
            }

            if (cacheDict.TryGetValue(AccountKey, out IEnumerable<string> accountKeys))
            {
                foreach (var account in accountKeys)
                {
                    _accessor.SaveAccount(MsalAccountCacheItem.FromJsonString(account));
                }
            }

            return null;
        }
    }
}
