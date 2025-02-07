// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Cache
{
    internal static class StorageJsonKeys
    {
        public const string HomeAccountId = "home_account_id";
        public const string Environment = "environment";
        public const string Realm = "realm";
        public const string LocalAccountId = "local_account_id";
        public const string Username = "username";
        public const string AuthorityType = "authority_type";
        public const string AlternativeAccountId = "alternative_account_id";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string MiddleName = "middle_name";
        public const string Name = "name";
        public const string AvatarUrl = "avatar_url";
        public const string CredentialType = "credential_type";
        public const string ClientId = "client_id";
        public const string Secret = "secret";
        public const string Target = "target";
        public const string CachedAt = "cached_at";
        public const string ExpiresOn = "expires_on";
        public const string RefreshOn = "refresh_on";
        public const string ExtendedExpiresOn = "extended_expires_on";
        public const string ClientInfo = "client_info";
        public const string FamilyId = "family_id";
        public const string AppMetadata = "appmetadata";
        public const string KeyId = "kid";
        public const string TokenType = "token_type";
        public const string WamAccountIds = "wam_account_ids";
        public const string AccountSource = "account_source";

        // todo(cache): this needs to be added to the spec.  needed for OBO flow on .NET.
        public const string UserAssertionHash = "user_assertion_hash";

        // previous versions of MSAL used "ext_expires_on" instead of the correct "extended_expires_on".
        // this is here for back compatibility
        public const string ExtendedExpiresOn_MsalCompat = "ext_expires_on";

        public const string CacheExtensions = "ext";

        //Known storeage keys need to be added here
        public static readonly HashSet<string> s_knownStorageJsonKeys = new HashSet<string>
        {
            HomeAccountId, 
            Environment, 
            Realm, 
            LocalAccountId, 
            Username, 
            AuthorityType, 
            AlternativeAccountId,
            GivenName, 
            FamilyName, 
            MiddleName,
            Name, 
            AvatarUrl, 
            CredentialType,
            ClientId,
            Secret,
            Target,
            CachedAt, 
            ExpiresOn, 
            RefreshOn, 
            ExtendedExpiresOn, 
            ClientInfo,
            FamilyId,
            AppMetadata, 
            KeyId,
            TokenType, 
            WamAccountIds, 
            AccountSource,
            UserAssertionHash,
            ExtendedExpiresOn_MsalCompat, 
            CacheExtensions
        };

        public static bool IsKnownStorageJsonKey(string key)
        {
            return s_knownStorageJsonKeys.Contains(key);
        }
    }
}
