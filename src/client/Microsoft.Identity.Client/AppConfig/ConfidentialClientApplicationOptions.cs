// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Configuration options for a confidential client application
    /// (web app / web API / daemon app). See https://aka.ms/msal-net/application-configuration
    /// </summary>
    public class ConfidentialClientApplicationOptions : ApplicationOptions
    {
        /// <summary>
        /// Client secret for the confidential client application. This secret (application password)
        /// is provided by the application registration portal, or provided to Azure AD during the
        /// application registration with PowerShell AzureAD, PowerShell AzureRM, or Azure CLI.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Instructs MSAL to use an Azure regional token service using the region given.
        /// If the calling app knows the region it is deployed to, it should use this information. Region strings are available at https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.resourcemanager.fluent.core.region?view=azure-dotnet
        /// Otherwise, set the variable to "AutoDetect", and MSAL will attempt to auto-detect the region. This process
        /// works on a limited number of Azure artifacts (TBD - which ones!?). If auto-discovery fails, MSAL will use the non-regional service.
        /// </summary>
        public string AzureRegion { get; set; }
    }
}
