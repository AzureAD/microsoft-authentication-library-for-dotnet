// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    internal class AccessTokenWithExpiration : IToken
    {
        public DateTimeOffset? ExpiresOn { get; set; }

        public string AccessToken { get; set; }
    }
}
