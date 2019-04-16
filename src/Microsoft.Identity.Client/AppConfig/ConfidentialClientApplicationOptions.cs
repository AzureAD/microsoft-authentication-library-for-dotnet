// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Configuration options for a confidential client application
    /// (Web app / Web API / daemon app). See https://aka.ms/msal-net/application-configuration
    /// </summary>
    public class ConfidentialClientApplicationOptions : ApplicationOptions
    {
        /// <summary>
        /// Client secret for the confidential client application. This secret (application password)
        /// is provided by the application registration portal, or provided to Azure AD during the
        /// application registration with PowerShell AzureAD, PowerShell AzureRM, or Azure CLI.
        /// </summary>
        public string ClientSecret { get; set; }
    }
}
