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
        /// Instructs MSAL.NET to use an Azure regional token service.
        /// This setting should be set to either the string with the region (preferred) or to 
        /// "TryAutoDetect" and MSAL.NET will attempt to auto-detect the region. 
        /// </summary>
        /// <remarks>
        /// Region names as per https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.resourcemanager.fluent.core.region?view=azure-dotnet.
        /// Not all auth flows can use the regional token service. 
        /// Service To Service (client credential flow) tokens can be obtained from the regional service.
        /// Requires configuration at the tenant level.
        /// Auto-detection works on a limited number of Azure artifacts (VMs, Azure functions). 
        /// If auto-detection fails, the non-regional endpoint will be used.
        /// If an invalid region name is provided, the non-regional endpoint MIGHT be used or the token request MIGHT fail.
        /// See https://aka.ms/msal-net-region-discovery for more details.        
        /// </remarks>
        public string AzureRegion { get; set; }

        /// <summary>
        /// When set to <c>true</c>, MSAL will lock cache access at the <see cref="ConfidentialClientApplication"/> level, i.e.
        /// the block of code between BeforeAccessAsync and AfterAccessAsync callbacks will be synchronized. 
        /// Apps can set this flag to <c>false</c> to enable an optimistic cache locking strategy, which may result in better performance, especially 
        /// when ConfidentialClientApplication objects are reused.
        /// </summary>
        /// <remarks>
        /// False by default.
        /// Not recommended for apps that call RemoveAsync
        /// </remarks>
        public bool EnableCacheSynchronization { get; set; } = false;
    }
}
