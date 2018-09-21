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
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core.Cache
{
    internal class CacheFallbackOperations
    {
        internal /* internal for testing only */ const string DifferentEnvError = 
            "Not expecting the RT and IdT to have different env when adding to legacy cache";
        internal /* internal for testing only */ const string DifferentAuthorityError = 
            "Not expecting authority to have a different env than the RT and IdT";

        public static void WriteMsalRefreshToken(ITokenCacheAccessor tokenCacheAccessor,
            AdalResultWrapper resultWrapper, string authority, string clientId, string displayableId,
             string givenName, string familyName, string objectId)
        {
            string msg;
            if (string.IsNullOrEmpty(resultWrapper.RawClientInfo))
            {
                msg = "Client Info is missing. Skipping MSAL refresh token cache write";
                CoreLoggerBase.Default.Info(msg);
                CoreLoggerBase.Default.InfoPii(msg);
                return;
            }

            if (string.IsNullOrEmpty(resultWrapper.RefreshToken))
            {
                msg = "Refresh Token is missing. Skipping MSAL refresh token cache write";
                CoreLoggerBase.Default.Info(msg);
                CoreLoggerBase.Default.InfoPii(msg);
                return;
            }

            if (string.IsNullOrEmpty(resultWrapper.Result.IdToken))
            {
                msg = "Id Token is missing. Skipping MSAL refresh token cache write";
                CoreLoggerBase.Default.Info(msg);
                CoreLoggerBase.Default.InfoPii(msg);
                return;
            }

            try
            {
                var rtItem = new MsalRefreshTokenCacheItem
                    (new Uri(authority).Host, clientId, resultWrapper.RefreshToken, resultWrapper.RawClientInfo);
                tokenCacheAccessor.SaveRefreshToken(rtItem);

                MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                    (new Uri(authority).Host, objectId, resultWrapper.RawClientInfo, null, displayableId, resultWrapper.Result.TenantId);
                tokenCacheAccessor.SaveAccount(accountCacheItem);
            }
            catch (Exception ex)
            {
                msg = "An error occurred while writing ADAL refresh token to the cache in MSAL format. " +
                      "For details please see https://aka.ms/net-cache-persistence-errors. ";
                string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);
                CoreLoggerBase.Default.Warning(msg + noPiiMsg);
                CoreLoggerBase.Default.WarningPii(msg + ex);
            }
        }

        public static void WriteAdalRefreshToken(
            ILegacyCachePersistance legacyCachePersistance,
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
                    string msg = "No refresh token available. Skipping MSAL refresh token cache write";
                    CoreLoggerBase.Default.Info(msg);
                    CoreLoggerBase.Default.InfoPii(msg);
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

                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
                dictionary[key] = wrapper;
                legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
            }
            catch (Exception ex)
            {
                if (!String.Equals(rtItem?.Environment, idItem?.Environment, StringComparison.OrdinalIgnoreCase))
                {
                    CoreLoggerBase.Default.Error(DifferentEnvError);
                    CoreLoggerBase.Default.ErrorPii(DifferentEnvError);
                }

                if (!String.Equals(rtItem?.Environment, (new Uri(authority)).Host, StringComparison.OrdinalIgnoreCase))
                {
                    CoreLoggerBase.Default.Error(DifferentAuthorityError);
                    CoreLoggerBase.Default.ErrorPii(DifferentAuthorityError);
                }

                string msg = "An error occurred while writing MSAL refresh token to the cache in ADAL format. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ";
                string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);
                CoreLoggerBase.Default.Warning(msg + noPiiMsg);
                CoreLoggerBase.Default.WarningPii(msg + ex);
            }
        }

        /// <summary>
        /// Returns a tuple where
        /// 
        /// Item1 is a map of ClientInfo -> AdalUserInfo for those users that have ClientInfo 
        /// Item2 is a list of AdalUserInfo for those users that do not have ClientInfo
        /// </summary>
        public static Tuple<Dictionary<string, AdalUserInfo>, List<AdalUserInfo>> GetAllAdalUsersForMsal
            (ILegacyCachePersistance legacyCachePersistance, ISet<string> environments, string clientId)
        {
            Dictionary<string, AdalUserInfo> clientInfoToAdalUserMap = new Dictionary<string, AdalUserInfo>();
            List<AdalUserInfo> adalUsersWithoutClientInfo = new List<AdalUserInfo>();
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
                //filter by client id and environment first
                //TODO - authority check needs to be updated for alias check
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p =>
                        p.Key.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)
                        && environments.Contains(new Uri(p.Key.Authority).Host)).ToList();

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
                string msg = "An error occurred while reading accounts in ADAL format from the cache for MSAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ";
                string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);
                CoreLoggerBase.Default.Warning(msg + noPiiMsg);
                CoreLoggerBase.Default.WarningPii(msg + ex);

                return Tuple.Create(new Dictionary<string, AdalUserInfo>(), new List<AdalUserInfo>());
            }
            return Tuple.Create(clientInfoToAdalUserMap, adalUsersWithoutClientInfo);
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
            ILegacyCachePersistance legacyCachePersistance,
            ISet<string> environmentAliases,
            string clientId,
            string displayableId,
            string accountOrUserId)
        {
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache =
                AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());

                if (!string.IsNullOrEmpty(accountOrUserId))
                {
                    RemoveEntriesWithMatchingId(environmentAliases, clientId, accountOrUserId, adalCache);
                }
                else
                {
                    RemoveEntriesWithMatchingName(environmentAliases, clientId, displayableId, adalCache);
                }

                legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(adalCache));
            }
            catch (Exception ex)
            {
                string msg = "An error occurred while deleting account in ADAL format from the cache. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ";
                string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);
                CoreLoggerBase.Default.Warning(msg + noPiiMsg);
                CoreLoggerBase.Default.WarningPii(msg + ex);
            }
        }

        private static void RemoveEntriesWithMatchingName(
            ISet<string> environmentAliases,
            string clientId,
            string displayableId,
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache)
        {
            if (String.IsNullOrEmpty(displayableId))
            {
                CoreLoggerBase.Default.Error(CoreErrorMessages.InternalErrorCacheEmptyUsername);
                CoreLoggerBase.Default.ErrorPii(CoreErrorMessages.InternalErrorCacheEmptyUsername);
                return;
            }

            List<AdalTokenCacheKey> keysToRemove = new List<AdalTokenCacheKey>();

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
                string cachedEnvironment = new Uri(kvp.Key.Authority).Host;
                string cachedAcccountDisplayableId = kvp.Key.DisplayableId;
                string cachedClientId = kvp.Key.ClientId;

                if (environmentAliases.Contains(cachedEnvironment, StringComparer.OrdinalIgnoreCase) &&
                    string.Equals(displayableId, cachedAcccountDisplayableId, StringComparison.OrdinalIgnoreCase) &&
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
            ISet<string> environmentAliases,
            string clientId,
            string accountOrUserId,
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache)
        {
            List<AdalTokenCacheKey> keysToRemove = new List<AdalTokenCacheKey>();

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
                string rawClientInfo = kvp.Value.RawClientInfo;

                if (!String.IsNullOrEmpty(rawClientInfo))
                {
                    string cachedAccountId = ClientInfo.CreateFromJson(rawClientInfo).ToAccountIdentifier();
                    string cachedEnvironment = new Uri(kvp.Key.Authority).Host;
                    string cachedClientId = kvp.Key.ClientId;

                    if (environmentAliases.Contains(cachedEnvironment, StringComparer.OrdinalIgnoreCase) &&
                        string.Equals(accountOrUserId, cachedAccountId, StringComparison.OrdinalIgnoreCase) &&
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

        public static List<MsalRefreshTokenCacheItem> GetAllAdalEntriesForMsal(ILegacyCachePersistance legacyCachePersistance,
            ISet<string> environmentAliases, string clientId, string upn, string uniqueId, string rawClientInfo)
        {
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
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
                string msg = "An error occurred while searching for refresh tokens in ADAL format in the cache for MSAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ";
                string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);
                CoreLoggerBase.Default.Warning(msg + noPiiMsg);
                CoreLoggerBase.Default.WarningPii(msg + ex);

                return new List<MsalRefreshTokenCacheItem>();
            }
        }

        public static MsalRefreshTokenCacheItem GetAdalEntryForMsal(ILegacyCachePersistance legacyCachePersistance,
            string preferredEnvironment, ISet<string> environmentAliases, string clientId, string upn, string uniqueId, string rawClientInfo)
        {
            var adalRts = GetAllAdalEntriesForMsal(legacyCachePersistance, environmentAliases, clientId, upn, uniqueId, rawClientInfo);

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

        public static AdalResultWrapper FindMsalEntryForAdal(ITokenCacheAccessor tokenCacheAccessor, string authority,
            string clientId, string upn, RequestContext requestContext)
        {
            try
            {
                var environment = new Uri(authority).Host;

                List<MsalAccountCacheItem> accounts = new List<MsalAccountCacheItem>();
                foreach (string accountStr in tokenCacheAccessor.GetAllAccountsAsString())
                {
                    var accountItem = JsonHelper.TryToDeserializeFromJson<MsalAccountCacheItem>(accountStr, requestContext);
                    if (accountItem != null && accountItem.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
                    {
                        accounts.Add(accountItem);
                    }
                }
                if (accounts.Count > 0)
                {
                    foreach (var rtString in tokenCacheAccessor.GetAllRefreshTokensAsString())
                    {
                        var rtCacheItem =
                            JsonHelper.TryToDeserializeFromJson<MsalRefreshTokenCacheItem>(rtString, requestContext);

                        //TODO - authority check needs to be updated for alias check
                        if (rtCacheItem != null && environment.Equals(rtCacheItem.Environment, StringComparison.OrdinalIgnoreCase)
                            && rtCacheItem.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase))
                        {
                            // join refresh token cache item to corresponding account cache item to get upn
                            foreach (MsalAccountCacheItem accountCacheItem in accounts)
                            {
                                if (rtCacheItem.HomeAccountId.Equals(accountCacheItem.HomeAccountId, StringComparison.OrdinalIgnoreCase)
                                    && accountCacheItem.PreferredUsername.Equals(upn, StringComparison.OrdinalIgnoreCase))
                                {
                                    return new AdalResultWrapper
                                    {
                                        Result = new AdalResult(null, null, DateTimeOffset.MinValue),
                                        RefreshToken = rtCacheItem.Secret,
                                        RawClientInfo = rtCacheItem.RawClientInfo
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = "An error occurred while searching for refresh tokens in MSAL format in the cache for ADAL. " +
                             "For details please see https://aka.ms/net-cache-persistence-errors. ";
                string noPiiMsg = CoreExceptionFactory.Instance.GetPiiScrubbedDetails(ex);

                CoreLoggerBase.Default.Warning(msg + noPiiMsg);
                CoreLoggerBase.Default.WarningPii(msg + ex);
            }

            return null;
        }
    }
}
