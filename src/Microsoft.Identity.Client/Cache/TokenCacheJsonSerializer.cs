// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Cache
{
    internal class TokenCacheJsonSerializer : ITokenCacheSerializer
    {
        private readonly ITokenCacheAccessor _accessor;

        public TokenCacheJsonSerializer(ITokenCacheAccessor accessor)
        {
            _accessor = accessor;
        }

        public byte[] Serialize(IDictionary<string, JToken> unkownNodes)
        {
            var cache = new CacheSerializationContract(unkownNodes);
            foreach (var token in _accessor.GetAllAccessTokens())
            {
                cache.AccessTokens[token.GetKey()
                                    .ToString()] = token;
            }

            foreach (var token in _accessor.GetAllRefreshTokens())
            {
                cache.RefreshTokens[token.GetKey()
                                     .ToString()] = token;
            }

            foreach (var token in _accessor.GetAllIdTokens())
            {
                cache.IdTokens[token.GetKey()
                                .ToString()] = token;
            }

            foreach (var accountItem in _accessor.GetAllAccounts())
            {
                cache.Accounts[accountItem.GetKey()
                                .ToString()] = accountItem;
            }

            foreach (var appMetadata in _accessor.GetAllAppMetadata())
            {
                cache.AppMetadata[appMetadata.GetKey()
                    .ToString()] = appMetadata;
            }

            return cache.ToJsonString()
                        .ToByteArray();
        }

        public IDictionary<string, JToken> Deserialize(byte[] bytes)
        {
            CacheSerializationContract cache;

            try
            {
                cache = CacheSerializationContract.FromJsonString(CoreHelpers.ByteArrayToString(bytes));
            }
            catch (Exception ex)
            {
                throw new MsalClientException(MsalError.JsonParseError, MsalErrorMessage.TokenCacheJsonSerializerFailedParse, ex);
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
