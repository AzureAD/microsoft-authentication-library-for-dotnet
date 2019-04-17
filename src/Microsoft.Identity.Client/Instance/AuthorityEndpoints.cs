// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Instance
{
    internal class AuthorityEndpoints
    {
        public AuthorityEndpoints(string authorizationEndpoint, string tokenEndpoint, string selfSignedJwtAudience)
        {
            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
            SelfSignedJwtAudience = selfSignedJwtAudience;
        }

        public string AuthorizationEndpoint { get; }
        public string TokenEndpoint { get; }
        public string SelfSignedJwtAudience { get; }
    }
}
