// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.AuthScheme.SSHCertificates;

namespace Microsoft.Identity.Client.AuthScheme
{
    internal class AuthSchemeHelper
    {
        /// <summary>
        /// For backwards compatibility reasons, keep the cache key unchanged for Bearer and SSH tokens. 
        /// For PoP and future tokens, the cache should support both several types of tokens for the same scope (e.g. PoP and Bearer)
        /// </summary>
        /// <param name="tokenType"></param>
        /// <returns></returns>
        public static bool StoreTokenTypeInCacheKey(string tokenType)
        {
            if (string.Equals(
                tokenType,
                BearerAuthenticationScheme.BearerTokenType,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(
                tokenType,
                SSHCertAuthenticationScheme.SSHCertTokenType,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
