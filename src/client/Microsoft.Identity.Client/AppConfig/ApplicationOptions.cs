// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for options objects with string values loadable from a configuration file
    /// (for instance a JSON file, as in an asp.net configuration scenario)
    /// See https://aka.ms/msal-net-application-configuration
    /// See also derived classes <see cref="PublicClientApplicationOptions"/>
    /// and <see cref="ConfidentialClientApplicationOptions"/>
    /// </summary>
    public abstract class ApplicationOptions
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
        /// Enables you to configure the level of logging you want. The default value is <see cref="LogLevel.Info"/>. Setting it to <see cref="LogLevel.Error"/> will only get errors
        /// Setting it to <see cref="LogLevel.Warning"/> will get errors and warning, etc..
        /// See https://aka.ms/msal-net-logging
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Flag to enable/disable logging of Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR. You can set
        /// it to <c>true</c> for advanced debugging requiring PII. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <seealso cref="IsDefaultPlatformLoggingEnabled"/>
        public bool EnablePiiLogging { get; set; }

        /// <summary>
        /// Flag to enable/disable logging to platform defaults. In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In Android, logcat is used. The default value is <c>false</c>. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <seealso cref="EnablePiiLogging"/>
        public bool IsDefaultPlatformLoggingEnabled { get; set; }

        /// <summary>
        /// Identifier of the component (libraries/SDK) consuming MSAL.NET.
        /// This will allow for disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        [Obsolete("Should use ClientName and ClientVersion properties instead of Component", true)]
        public string Component { get; set; }

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
        /// Enables ADAL cache serialialization and deserialization.
        /// </summary>
        public bool AdalCacheCompatibilityEnabled { get; set; }
    }
}
