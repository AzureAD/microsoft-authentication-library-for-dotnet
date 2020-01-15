// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.CacheV2.Schema
{
    /// <summary>
    /// This is the object we will serialize (using StorageJson* classes for specific field names) for Account information.
    /// If you're modifying this object and the related (de)serialization, you're modifying the cache persistence
    /// model and need to ensure it's compatible and compliant with the other cache implementations.
    /// </summary>
    internal class Account : IAccount
    {
        public string HomeAccountId { get; set; }
        public string Environment { get; set; }
        public string Realm { get; set; }
        public string LocalAccountId { get; set; }
        public CacheV2AuthorityType AuthorityType { get; set; }
        public string Username { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string MiddleName { get; set; }
        public string Name { get; set; }
        public string AlternativeAccountId { get; set; }
        public string ClientInfo { get; set; }
        public string AdditionalFieldsJson { get; set; }

        string IAccount.Username => Username;
        string IAccount.Environment => Environment;

        AccountId IAccount.HomeAccountId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(HomeAccountId))
                {
                    return null;
                }
                var parts = HomeAccountId.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    return null;
                }

                return new AccountId(HomeAccountId, parts[0], parts[1]);
            }
        }

        public static Account CreateEmpty()
        {
            return new Account();
        }

        public static Account Create(
            string homeAccountId,
            string environment,
            string realm,
            string localAccountId,
            CacheV2AuthorityType authorityType,
            string userName,
            string givenName,
            string familyName,
            string middleName,
            string name,
            string alternativeAccountId,
            string clientInfo,
            string additionalFieldsJson)
        {
            return new Account
            {
                HomeAccountId = homeAccountId,
                Environment = environment,
                Realm = realm,
                LocalAccountId = localAccountId,
                AuthorityType = authorityType,
                Username = userName,
                GivenName = givenName,
                FamilyName = familyName,
                MiddleName = middleName,
                Name = name,
                AlternativeAccountId = alternativeAccountId,
                ClientInfo = clientInfo,
                AdditionalFieldsJson = additionalFieldsJson
            };
        }
    }
}
