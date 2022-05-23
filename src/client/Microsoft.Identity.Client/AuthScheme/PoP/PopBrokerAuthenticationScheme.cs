// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    //Authentication Scheme used when MSAL Broker and pop are used together.
    internal class PopBrokerAuthenticationScheme : IAuthenticationScheme
    {
        public string AuthorizationHeaderPrefix => Constants.PoPAuthHeaderPrefix;

        public string KeyId => string.Empty;

        public string AccessTokenType => Constants.PoPTokenType;

        public string FormatAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            //no op
            return msalAccessTokenCacheItem.Secret;
        }

        public IDictionary<string, string> GetTokenRequestParams()
        {
            return new Dictionary<string, string>();
        }
    }
}
