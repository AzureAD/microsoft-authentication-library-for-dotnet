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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Helpers;

namespace Microsoft.Identity.Core.Cache
{
    internal class CacheFallbackOperations
    {
        public static void WriteMsalRefreshToken(ITokenCacheAccessor tokenCacheAccessor,
            AdalResultWrapper resultWrapper, string authority, string clientId, string displayableId,
             string givenName, string familyName, string objectId)
        {
            if (string.IsNullOrEmpty(resultWrapper.RawClientInfo))
            {
                CoreLoggerBase.Default.Info("Client Info is missing. Skipping MSAL RT cache write");
                return;
            }

            if (string.IsNullOrEmpty(resultWrapper.RefreshToken))
            {
                CoreLoggerBase.Default.Info("Refresh Token is missing. Skipping MSAL RT cache write");
                return;
            }

            if (string.IsNullOrEmpty(resultWrapper.Result.IdToken))
            {
                CoreLoggerBase.Default.Info("Id Token is missing. Skipping MSAL RT cache write");
                return;
            }

            var rtItem = new MsalRefreshTokenCacheItem
                (new Uri(authority).Host, clientId, resultWrapper.RefreshToken, resultWrapper.RawClientInfo);

            tokenCacheAccessor.SaveRefreshToken(rtItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem
                (new Uri(authority).Host, objectId, resultWrapper.RawClientInfo, null, displayableId, resultWrapper.Result.TenantId);

            tokenCacheAccessor.SaveAccount(accountCacheItem);
        }

        public static void WriteAdalRefreshToken(ILegacyCachePersistance legacyCachePersistance, MsalRefreshTokenCacheItem rtItem, MsalIdTokenCacheItem idItem,
            string authority, string uniqueId, string scope)
        {
            if (rtItem == null)
            {
                CoreLoggerBase.Default.Info("rtItem is null. Skipping MSAL RT cache write");
                return;
            }

            //Using scope instead of resource becaue that value does not exist. STS should return it.
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

#if !FACADE && !NETSTANDARD1_3
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            dictionary[key] = wrapper;
            legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
#endif
        }

        public static Dictionary<String, AdalUserInfo> GetAllAdalUsersForMsal(ILegacyCachePersistance legacyCachePersistance,
            ISet<string> environments, string clientId)
        {
            Dictionary<String, AdalUserInfo> users = new Dictionary<String, AdalUserInfo>();
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
                        users.Add(pair.Value.RawClientInfo, pair.Value.Result.UserInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                CoreLoggerBase.Default.Warning("GetAllAdalUsersForMsal failed due to Exception - " + ex);
            }
            return users;
        }

        public static Dictionary<String, AdalUserInfo> RemoveAdalUser(ILegacyCachePersistance legacyCachePersistance,
            string displayableId, ISet<string> authorityHostAliases, string identifier)
        {
            Dictionary<String, AdalUserInfo> users = new Dictionary<String, AdalUserInfo>();
            try
            {
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary =
                    AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());

                List<AdalTokenCacheKey> keysToRemove = new List<AdalTokenCacheKey>();
                foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> pair in dictionary)
                {
                    if (authorityHostAliases.Contains(new Uri(pair.Key.Authority).Host) &&
                         displayableId.Equals(pair.Key.DisplayableId) &&
                         identifier.Equals(ClientInfo.CreateFromJson(pair.Value.RawClientInfo).ToUserIdentifier()))
                    {
                        keysToRemove.Add(pair.Key);
                    }
                }

                foreach (AdalTokenCacheKey key in keysToRemove)
                {
                    dictionary.Remove(key);
                }

                legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
            }
            catch (Exception ex)
            {
                CoreLoggerBase.Default.Warning("RemoveAdalUser failed due to Exception - " + ex);
            }
            return users;
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
                CoreLoggerBase.Default.Warning("GetAllAdalEntriesForMsal failed due to Exception - " + ex);

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
            return null;
        }
    }
}
