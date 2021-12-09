// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Client.Extensibility
{
    internal class ExternalBoundTokenScheme : IAuthenticationScheme
    {
        private readonly string _keyId;

        public ExternalBoundTokenScheme(string keyId)
        {
            _keyId = keyId;
        }

        public string AuthorizationHeaderPrefix => "Bearer";

        public string KeyId => _keyId;

        public string AccessTokenType => "Bearer";

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            return msalAccessTokenCacheItem.Secret;
        }

        public IDictionary<string, string> GetTokenRequestParams()
        {
            return null;
        }
    }
}
