// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.Bearer
{
    internal class BearerAuthenticationScheme : IAuthenticationScheme
    {
        internal const string BearerTokenType = "bearer";

        public TokenType TelemetryTokenType => TokenType.Bearer;

        public string AuthorizationHeaderPrefix => "Bearer";

        public string AccessTokenType => BearerTokenType;

        public string KeyId => null;

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            return msalAccessTokenCacheItem.Secret;
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            // ESTS issues Bearer tokens by default, no need for any extra params
            return CollectionHelpers.GetEmptyDictionary<string, string>();
        }
    }
}
