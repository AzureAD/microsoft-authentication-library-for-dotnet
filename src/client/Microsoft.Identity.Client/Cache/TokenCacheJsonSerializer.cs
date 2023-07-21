// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using JToken = System.Text.Json.Nodes.JsonNode;
#else
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Cache
{
    internal class TokenCacheJsonSerializer : ITokenCacheSerializable
    {
        private readonly ITokenCacheAccessor _accessor;

        public TokenCacheJsonSerializer(ITokenCacheAccessor accessor)
        {
            _accessor = accessor;
        }

        public byte[] Serialize(IDictionary<string, JToken> unknownNodes)
        {
            var cache = new CacheSerializationContract(unknownNodes);
            foreach (var token in _accessor.GetAllAccessTokens())
            {
                cache.AccessTokens[token.CacheKey] = token;
            }

            foreach (var token in _accessor.GetAllRefreshTokens())
            {
                cache.RefreshTokens[token.CacheKey] = token;
            }

            foreach (var token in _accessor.GetAllIdTokens())
            {
                cache.IdTokens[token.CacheKey] = token;
            }

            foreach (var accountItem in _accessor.GetAllAccounts())
            {
                cache.Accounts[accountItem.CacheKey] = accountItem;
            }

            foreach (var appMetadata in _accessor.GetAllAppMetadata())
            {
                cache.AppMetadata[appMetadata.CacheKey] = appMetadata;
            }

            return cache.ToJsonString()
                        .ToByteArray();
        }

        public IDictionary<string, JToken> Deserialize(byte[] bytes, bool clearExistingCacheData)
        {
            CacheSerializationContract cache;
            string cacheAsString = CoreHelpers.ByteArrayToString(bytes);
            try
            {
                cache = CacheSerializationContract.FromJsonString(cacheAsString);
            }
            catch (Exception ex)
            {
                // see if the string is at least in JSON format. First few characters do not have any personal / secret data.
                string firstFewCharacters = cacheAsString.Length > 5 ? cacheAsString.Substring(0, 5) : cacheAsString;

                throw new MsalClientException(
                    MsalError.JsonParseError, 
                    string.Format(MsalErrorMessage.TokenCacheJsonSerializerFailedParse, firstFewCharacters, ex),
                    ex);
            }

            if (clearExistingCacheData)
            {
                _accessor.Clear();
            }

            if (cache.AccessTokens != null)
            {
                foreach (var atItem in cache.AccessTokens.Values)
                {
                    _accessor.SaveAccessToken(atItem);
                }
            }

            if (cache.RefreshTokens != null)
            {
                foreach (var rtItem in cache.RefreshTokens.Values)
                {
                    _accessor.SaveRefreshToken(rtItem);
                }
            }

            if (cache.IdTokens != null)
            {
                foreach (var idItem in cache.IdTokens.Values)
                {
                    _accessor.SaveIdToken(idItem);
                }
            }

            if (cache.Accounts != null)
            {
                foreach (var account in cache.Accounts.Values)
                {
                    _accessor.SaveAccount(account);
                }
            }

            if (cache.AppMetadata != null)
            {
                foreach (var appMetadata in cache.AppMetadata.Values)
                {
                    _accessor.SaveAppMetadata(appMetadata);
                }
            }

            return cache.UnknownNodes;
        }
    }
}
