// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Cache.Items
{
    internal static class CacheSnapshotCloner
    {
        internal static MsalRefreshTokenCacheItem Clone(MsalRefreshTokenCacheItem item)
        {
            return new MsalRefreshTokenCacheItem(
                item.Environment ?? "", item.ClientId ?? "", item.Secret ?? "",
                item.RawClientInfo ?? "", item.FamilyId ?? "", item.HomeAccountId ?? "",
                item.AdditionalCacheKeyComponents is null ? null :
                    new SortedList<string, string>(item.AdditionalCacheKeyComponents))
            {
                OboCacheKey = item.OboCacheKey ?? "",
                CredentialType = item.CredentialType ?? "",
                AdditionalFieldsJson = item.AdditionalFieldsJson
            };
        }

        internal static MsalIdTokenCacheItem Clone(MsalIdTokenCacheItem item)
        {
            return new MsalIdTokenCacheItem(
                item.Environment ?? "", item.ClientId ?? "", item.Secret ?? "",
                item.RawClientInfo ?? "", item.HomeAccountId ?? "", item.TenantId ?? "")
            {
                CredentialType = item.CredentialType ?? "",
                AdditionalFieldsJson = item.AdditionalFieldsJson
            };
        }

        internal static MsalAccountCacheItem Clone(MsalAccountCacheItem item)
        {
            return new MsalAccountCacheItem(
                item.Environment ?? "", item.LocalAccountId ?? "", item.RawClientInfo ?? "",
                item.HomeAccountId ?? "", item.AccountSource ?? "", item.Name ?? "",
                item.PreferredUsername ?? "", item.TenantId ?? "", item.GivenName ?? "",
                item.FamilyName ?? "", item.WamAccountIds is null ? null :
                    new Dictionary<string, string>(item.WamAccountIds))
            {
                AuthorityType = item.AuthorityType ?? "",
                AdditionalFieldsJson = item.AdditionalFieldsJson
            };
        }

        internal static MsalAppMetadataCacheItem Clone(MsalAppMetadataCacheItem item)
        {
            return new MsalAppMetadataCacheItem(
                item.ClientId ?? "", item.Environment ?? "", item.FamilyId ?? "")
            {
                AdditionalFieldsJson = item.AdditionalFieldsJson
            };
        }
    }
}
