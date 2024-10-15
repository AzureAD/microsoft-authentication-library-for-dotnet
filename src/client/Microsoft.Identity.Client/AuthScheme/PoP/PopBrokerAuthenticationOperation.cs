// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    //Authentication Scheme used when MSAL Broker and pop are used together.
    //Tokens acquired from brokers will not be saved in the local ache and MSAL will not search the local cache during silent authentication.
    //This is because tokens are cached in the broker instead so MSAL will rely on the broker's cache for silent requests.
    internal class PopBrokerAuthenticationOperation : IAuthenticationOperation
    {
        public int TelemetryTokenType => (int)TokenType.Pop;

        public string AuthorizationHeaderPrefix => Constants.PoPAuthHeaderPrefix;

        public string KeyId => string.Empty;

        public string AccessTokenType => Constants.PoPTokenType;

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            //no-op
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return CollectionHelpers.GetEmptyDictionary<string, string>();
        }
    }
}
