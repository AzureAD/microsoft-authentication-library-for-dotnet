// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Region;

namespace Microsoft.Identity.Client.Instance
{
    internal class AuthorityEndpoints
    {
        public AuthorityEndpoints(string authorizationEndpoint, string tokenEndpoint, string deviceCodeEndpoint)
        {
            AuthorizationEndpoint = authorizationEndpoint;
            TokenEndpoint = tokenEndpoint;
            SelfSignedJwtAudience = tokenEndpoint;
            DeviceCodeEndpoint = deviceCodeEndpoint;
        }

        public string AuthorizationEndpoint { get; }
        public string TokenEndpoint { get; }
        public string SelfSignedJwtAudience { get; }
        public string DeviceCodeEndpoint { get; }

    }
}
