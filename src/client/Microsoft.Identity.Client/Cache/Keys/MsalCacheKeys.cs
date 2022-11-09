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
        public const string CacheKeyDelimiter = "-";

        public static string GetCredentialKey(
            string homeAccountId, 
            string environment, 
            string keyDescriptor, 
            string clientId, 
            string tenantId, 
            string scopes, 
            params string[] additionalKeys /* for extensibility */)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(homeAccountId ?? "");
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(environment); // guaranteed not null
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(keyDescriptor); // a constant
            stringBuilder.Append(CacheKeyDelimiter);

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

            return stringBuilder.ToString().ToLowerInvariant();
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
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(keyDescriptor);
            stringBuilder.Append(CacheKeyDelimiter);

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

            return stringBuilder.ToString().ToLowerInvariant();
        }

        public static string GetiOSGenericKey(string keyDescriptor, string clientId, string tenantId)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(keyDescriptor);
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(clientId);
            stringBuilder.Append(CacheKeyDelimiter);

            stringBuilder.Append(tenantId ?? "");

            return stringBuilder.ToString().ToLowerInvariant();
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
