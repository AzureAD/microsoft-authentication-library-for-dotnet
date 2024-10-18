// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Extensibility
{
    internal class ExternalBoundTokenScheme : IAuthenticationOperation
    {
        private readonly string _keyId;
        private readonly string _tokenType;

        public ExternalBoundTokenScheme(string keyId, string expectedTokenTypeFromEsts = "Bearer")
        {
            _keyId = keyId;
            _tokenType = expectedTokenTypeFromEsts;
        }

        public int TelemetryTokenType => (int)TokenType.External;

        public string AuthorizationHeaderPrefix => _tokenType;

        public string KeyId => _keyId;

        public string AccessTokenType => _tokenType;

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            // no-op
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            return CollectionHelpers.GetEmptyDictionary<string, string>();
        }
    }
}
