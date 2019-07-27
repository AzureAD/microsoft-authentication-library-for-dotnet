// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    internal static class MockTestConstants
    {
        public const string AccessTokenRaw = ".eyJBY2Nlc3NUb2tlbiJ9.";
        public const string RefreshTokenRaw = "eyJSZWZyZXNoVG9rZW4ifQ";
        public const string Authority = "https://localhost.com/common";
        public const string Environment = "localhost.com";
        public const string ClientId = "d15eased-c0de-abad-d00d-feed5badf00d";
        public const string FamilyId = "1";
        public const string CloudAudienceUrn = "urn:federation:LocalHost";
        public const string CorrelationId = "d77051e7-4ebb-4afa-9424-f55bd838f0bb";
        public const string DomainName = "localhost.com";

        public const string Password
            = // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake Password for Test")]
            "fakePassword"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake Password for Test")]

        public const string Realm = "common";
        public const string Redirect = "http://redirect";
        public const string MiddleName = "Charles";
        public const string Name = "Joe Charles Doe II";
        public const string AlternativeAccountId = "test_alternative_account_id";
        public const string Scopes = "scope1 scope2";
        public const Client.CacheV2.Schema.CacheV2AuthorityType DefaultAuthorityType = Client.CacheV2.Schema.CacheV2AuthorityType.Adfs;
        public const long ExpiresIn = 3599;
        public const long ExtendedExpiresIn = 0;
        public const long CachedAt = (long)1 << 40;
        public const long ExpiresOn = (long)1 << 41;
        public const long ExtendedExpiresOn = (long)1 << 42;
        public static string AdditionalFieldsJson = "{'my_name': 'Alex', 'age': 30, 'has_a_cat': true}";
        public static string ClientInfoJson = "{'uid': 'test_uid', 'utid': 'test_utid'}";
        public static string IdTokenJson = "{'given_name': 'Joe_id_token', 'family_name': 'Doe_id_token'}";

        public static string GetHomeAccountId()
        {
            return "test_uid.test_utid";
        }

        public static Client.CacheV2.Schema.Credential GetAccessToken()
        {
            return Client.CacheV2.Schema.Credential.CreateAccessToken(
                GetHomeAccountId(),
                Environment,
                Realm,
                ClientId,
                GetTarget(),
                CachedAt,
                ExpiresOn,
                ExtendedExpiresOn,
                AccessTokenRaw,
                AdditionalFieldsJson);
        }

        public static string GetTarget()
        {
            return "scope1 scope2";
        }

        public static Client.CacheV2.Schema.Credential GetRefreshToken()
        {
            return Client.CacheV2.Schema.Credential.CreateRefreshToken(
                GetHomeAccountId(),
                Environment,
                ClientId,
                CachedAt,
                RefreshTokenRaw,
                AdditionalFieldsJson);
        }

        public static Client.CacheV2.Schema.Credential GetFamilyRefreshToken()
        {
            return Client.CacheV2.Schema.Credential.CreateFamilyRefreshToken(
                GetHomeAccountId(),
                Environment,
                ClientId,
                FamilyId,
                CachedAt,
                RefreshTokenRaw,
                AdditionalFieldsJson);
        }

        public static Client.CacheV2.Schema.Credential GetIdToken()
        {
            return Client.CacheV2.Schema.Credential.CreateIdToken(
                GetHomeAccountId(),
                Environment,
                Realm,
                ClientId,
                CachedAt,
                GetRawIdToken(),
                AdditionalFieldsJson);
        }

        private static string GetRawIdToken()
        {
            return string.Format(CultureInfo.InvariantCulture, ".%s.", Base64UrlEncodeUnpadded(IdTokenJson));
        }

        private static string Base64UrlEncodeUnpadded(string input)
        {
            return Base64UrlHelpers.Encode(input);
        }

        public static Client.CacheV2.Schema.Account GetAccount()
        {
            return Client.CacheV2.Schema.Account.Create(
                GetHomeAccountId(),
                Environment,
                Realm,
                TestConstants.LocalAccountId,
                DefaultAuthorityType,
                TestConstants.Username,
                TestConstants.GivenName,
                TestConstants.FamilyName,
                MiddleName,
                Name,
                AlternativeAccountId,
                GetClientInfo(),
                AdditionalFieldsJson);
        }

        private static string GetClientInfo()
        {
            return Base64UrlEncodeUnpadded(ClientInfoJson);
        }

        public static Client.CacheV2.Schema.AppMetadata GetAppMetadata()
        {
            return new Client.CacheV2.Schema.AppMetadata(Environment, ClientId, FamilyId);
        }
    }
}
