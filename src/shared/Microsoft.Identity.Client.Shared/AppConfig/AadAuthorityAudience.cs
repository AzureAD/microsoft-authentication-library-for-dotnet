// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Specifies which Microsoft accounts can be used for sign-in with a given application.
    /// See https://aka.ms/msal-net-application-configuration
    /// </summary>
    public enum AadAuthorityAudience
    {
        /// <summary>
        /// The sign-in audience was not specified
        /// </summary>
        None,

        /// <summary>
        /// Users with a Microsoft work or school account in my organization’s Azure AD tenant (i.e. single tenant).
        /// Maps to https://[instance]/[tenantId]
        /// </summary>
        AzureAdMyOrg,

        /// <summary>
        /// Users with a personal Microsoft account, or a work or school account in any organization’s Azure AD tenant
        /// Maps to https://[instance]/common/
        /// </summary>
        AzureAdAndPersonalMicrosoftAccount,

        /// <summary>
        /// Users with a Microsoft work or school account in any organization’s Azure AD tenant (i.e. multi-tenant).
        /// Maps to https://[instance]/organizations/
        /// </summary>
        AzureAdMultipleOrgs,

        /// <summary>
        /// Users with a personal Microsoft account. Maps to https://[instance]/consumers/
        /// </summary>
        PersonalMicrosoftAccount
    }
}
