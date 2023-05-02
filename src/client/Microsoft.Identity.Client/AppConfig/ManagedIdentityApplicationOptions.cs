// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Configuration options for a managed identity application.
    /// See https://aka.ms/msal-net/application-configuration
    /// </summary>
    public class ManagedIdentityApplicationOptions : BaseApplicationOptions
    {
        /// <summary>
        /// Configuration of the managed identity assigned to the azure resource.
        /// </summary>
        public ManagedIdentityConfiguration ManagedIdentityConfiguration { get; set; }
    }
}
