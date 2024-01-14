// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Enum to represent the type of MSAL application.
    /// </summary>
    internal enum MsalClientType
    {
        ConfidentialClient,
        PublicClient,
        ManagedIdentityClient
    }
}
