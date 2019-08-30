// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        public const string ExtendedExpiresOn = "extended_expires_on";
        public const string ClientInfo = "client_info";
        public const string FamilyId = "family_id";
        public const string AppMetadata = "appmetadata";


        // todo(cache): this needs to be added to the spec.  needed for OBO flow on .NET.
        public const string UserAssertionHash = "user_assertion_hash";

        // previous versions of msal used "ext_expires_on" instead of the correct "extended_expires_on".
        // this is here for back compat
        public const string ExtendedExpiresOn_MsalCompat = "ext_expires_on";
    }
}
