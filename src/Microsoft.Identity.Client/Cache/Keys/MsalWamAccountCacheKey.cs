// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal class MsalWamAccountCacheKey
    {
        private readonly string _environment;
        private readonly string _wamAccountId;

        public MsalWamAccountCacheKey(string environment, string wamAccountId)
        {
            if (string.IsNullOrEmpty(environment))
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (string.IsNullOrEmpty(wamAccountId))
            {
                throw new ArgumentNullException(nameof(wamAccountId));
            }

            _environment = environment;
            _wamAccountId = wamAccountId;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_environment + MsalCacheKeys.CacheKeyDelimiter);
            stringBuilder.Append(_wamAccountId + MsalCacheKeys.CacheKeyDelimiter);
            
            return stringBuilder.ToString();
        }

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
