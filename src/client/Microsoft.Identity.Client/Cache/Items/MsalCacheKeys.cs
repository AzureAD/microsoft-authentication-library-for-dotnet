// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal partial class MsalCacheKeys
    {
        public const char CacheKeyDelimiter = '-';

        public static string GetCredentialKey(
            string homeAccountId, 
            string environment, 
            string keyDescriptor, 
            string clientId, 
            string tenantId, 
            string scopes, 
            params string[] additionalKeys /* for extensibility */)
        {
            return GetCredentialKey(false, homeAccountId, environment, keyDescriptor, clientId, tenantId, scopes, additionalKeys);
        }

        internal static string GetCredentialKey(
            bool isClientIdCaseSensitive,
            string homeAccountId,
            string environment,
            string keyDescriptor,
            string clientId,
            string tenantId,
            string scopes,
            params string[] additionalKeys)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(homeAccountId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(environment); // guaranteed not null
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(keyDescriptor); // a constant
            stringBuilder.Append(CacheKeyDelimiter);

            int clientIdOffset = stringBuilder.Length;
            stringBuilder.Append(clientId);   // guaranteed not null
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(scopes ?? "");

            foreach (var additionalKey in additionalKeys ?? Enumerable.Empty<string>())
            {
                stringBuilder.Append(CacheKeyDelimiter);
                stringBuilder.Append(additionalKey);
            }

            return NormalizeKey(stringBuilder, clientIdOffset, clientId, isClientIdCaseSensitive);
        }

        public static string GetiOSAccountKey(string homeAccountId, string environment)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(homeAccountId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(environment);

            return stringBuilder.ToString().ToLowerInvariant();
        }

        public static string GetiOSServiceKey(
            string keyDescriptor, 
            string clientId, 
            string tenantId, 
            string scopes,
            params string[] extraKeyParts)
        {
            return GetiOSServiceKey(false, keyDescriptor, clientId, tenantId, scopes, extraKeyParts);
        }

        internal static string GetiOSServiceKey(
            bool isClientIdCaseSensitive,
            string keyDescriptor,
            string clientId,
            string tenantId,
            string scopes,
            params string[] extraKeyParts)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(keyDescriptor);
            stringBuilder.Append(CacheKeyDelimiter);

            int clientIdOffset = stringBuilder.Length;
            stringBuilder.Append(clientId);
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(scopes ?? "");

            foreach (var additionalKey in extraKeyParts ?? Enumerable.Empty<string>())
            {
                stringBuilder.Append(CacheKeyDelimiter);
                stringBuilder.Append(additionalKey);
            }

            return NormalizeKey(stringBuilder, clientIdOffset, clientId, isClientIdCaseSensitive);
        }

        public static string GetiOSGenericKey(string keyDescriptor, string clientId, string tenantId)
        {
            return GetiOSGenericKey(false, keyDescriptor, clientId, tenantId);
        }

        internal static string GetiOSGenericKey(bool isClientIdCaseSensitive, string keyDescriptor, string clientId, string tenantId)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(keyDescriptor);
            stringBuilder.Append(CacheKeyDelimiter);

            int clientIdOffset = stringBuilder.Length;
            stringBuilder.Append(clientId);
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");

            return NormalizeKey(stringBuilder, clientIdOffset, clientId, isClientIdCaseSensitive);
        }

        private static string NormalizeKey(StringBuilder key, int clientIdOffset, string clientId, bool isClientIdCaseSensitive)
        {
            if (!isClientIdCaseSensitive || string.IsNullOrEmpty(clientId))
            {
                return key.ToString().ToLowerInvariant();
            }

            int suffixOffset = clientIdOffset + clientId.Length;
            return key.ToString(0, clientIdOffset).ToLowerInvariant() +
                clientId +
                key.ToString(suffixOffset, key.Length - suffixOffset).ToLowerInvariant();
        }

        #region iOS

        internal static readonly Dictionary<string, int> iOSAuthorityTypeToAttrType = new Dictionary<string, int>()
        {
            {CacheAuthorityType.AAD.ToString(), 1001},
            {CacheAuthorityType.MSA.ToString(), 1002},
            {CacheAuthorityType.MSSTS.ToString(), 1003},
            {CacheAuthorityType.OTHER.ToString(), 1004},
        };

        #endregion
    }
}
