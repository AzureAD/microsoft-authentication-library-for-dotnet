// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Cache
{
    internal static class CacheFallbackOperations
    {
        internal /* internal for testing only */ const string DifferentEnvError =
            "Not expecting the RT and IdT to have different env when adding to legacy cache";
        internal /* internal for testing only */ const string DifferentAuthorityError =
            "Not expecting authority to have a different env than the RT and IdT";

        public static void WriteAdalRefreshToken(
            ILoggerAdapter logger,
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
                    logger.Info("No refresh token available. Skipping writing to ADAL legacy cache. ");
                    return;
                }

                if (!string.IsNullOrEmpty(rtItem.FamilyId))
                {
                    logger.Info("Not writing FRT in ADAL legacy cache. ");
                    return;
                }

                //Using scope instead of resource because that value does not exist. STS should return it.
                AdalTokenCacheKey key = new AdalTokenCacheKey(authority, scope, rtItem.ClientId, TokenSubjectType.User,
                uniqueId, idItem.IdToken.PreferredUsername);
                AdalResultWrapper wrapper = new AdalResultWrapper()
                {
                    Result = new AdalResult()
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

                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(
                    logger,
                    legacyCachePersistence.LoadCache());

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
        public static AdalUsersForMsal GetAllAdalUsersForMsal(
            ILoggerAdapter logger,
            ILegacyCachePersistence legacyCachePersistence,
            string clientId)
        {
            var userEntries = new List<AdalUserForMsalEntry>();
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());

                // filter by client id
                dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(p.Key.Authority))
                        .ToList()
                        .ForEach(kvp =>
                            {
                                userEntries.Add(new AdalUserForMsalEntry(
                                    authority: kvp.Key.Authority,
                                    clientId: clientId,
                                    clientInfo: kvp.Value.RawClientInfo, // optional, missing in ADAL v3
                                    userInfo: kvp.Value.Result.UserInfo));
                            });
            }
            catch (Exception ex)
            {
                logger.WarningPiiWithPrefix(ex, "An error occurred while reading accounts in ADAL format from the cache for MSAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ");
            }

            return new AdalUsersForMsal(userEntries);
        }

        /// <summary>
        /// Algorithm to delete:
        ///
        /// DisplayableId cannot be null
        /// Removal is scoped by environment and clientId;
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
            ILoggerAdapter logger,
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
            ILoggerAdapter logger,
            string clientId,
            string displayableId,
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache)
        {
            if (string.IsNullOrEmpty(displayableId))
            {
                logger.Error(MsalErrorMessage.InternalErrorCacheEmptyUsername);
                return;
            }

            var keysToRemove = new List<AdalTokenCacheKey>();

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
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

        public static MsalRefreshTokenCacheItem GetRefreshToken(
           ILoggerAdapter logger,
           ILegacyCachePersistence legacyCachePersistence,
           IEnumerable<string> environmentAliases,
           string clientId,
           IAccount account)
        {
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(logger, legacyCachePersistence.LoadCache());

                IEnumerable<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase) &&
                        environmentAliases.Contains(new Uri(p.Key.Authority).Host));

                bool filtered = false;
                
                if (!string.IsNullOrEmpty(account?.Username))
                {
                    listToProcess =
                        listToProcess.Where(p => account.Username.Equals(
                            p.Key.DisplayableId, StringComparison.OrdinalIgnoreCase));                        
                    
                    filtered = true;
                }
                
                if (!string.IsNullOrEmpty(account?.HomeAccountId?.ObjectId))
                {
                    listToProcess =
                        listToProcess.Where(p => account.HomeAccountId.ObjectId.Equals(
                            p.Key.UniqueId, StringComparison.OrdinalIgnoreCase)).ToList();

                    filtered = true;
                }

                // We should filter at least by one criteria to ensure we return adequate RT
                if (!filtered)
                {
                    logger.Warning("Could not filter ADAL entries by either UPN or unique ID, skipping. ");
                    return null;
                }

                return listToProcess.Select(adalEntry => new MsalRefreshTokenCacheItem(
                      new Uri(adalEntry.Key.Authority).Host,
                      adalEntry.Key.ClientId,
                      adalEntry.Value.RefreshToken,
                      adalEntry.Value.RawClientInfo,
                      familyId: null,
                      homeAccountId: GetHomeAccountId(adalEntry.Value)))
                .FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.WarningPiiWithPrefix(ex, "An error occurred while searching for refresh tokens in ADAL format in the cache for MSAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ");

                return null;
            }
        }

        private static string GetHomeAccountId(AdalResultWrapper adalResultWrapper)
        {
            if (!string.IsNullOrEmpty(adalResultWrapper.RawClientInfo))
            {
                return ClientInfo.CreateFromJson(adalResultWrapper.RawClientInfo).ToAccountIdentifier();
            }

            return null;
        }
    }
}
