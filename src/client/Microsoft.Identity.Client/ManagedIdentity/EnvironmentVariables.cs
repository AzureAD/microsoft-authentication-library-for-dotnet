// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class EnvironmentVariables
    {
        public static string IdentityEndpoint => Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT");
        public static string IdentityHeader => Environment.GetEnvironmentVariable("IDENTITY_HEADER");
        public static string PodIdentityEndpoint => Environment.GetEnvironmentVariable("AZURE_POD_IDENTITY_AUTHORITY_HOST");
        public static string ImdsEndpoint => Environment.GetEnvironmentVariable("IMDS_ENDPOINT");
        public static string MsiEndpoint => Environment.GetEnvironmentVariable("MSI_ENDPOINT");
        public static string IdentityServerThumbprint => Environment.GetEnvironmentVariable("IDENTITY_SERVER_THUMBPRINT");
    }
}
