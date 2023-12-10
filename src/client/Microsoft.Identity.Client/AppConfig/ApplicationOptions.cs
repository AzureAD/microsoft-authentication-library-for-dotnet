// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Identity.Client.Kerberos;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for options objects with string values loadable from a configuration file
    /// (for instance a JSON file, as in an asp.net configuration scenario)
    /// See https://aka.ms/msal-net-application-configuration
    /// See also derived classes <see cref="PublicClientApplicationOptions"/>
    /// and <see cref="ConfidentialClientApplicationOptions"/>
    /// </summary>
    public abstract class ApplicationOptions : BaseApplicationOptions
    {
        /// <summary>
        /// Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Tenant from which the application will allow users to sign it. This can be:
        /// a domain associated with a tenant, a GUID (tenant id), or a meta-tenant (e.g. consumers).
        /// This property is mutually exclusive with <see cref="AadAuthorityAudience"/>. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        /// <remarks>The name of the property was chosen to ensure compatibility with AzureAdOptions
        /// in ASP.NET Core configuration files (even the semantics would be tenant)</remarks>
        public string TenantId { get; set; }

        /// <summary>
        /// Sign-in audience. This property is mutually exclusive with TenantId. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        public AadAuthorityAudience AadAuthorityAudience { get; set; } = AadAuthorityAudience.None;

        /// <summary>
        /// STS instance (for instance https://login.microsoftonline.com for the Azure public cloud).
        /// The name was chosen to ensure compatibility with AzureAdOptions in ASP.NET Core.
        /// This property is mutually exclusive with <see cref="AzureCloudInstance"/>. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Specific instance in the case of Azure Active Directory.
        /// It allows users to use the enum instead of the explicit URL.
        /// This property is mutually exclusive with <see cref="Instance"/>. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        public AzureCloudInstance AzureCloudInstance { get; set; } = AzureCloudInstance.None;

        /// <summary>
        /// This redirect URI needs to be registered in the app registration. See https://aka.ms/msal-net-register-app for
        /// details on which redirect URIs are defined by default by MSAL.NET and how to register them.
        /// Also use: <see cref="PublicClientApplicationBuilder.WithDefaultRedirectUri"/> which provides
        /// a good default for public client applications for all platforms.
        ///
        /// For web apps and web APIs, the redirect URI is computed from the URL where the application is running
        /// (for instance, <c>baseUrl//signin-oidc</c> for ASP.NET Core web apps).
        ///
        /// For daemon applications (confidential client applications using only the Client Credential flow
        /// that is calling <c>AcquireTokenForClient</c>), no reply URI is needed.
        /// </summary>
        /// <remarks>This is especially important when you deploy an application that you have initially tested locally;
        /// you then need to add the reply URL of the deployed application in the application registration portal
        /// </remarks>
        public string RedirectUri { get; set; }

        /// <summary>
        /// The name of the calling application for telemetry purposes.
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// The version of the calling application for telemetry purposes.
        /// </summary>
        public string ClientVersion { get; set; }

        /// <summary>
        /// Microsoft Identity specific OIDC extension that allows resource challenges to be resolved without interaction. 
        /// Allows configuration of one or more client capabilities, e.g. "llt"
        /// </summary>
        /// <remarks>
        /// MSAL will transform these into special claims request. See https://openid.net/specs/openid-connect-core-1_0-final.html#ClaimsParameter for
        /// details on claim requests.
        /// For more details see https://aka.ms/msal-net-claims-request
        /// </remarks>
        public IEnumerable<string> ClientCapabilities { get; set; }

        /// <summary>
        /// Enables legacy ADAL cache serialization and deserialization.
        /// </summary>
        public bool LegacyCacheCompatibilityEnabled { get; set; } = true;

        /// <summary>
        /// Service principal name for Kerberos Service Ticket.
        /// </summary>
        public string KerberosServicePrincipalName { get; set; } = string.Empty;

        /// <summary>
        /// Kerberos Service Ticket container to be used.
        /// </summary>
        public KerberosTicketContainer TicketContainer { get; set; } = KerberosTicketContainer.IdToken;
    }
}
