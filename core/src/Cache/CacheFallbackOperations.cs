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
        public static void WriteMsalRefreshToken(AdalResultWrapper resultWrapper, string authority, string clientId, string displayableId, string identityProvider, string givenName)
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

            MsalRefreshTokenCacheItem rtItem = new MsalRefreshTokenCacheItem()
            {
                RefreshToken = resultWrapper.RefreshToken,
                ClientId = clientId,
                RawClientInfo = resultWrapper.RawClientInfo,
                Version = MsalTokenCacheItemBase.CacheVersion,
                DisplayableId = displayableId,
                IdentityProvider = identityProvider,
                Name = givenName,
                ClientInfo = ClientInfo.CreateFromEncodedString(resultWrapper.RawClientInfo)
            };

            ITokenCacheAccessor accessor = new TokenCacheAccessor();
            accessor.SaveRefreshToken(rtItem.GetRefreshTokenItemKey().ToString(), JsonHelper.SerializeToJson(rtItem));
        }

        public static void WriteAdalRefreshToken(MsalRefreshTokenCacheItem rtItem, string authority, string uniqueId, string scope)
        {
            if (rtItem == null)
            {
                CoreLoggerBase.Default.Info("rtItem is null. Skipping MSAL RT cache write");
                return;
            }

            //Using scope instead of resource becaue that value does not exist. STS should return it.
            AdalTokenCacheKey key = new AdalTokenCacheKey(authority, scope, rtItem.ClientId, TokenSubjectType.User, uniqueId, rtItem.DisplayableId);
            AdalResultWrapper wrapper = new AdalResultWrapper()
            {
                Result = new AdalResult(null,null, DateTimeOffset.MinValue),
                RefreshToken = rtItem.RefreshToken,
                RawClientInfo = rtItem.RawClientInfo,
                //ResourceInResponse is needed to treat RT as an MRRT. See IsMultipleResourceRefreshToken 
                //property in AdalResultWrapper and its usage. Stronger design would be for the STS to return resource
                //for which the token was issued as well on v2 endpoint.
                ResourceInResponse = scope
            };

#if !FACADE && !NETSTANDARD1_3
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(LegacyCachePersistance.LoadCache());
            dictionary[key] = wrapper;
            LegacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
#endif
        }


        public static List<MsalRefreshTokenCacheItem> GetAllAdalUsersForMsal(string environment, string clientId)
        {
            //returns all the adal entries where client info is present
            List<MsalRefreshTokenCacheItem> list = GetAllAdalEntriesForMsal(environment, clientId, null, null);
            //TODO return distinct clientinfo only
            return list.Where(p => !string.IsNullOrEmpty(p.RawClientInfo)).ToList();
        }

        public static List<MsalRefreshTokenCacheItem> GetAllAdalEntriesForMsal(string environment, string clientId, string upn, string rawClientInfo)
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(LegacyCachePersistance.LoadCache());
            //filter by client id and environment first
            //TODO - authority check needs to be updated for alias check
            List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> listToProcess =
                    dictionary.Where(p => p.Key.ClientId.Equals(clientId) && environment.Equals(new Uri(p.Key.Authority).Host)).ToList();

            //if client info is provided then use it to filter
            if (!string.IsNullOrEmpty(rawClientInfo))
            {
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> clientInfoEntries = listToProcess.Where(p => rawClientInfo.Equals(p.Value.RawClientInfo)).ToList();
                if (clientInfoEntries.Any())
                {
                    listToProcess = clientInfoEntries;
                }
            }

            //if upn is provided then use it to filter
            if (!string.IsNullOrEmpty(upn))
            {
                List<KeyValuePair<AdalTokenCacheKey, AdalResultWrapper>> upnEntries = listToProcess.Where(p => upn.Equals(p.Key.DisplayableId)).ToList();
                if (upnEntries.Any())
                {
                    listToProcess = upnEntries;
                }
            }

            List<MsalRefreshTokenCacheItem> list = new List<MsalRefreshTokenCacheItem>();
            foreach(KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> pair in listToProcess)
            {
                list.Add(
            new MsalRefreshTokenCacheItem()
            {
                RawClientInfo = pair.Value.RawClientInfo,
                RefreshToken = pair.Value.RefreshToken,
                DisplayableId = pair.Key.DisplayableId,
                ClientId = pair.Key.ClientId,
                Version = MsalTokenCacheItemBase.CacheVersion,
                Environment = environment,
                IdentityProvider = pair.Value.Result?.UserInfo.IdentityProvider,
                Name = pair.Value.Result?.UserInfo.GivenName,
                ClientInfo = ClientInfo.CreateFromEncodedString(pair.Value.RawClientInfo)
            });
            }

            return list;
        }

        public static MsalRefreshTokenCacheItem GetAdalEntryForMsal(string environment, string clientId, string upn, string rawClientInfo)
        {
            return GetAllAdalEntriesForMsal(environment, clientId, upn, rawClientInfo).FirstOrDefault();
        }

        public static AdalResultWrapper FindMsalEntryForAdal(string authority, string clientId, string upn)
        {
            ITokenCacheAccessor accessor = new TokenCacheAccessor();
            foreach(string rtString in accessor.GetAllRefreshTokensAsString())
            {
                MsalRefreshTokenCacheItem rtItem =
                    JsonHelper.DeserializeFromJson<MsalRefreshTokenCacheItem>(rtString);

                //TODO - authority check needs to be updated for alias check
                if (new Uri(authority).Host.Equals(rtItem.Environment) && rtItem.ClientId.Equals(clientId) && rtItem.DisplayableId.Equals(upn)) {
                    return new AdalResultWrapper()
                    {
                        Result = new AdalResult(null, null, DateTimeOffset.MinValue),
                        RefreshToken = rtItem.RefreshToken,
                        RawClientInfo = rtItem.RawClientInfo
                    };
                }
            }

            return null;
        }
    }
}
