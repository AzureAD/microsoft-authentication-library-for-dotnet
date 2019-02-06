//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Cache
{
    internal static class CacheFallbackOperations
    {
        internal /* internal for testing only */ const string DifferentEnvError = 
            "Not expecting the RT and IdT to have different env when adding to legacy cache";
        internal /* internal for testing only */ const string DifferentAuthorityError = 
            "Not expecting authority to have a different env than the RT and IdT";

        public static void WriteAdalRefreshToken(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence,
            MsalRefreshTokenCacheItem rtItem,
            MsalIdTokenCacheItem idItem,
            string authority,
            string uniqueId,
            string scope)
        {
            try
            {
                if (rtItem == null)
                {
                    logger.Info("No refresh token available. Skipping MSAL refresh token cache write");
                    return;
                }

                //Using scope instead of resource because that value does not exist. STS should return it.
                AdalTokenCacheKey key = new AdalTokenCacheKey(authority, scope, rtItem.ClientId, TokenSubjectType.User,
                uniqueId, idItem.IdToken.PreferredUsername);
                AdalResultWrapper wrapper = new AdalResultWrapper()
                {
                    Result = new AdalResult(null, null, DateTimeOffset.MinValue)
                    {
                        UserInfo = new AdalUserInfo()
                        {
                            UniqueId = uniqueId,
                            DisplayableId = idItem.IdToken.PreferredUsername
                        }
                    },
                    RefreshToken = rtItem.Secret,
                    RawClientInfo = rtItem.RawClientInfo,
                    //ResourceInResponse is needed to treat RT as an MRRT. See IsMultipleResourceRefreshToken 
                    //property in AdalResultWrapper and its usage. Stronger design would be for the STS to return resource
                    //for which the token was issued as well on v2 endpoint.
                    ResourceInResponse = scope
                };

                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());
                dictionary[key] = wrapper;
                legacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(logger, dictionary));
            }
            catch (Exception ex)
            {
                if (!string.Equals(rtItem?.Environment, idItem?.Environment, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Error(DifferentEnvError);
                }

                if (!string.Equals(rtItem?.Environment, new Uri(authority).Host, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Error(DifferentAuthorityError);
                }

                logger.WarningPiiWithPrefix(ex, "An error occurred while writing MSAL refresh token to the cache in ADAL format. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ");
            }
        }

        /// <summary>
        /// Returns a tuple where
        /// 
        /// Item1 is a map of ClientInfo -> AdalUserInfo for those users that have ClientInfo 
        /// Item2 is a list of AdalUserInfo for those users that do not have ClientInfo
        /// </summary>
        public static AdalUsersForMsalResult GetAllAdalUsersForMsal(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence, 
            string clientId)
        {
            var clientInfoToAdalUserMap = new Dictionary<string, AdalUserInfo>();
            var adalUsersWithoutClientInfo = new List<AdalUserInfo>();
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());
                // filter by client id and environment first
                // TODO - authority check needs to be updated for alias check
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> pair in listToProcess)
                {
                    if (!string.IsNullOrEmpty(pair.Value.RawClientInfo))
                    {
                        clientInfoToAdalUserMap[pair.Value.RawClientInfo] = pair.Value.Result.UserInfo;
                    }
                    else
                    {
                        adalUsersWithoutClientInfo.Add(pair.Value.Result.UserInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.WarningPiiWithPrefix(ex, "An error occurred while reading accounts in ADAL format from the cache for MSAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ");

                return new AdalUsersForMsalResult();
            }
            return new AdalUsersForMsalResult(clientInfoToAdalUserMap, adalUsersWithoutClientInfo);
        }

        /// <summary>
        /// Algorithm to delete: 
        /// 
        /// DisplayableId cannot be null 
        /// Removal is scoped by enviroment and clientId;
        /// 
        /// If accountId != null then delete everything with the same clientInfo
        /// otherwise, delete everything with the same displayableId
        /// 
        /// Notes: 
        /// - displayableId can change rarely
        /// - ClientCredential Grant uses the app token cache, not the user token cache, so this algorithm does not apply
        /// (nor will GetAccounts / RemoveAccount work)
        /// 
        /// </summary>
        public static void RemoveAdalUser(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence,
            string clientId,
            string displayableId,
            string accountOrUserId)
        {
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache =
                    AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());

                if (!string.IsNullOrEmpty(accountOrUserId))
                {
                    RemoveEntriesWithMatchingId(clientId, accountOrUserId, adalCache);
                }

                RemoveEntriesWithMatchingName(logger, clientId, displayableId, adalCache);
                legacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(logger, adalCache));
            }
            catch (Exception ex)
            {
                logger.WarningPiiWithPrefix(ex, "An error occurred while deleting account in ADAL format from the cache. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ");
            }
        }

        private static void RemoveEntriesWithMatchingName(
            ICoreLogger logger,
            string clientId,
            string displayableId,
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache)
        {
            if (string.IsNullOrEmpty(displayableId))
            {
                logger.Error(CoreErrorMessages.InternalErrorCacheEmptyUsername);
                return;
            }

            var keysToRemove = new List<AdalTokenCacheKey>();

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
                string cachedEnvironment = new Uri(kvp.Key.Authority).Host;
                string cachedAccountDisplayableId = kvp.Key.DisplayableId;
                string cachedClientId = kvp.Key.ClientId;

                if (string.Equals(displayableId, cachedAccountDisplayableId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(clientId, cachedClientId, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (AdalTokenCacheKey key in keysToRemove)
            {
                adalCache.Remove(key);
            }
        }

        private static void RemoveEntriesWithMatchingId(
            string clientId,
            string accountOrUserId,
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache)
        {
            var keysToRemove = new List<AdalTokenCacheKey>();

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
                string rawClientInfo = kvp.Value.RawClientInfo;

                if (!string.IsNullOrEmpty(rawClientInfo))
                {
                    string cachedAccountId = ClientInfo.CreateFromJson(rawClientInfo).ToAccountIdentifier();
                    string cachedEnvironment = new Uri(kvp.Key.Authority).Host;
                    string cachedClientId = kvp.Key.ClientId;

                    if (string.Equals(accountOrUserId, cachedAccountId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(clientId, cachedClientId, StringComparison.OrdinalIgnoreCase))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

            }

            foreach (AdalTokenCacheKey key in keysToRemove)
            {
                adalCache.Remove(key);
            }
        }

        public static List<MsalRefreshTokenCacheItem> GetAllAdalEntriesForMsal(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence,
            ISet<string> environmentAliases, 
            string clientId, 
            string upn, 
            string uniqueId, 
            string rawClientInfo)
        {
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());
                //filter by client id and environment first
                //TODO - authority check needs to be updated for alias check
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase) &&
                        environmentAliases.Contains(new Uri(p.Key.Authority).Host)).ToList();

                //if client info is provided then use it to filter
                if (!string.IsNullOrEmpty(rawClientInfo))
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> clientInfoEntries =
                        listToProcess.Where(p => rawClientInfo.Equals(p.Value.RawClientInfo, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (clientInfoEntries.Any())
                    {
                        listToProcess = clientInfoEntries;
                    }
                }

                //if upn is provided then use it to filter
                if (!string.IsNullOrEmpty(upn))
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> upnEntries =
                        listToProcess.Where(p => upn.Equals(p.Key.DisplayableId, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (upnEntries.Any())
                    {
                        listToProcess = upnEntries;
                    }
                }

                //if userId is provided then use it to filter
                if (!string.IsNullOrEmpty(uniqueId))
                {
                    List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> uniqueIdEntries =
                        listToProcess.Where(p => uniqueId.Equals(p.Key.UniqueId, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (uniqueIdEntries.Any())
                    {
                        listToProcess = uniqueIdEntries;
                    }
                }

                List<MsalRefreshTokenCacheItem> list = new List<MsalRefreshTokenCacheItem>();
                foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> pair in listToProcess)
                {
                    list.Add(new MsalRefreshTokenCacheItem
                        (new Uri(pair.Key.Authority).Host, pair.Key.ClientId, pair.Value.RefreshToken, pair.Value.RawClientInfo));
                }

                return list;
            }
            catch (Exception ex)
            {
                logger.WarningPiiWithPrefix(ex, "An error occurred while searching for refresh tokens in ADAL format in the cache for MSAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ");

                return new List<MsalRefreshTokenCacheItem>();
            }
        }

        public static MsalRefreshTokenCacheItem GetAdalEntryForMsal(
            ICoreLogger logger,
            ILegacyCachePersistence legacyCachePersistence,
            string preferredEnvironment, 
            ISet<string> environmentAliases, 
            string clientId, 
            string upn, 
            string uniqueId, 
            string rawClientInfo)
        {
            var adalRts = GetAllAdalEntriesForMsal(logger, legacyCachePersistence, environmentAliases, clientId, upn, uniqueId, rawClientInfo);

            List<MsalRefreshTokenCacheItem> filteredByPrefEnv = adalRts.Where
                (rt => rt.Environment.Equals(preferredEnvironment, StringComparison.OrdinalIgnoreCase)).ToList();

            if (filteredByPrefEnv.Any())
            {
                return filteredByPrefEnv.First();
            }
            else
            {
                return adalRts.FirstOrDefault();
            }
        }
    }
}
