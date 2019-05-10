// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.Identity.Client.Cache.Keys
{
    /// <summary>
    /// An object representing the key of the token cache Account dictionary. The
    /// format of the key is not important for this library, as long as it is unique.
    /// </summary>
    internal class MsalAccountCacheKey : IiOSKey
    {
        private readonly string _environment;
        private readonly string _homeAccountId;
        private readonly string _tenantId;
        private readonly string _username;
        private readonly string _authorityType;

        public MsalAccountCacheKey(string environment, string tenantId, string userIdentifier, string username, string authorityType)
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _tenantId = tenantId;
            _environment = environment;
            _homeAccountId = userIdentifier;
            _username = username;
            _authorityType = authorityType;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_homeAccountId + MsalCacheKeys.CacheKeyDelimiter);
            stringBuilder.Append(_environment + MsalCacheKeys.CacheKeyDelimiter);
            stringBuilder.Append(_tenantId);

            return stringBuilder.ToString();
        }

        #region iOS

        public string iOSAccount
        {
            get
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append(_homeAccountId ?? "");
                stringBuilder.Append(MsalCacheKeys.CacheKeyDelimiter);

                stringBuilder.Append(_environment);

                return stringBuilder.ToString().ToLowerInvariant();
            }
        }

        public string iOSGeneric => _username.ToLowerInvariant();

        public string iOSService => (_tenantId ?? "").ToLowerInvariant();

        public int iOSType => MsalCacheKeys.iOSAuthorityTypeToAttrType[_authorityType];


        #endregion
    }
}
