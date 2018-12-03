// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Schema
{
    /// <summary>
    /// This class contains the methods for encoding/decoding our object representations of cache data.
    /// If you're modifying this class, you're updating the schema persistence behavior so ensure you're
    /// aligned with the other cache schema models.
    /// </summary>
    internal static class StorageJsonUtils
    {
        public static JObject CredentialToJson(Credential credential)
        {
            var json = string.IsNullOrWhiteSpace(credential.AdditionalFieldsJson)
                           ? new JObject()
                           : JObject.Parse(credential.AdditionalFieldsJson);

            json[StorageJsonKeys.HomeAccountId] = credential.HomeAccountId;
            json[StorageJsonKeys.Environment] = credential.Environment;
            json[StorageJsonKeys.Realm] = credential.Realm;
            json[StorageJsonKeys.CredentialType] = CredentialTypeToString(credential.CredentialType);
            json[StorageJsonKeys.ClientId] = credential.ClientId;
            json[StorageJsonKeys.FamilyId] = credential.FamilyId;
            json[StorageJsonKeys.Target] = credential.Target;
            json[StorageJsonKeys.Secret] = credential.Secret;
            json[StorageJsonKeys.CachedAt] = credential.CachedAt.ToString(CultureInfo.InvariantCulture);
            json[StorageJsonKeys.ExpiresOn] = credential.ExpiresOn.ToString(CultureInfo.InvariantCulture);
            json[StorageJsonKeys.ExtendedExpiresOn] = credential.ExtendedExpiresOn.ToString(CultureInfo.InvariantCulture);

            return json;
        }

        private static string CredentialTypeToString(CredentialType credentialType)
        {
            switch (credentialType)
            {
            case CredentialType.OAuth2AccessToken:
                return StorageJsonValues.CredentialTypeAccessToken;
            case CredentialType.OAuth2RefreshToken:
                return StorageJsonValues.CredentialTypeRefreshToken;
            case CredentialType.OidcIdToken:
                return StorageJsonValues.CredentialTypeIdToken;
            default:
                return StorageJsonValues.CredentialTypeOther;
            }
        }

        public static Credential CredentialFromJson(JObject credentialJson)
        {
            var credential = Credential.CreateEmpty();
            credential.HomeAccountId = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.HomeAccountId);
            credential.Environment = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.Environment);
            credential.Realm = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.Realm);
            credential.CredentialType = CredentialTypeToEnum(
                JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.CredentialType));
            credential.ClientId = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.ClientId);
            credential.FamilyId = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.FamilyId);
            credential.Target = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.Target);
            credential.Secret = JsonUtils.ExtractExistingOrEmptyString(credentialJson, StorageJsonKeys.Secret);
            credential.CachedAt = JsonUtils.ExtractParsedIntOrZero(credentialJson, StorageJsonKeys.CachedAt);
            credential.ExpiresOn = JsonUtils.ExtractParsedIntOrZero(credentialJson, StorageJsonKeys.ExpiresOn);
            credential.ExtendedExpiresOn = JsonUtils.ExtractParsedIntOrZero(credentialJson, StorageJsonKeys.ExtendedExpiresOn);

            credential.AdditionalFieldsJson = credentialJson.ToString();

            return credential;
        }

        private static CredentialType CredentialTypeToEnum(string credentialTypeString)
        {
            if (string.Compare(
                    credentialTypeString,
                    StorageJsonValues.CredentialTypeAccessToken,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return CredentialType.OAuth2AccessToken;
            }

            if (string.Compare(
                    credentialTypeString,
                    StorageJsonValues.CredentialTypeRefreshToken,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return CredentialType.OAuth2RefreshToken;
            }

            if (string.Compare(
                    credentialTypeString,
                    StorageJsonValues.CredentialTypeIdToken,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return CredentialType.OidcIdToken;
            }

            return CredentialType.Other;
        }

        public static JObject AccountToJson(Account account)
        {
            var json = string.IsNullOrWhiteSpace(account.AdditionalFieldsJson)
                           ? new JObject()
                           : JObject.Parse(account.AdditionalFieldsJson);

            json[StorageJsonKeys.HomeAccountId] = account.HomeAccountId;
            json[StorageJsonKeys.Environment] = account.Environment;
            json[StorageJsonKeys.Realm] = account.Realm;
            json[StorageJsonKeys.LocalAccountId] = account.LocalAccountId;
            json[StorageJsonKeys.AuthorityType] = AuthorityTypeToString(account.AuthorityType);
            json[StorageJsonKeys.Username] = account.Username;
            json[StorageJsonKeys.GivenName] = account.GivenName;
            json[StorageJsonKeys.FamilyName] = account.FamilyName;
            json[StorageJsonKeys.MiddleName] = account.MiddleName;
            json[StorageJsonKeys.Name] = account.Name;
            json[StorageJsonKeys.AlternativeAccountId] = account.AlternativeAccountId;
            json[StorageJsonKeys.ClientInfo] = account.ClientInfo;

            return json;
        }

        private static string AuthorityTypeToString(AuthorityType authorityType)
        {
            switch (authorityType)
            {
            case AuthorityType.MsSts:
                return StorageJsonValues.AuthorityTypeMsSts;
            case AuthorityType.Adfs:
                return StorageJsonValues.AuthorityTypeAdfs;
            case AuthorityType.Msa:
                return StorageJsonValues.AuthorityTypeMsa;
            default:
                return StorageJsonValues.AuthorityTypeOther;
            }
        }

        public static Microsoft.Identity.Client.CacheV2.Schema.Account AccountFromJson(JObject accountJson)
        {
            var account = Account.CreateEmpty();

            account.HomeAccountId = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.Environment = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.Realm = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.LocalAccountId = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.AuthorityType =
                AuthorityTypeToEnum(JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId));
            account.Username = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.GivenName = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.FamilyName = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.MiddleName = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.Name = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.AlternativeAccountId = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);
            account.ClientInfo = JsonUtils.ExtractExistingOrEmptyString(accountJson, StorageJsonKeys.HomeAccountId);

            account.AdditionalFieldsJson = accountJson.ToString();

            return account;
        }

        private static AuthorityType AuthorityTypeToEnum(string authorityTypeString)
        {
            if (string.Compare(
                    authorityTypeString,
                    StorageJsonValues.AuthorityTypeMsSts,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AuthorityType.MsSts;
            }

            if (string.Compare(authorityTypeString, StorageJsonValues.AuthorityTypeAdfs, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AuthorityType.Adfs;
            }

            if (string.Compare(authorityTypeString, StorageJsonValues.AuthorityTypeMsa, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return AuthorityType.Msa;
            }

            return AuthorityType.Other;
        }

        public static JObject AppMetadataToJson(AppMetadata appMetadata)
        {
            var json = new JObject
            {
                [StorageJsonKeys.Environment] = appMetadata.Environment,
                [StorageJsonKeys.ClientId] = appMetadata.ClientId,
                [StorageJsonKeys.FamilyId] = appMetadata.FamilyId
            };

            return json;
        }

        public static AppMetadata AppMetadataFromJson(JObject appMetadataJson)
        {
            string environment = JsonUtils.GetExistingOrEmptyString(appMetadataJson, StorageJsonKeys.Environment);
            string clientId = JsonUtils.GetExistingOrEmptyString(appMetadataJson, StorageJsonKeys.ClientId);
            string familyId = JsonUtils.GetExistingOrEmptyString(appMetadataJson, StorageJsonKeys.FamilyId);

            return new AppMetadata(environment, clientId, familyId);
        }
    }
}