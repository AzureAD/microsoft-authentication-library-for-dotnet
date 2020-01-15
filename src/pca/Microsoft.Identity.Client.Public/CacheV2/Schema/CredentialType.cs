// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.CacheV2.Schema
{
    internal enum CredentialType
    {
        OAuth2AccessToken,
        OAuth2RefreshToken,
        OidcIdToken,
        Other
    }
}
