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

        internal string HomeAccountId => _homeAccountId;

        public MsalAccountCacheKey(string environment, string tenantId, string userIdentifier, string username)
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _tenantId = tenantId;
            _environment = environment;
            _homeAccountId = userIdentifier;
            _username = username;
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

        // This is a known issue.
        // Normally AuthorityType should be passed here but sice while building the MsalAccountCacheItem it is defaulted to "MSSTS",
        // keeping the default value here.
        public int iOSType => MsalCacheKeys.iOSAuthorityTypeToAttrType[CacheAuthorityType.MSSTS.ToString()];

        #endregion

        #region Equals and GetHashCode
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = obj as MsalAccountCacheKey;

            return string.Equals(
                ToString(),
                other.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        #endregion
    }
}
