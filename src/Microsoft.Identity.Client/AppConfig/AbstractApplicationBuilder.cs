// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractApplicationBuilder<T>
        where T : AbstractApplicationBuilder<T>
    {
        internal AbstractApplicationBuilder(ApplicationConfiguration configuration)
        {
            Config = configuration;
        }

        internal ApplicationConfiguration Config { get; }

        /// <summary>
        /// Uses a specific <see cref="IMsalHttpClientFactory"/> to communicate
        /// with the IdP. This enables advanced scenarios such as setting a proxy,
        /// or setting the Agent.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithHttpClientFactory(IMsalHttpClientFactory httpClientFactory)
        {
            Config.HttpClientFactory = httpClientFactory;
            return (T)this;
        }

        internal T WithHttpManager(IHttpManager httpManager)
        {
            Config.HttpManager = httpManager;
            return (T)this;
        }

        /// <summary>
        /// Sets the logging callback. For details see https://aka.ms/msal-net-logging
        /// </summary>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="InvalidOperationException"/> is thrown if the loggingCallback
        /// was already set on the application builder
        public T WithLoggingCallback(LogCallback loggingCallback)
        {
            if (Config.LoggingCallback != null)
            {
                throw new InvalidOperationException(CoreErrorMessages.LoggingCallbackAlreadySet);
            }

            Config.LoggingCallback = loggingCallback;
            return (T)this;
        }

        /// <summary>
        /// Sets the Debug logging callback to a default debug method which displays
        /// the level of the message and the message itself. For details see https://aka.ms/msal-net-logging
        /// </summary>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="InvalidOperationException"/> is thrown if the loggingCallback
        /// was already set on the application builder by calling <see cref="WithLoggingCallback(LogCallback)"/>
        /// <seealso cref="WithLoggingCallback(LogCallback)"/>
        /// <seealso cref="WithLoggingLevel(LogLevel)"/>
        public T WithDebugLoggingCallback()
        {
            if (Config.LoggingCallback != null)
            {
                throw new InvalidOperationException(CoreErrorMessages.LoggingCallbackAlreadySet);
            }

            Config.LoggingCallback = (level, message, pii) => { Debug.WriteLine($"{level}: {message}"); };
            return (T)this;
        }

        /// <summary>
        /// Sets the telemetry callback. For details see https://aka.ms/msal-net-telemetry
        /// </summary>
        /// <param name="telemetryCallback">Delegate to the callback sending the telemetry
        /// elaborated by the library to the telemetry endpoint of choice</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="InvalidOperationException"/> is thrown if the method was already
        /// called on the application builder.

        public T WithTelemetryCallback(TelemetryCallback telemetryCallback)
        {
            if (Config.TelemetryCallback  != null)
            {
                throw new InvalidOperationException(CoreErrorMessages.TelemetryCallbackAlreadySet);
            }

            Config.TelemetryCallback = telemetryCallback;
            return (T)this;
        }

        /// <summary>
        /// Sets the Client ID of the application
        /// </summary>
        /// <param name="clientId">Client ID (also known as <i>Application ID</i>) of the application as registered in the
        ///  application registration portal (https://aka.ms/msal-net-register-app)</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithClientId(string clientId)
        {
            Config.ClientId = clientId;
            return (T)this;
        }

        /// <summary>
        /// Sets the redirect URI of the application. See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="redirectUri">URL where the STS will call back the application with the security token.
        /// This parameter is not required for desktop or UWP applications (as a default is used).
        /// It's not required for mobile applications that don't use a broker
        /// It is required for Web Apps</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithRedirectUri(string redirectUri)
        {
            Config.RedirectUri = GetValueIfNotEmpty(Config.RedirectUri, redirectUri);
            return (T)this;
        }

        /// <summary>
        /// Sets the Tenant Id of the organization from which the application will let
        /// users sign-in. This is classically a GUID or a domain name. See https://aka.ms/msal-net-application-configuration.
        /// Although it is also possible to set <paramref name="tenantId"/> to <c>common</c>,
        /// <c>organizations</c>, and <c>consumers</c>, it's recommended to use one of the
        /// overrides of <see cref="WithAadAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>
        /// </summary>
        /// <param name="tenantId">tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in a user of a specific organization only</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithTenantId(string tenantId)
        {
            Config.TenantId = GetValueIfNotEmpty(Config.TenantId, tenantId);
            return (T)this;
        }

        /// <summary>
        /// Enables/disables logging of Personally Identifiable Information. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <param name="enablePiiLogging">Boolean used to enable/disable logging of
        /// Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR.
        /// You can set it to <c>true</c> for advanced debugging requiring PII</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithEnablePiiLogging(bool enablePiiLogging)
        {
            Config.EnablePiiLogging = enablePiiLogging;
            return (T)this;
        }

        /// <summary>
        /// Enables you to configure the level of logging you want. See https://aka.ms/msal-net-logging
        /// The default value is <see cref="LogLevel.Info"/>.
        /// Setting it to <see cref="LogLevel.Error"/> will only get errors
        /// Setting it to <see cref="LogLevel.Warning"/> will get errors and warning, etc..
        /// </summary>
        /// <param name="logLevel">Desired level of logging</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithLoggingLevel(LogLevel logLevel)
        {
            Config.LogLevel = logLevel;
            return (T)this;
        }

        /// <summary>
        /// Flag to enable/disable logging to platform defaults. See https://aka.ms/msal-net-logging
        /// In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In android, logcat is used. The default value is <c>false</c>
        /// </summary>
        /// <param name="enabled">Boolean telling if default logging is
        /// enabled or not</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithDefaultPlatformLoggingEnabled(bool enabled)
        {
            Config.IsDefaultPlatformLoggingEnabled = enabled;
            return (T)this;
        }

        /// <summary>
        /// Sets application options, which can, for instance have been read from configuration files.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="applicationOptions">Application options</param>
        /// <returns>The builder to chain the .With methods</returns>
        protected T WithOptions(ApplicationOptions applicationOptions)
        {
            WithClientId(applicationOptions.ClientId);
            WithRedirectUri(applicationOptions.RedirectUri);
            WithTenantId(applicationOptions.TenantId);
            WithLoggingLevel(applicationOptions.LogLevel);
            WithComponent(applicationOptions.Component);
            WithEnablePiiLogging(applicationOptions.EnablePiiLogging);
            WithDefaultPlatformLoggingEnabled(applicationOptions.IsDefaultPlatformLoggingEnabled);

            Config.Instance = applicationOptions.Instance;
            Config.AadAuthorityAudience = applicationOptions.AadAuthorityAudience;
            Config.AzureCloudInstance = applicationOptions.AzureCloudInstance;

            return (T)this;
        }

        /// <summary>
        /// Sets the identifier of the software component (libraries/SDK) consuming MSAL.NET.
        /// This will allow for disambiguation between MSAL usage by the app vs MSAL usage
        /// by component libraries. You can, for instance set it to the name of your application.
        /// This is used in telemetry.
        /// </summary>
        /// <param name="component">identifier of the software component (libraries/SDK) consuming MSAL.NET</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithComponent(string component)
        {
            Config.Component = GetValueIfNotEmpty(Config.Component, component);
            return (T)this;
        }

        internal virtual ApplicationConfiguration BuildConfiguration()
        {
            // Validate that we have a client id
            if (string.IsNullOrWhiteSpace(Config.ClientId))
            {
                throw new InvalidOperationException(CoreErrorMessages.NoClientIdWasSpecified);
            }

            TryAddDefaultAuthority();

            if (Config.AuthorityInfo.AuthorityType == AuthorityType.Adfs)
            {
                throw new InvalidOperationException(CoreErrorMessages.AdfsNotCurrentlySupportedAuthorityType);
            }

            return Config;
        }

        private void TryAddDefaultAuthority()
        {
            if (Config.AuthorityInfo != null)
            {
                return;
            }

            AuthorityType? defaultAuthorityType = DetermineDefaultAuthorityType();
            if (defaultAuthorityType.HasValue)
            {
                string defaultAuthorityInstance = GetDefaultAuthorityInstance();
                string defaultAuthorityAudience = GetDefaultAuthorityAudience();

                if (string.IsNullOrWhiteSpace(defaultAuthorityInstance) || string.IsNullOrWhiteSpace(defaultAuthorityAudience))
                {
                    // TODO: better documentation/description in exception of what's going on here...
                    throw new InvalidOperationException(
                        $"DefaultAuthorityType is {defaultAuthorityType.Value} but defaultAuthorityInstance({defaultAuthorityInstance}) or defaultAuthorityAudience({defaultAuthorityAudience}) is invalid.");
                }

                Config.AuthorityInfo = new AuthorityInfo(
                        defaultAuthorityType.Value,
                        new Uri($"{defaultAuthorityInstance}/{defaultAuthorityAudience}").ToString(),
                        true);
            }
            else
            {
                // Add the default.
                WithAadAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true);
            }
        }

        private AuthorityType? DetermineDefaultAuthorityType()
        {
            if (Config.AadAuthorityAudience != AadAuthorityAudience.None)
            {
                return AuthorityType.Aad;
            }

            // TODO: Once we have policy information in ApplicationOptions, we can determine B2C here.
            // TODO: do we have enough to get a default authority type of ADFS?  How do we verify?

            return null;
        }

        private string GetDefaultAuthorityAudience()
        {
            if (!string.IsNullOrWhiteSpace(Config.TenantId) &&
                Config.AadAuthorityAudience != AadAuthorityAudience.None &&
                Config.AadAuthorityAudience != AadAuthorityAudience.AzureAdMyOrg)
            {
                // Conflict, user has specified a string tenantId and the enum audience value for AAD, which is also the tenant.
                throw new InvalidOperationException(CoreErrorMessages.TenantIdAndAadAuthorityInstanceAreMutuallyExclusive);
            }

            if (Config.AadAuthorityAudience != AadAuthorityAudience.None)
            {
                return AuthorityInfo.GetAadAuthorityAudienceValue(Config.AadAuthorityAudience, Config.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(Config.TenantId))
            {
                return Config.TenantId;
            }

            return string.Empty;
        }

        private string GetDefaultAuthorityInstance()
        {
            // Check if there's enough information in the existing config to build up a default authority.
            if (!string.IsNullOrWhiteSpace(Config.Instance) && Config.AzureCloudInstance != AzureCloudInstance.None)
            {
                // Conflict, user has specified a string instance and the enum instance value.
                throw new InvalidOperationException(CoreErrorMessages.InstanceAndAzureCloudInstanceAreMutuallyExclusive);
            }

            if (!string.IsNullOrWhiteSpace(Config.Instance))
            {
                return Config.Instance;
            }

            if (Config.AzureCloudInstance != AzureCloudInstance.None)
            {
                return AuthorityInfo.GetCloudUrl(Config.AzureCloudInstance);
            }

            return string.Empty;
        }

        /// <summary>
        /// Adds a known authority to the application from its Uri. See https://aka.ms/msal-net-application-configuration.
        /// This constructor is mainly used for scenarios where the authority is not a standard Azure AD authority,
        /// nor an ADFS authority, nor an Azure AD B2C authority. For Azure AD, even in national and sovereign clouds, prefer
        /// using other overrides such as <see cref="WithAadAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>
        /// </summary>
        /// <param name="authorityUri">Uri of the authority</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(Uri authorityUri, bool validateAuthority = false)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAuthorityUri(authorityUri.ToString(), validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) specified by its tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Azure Cloud instance</param>
        /// <param name="tenantId">Guid of the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAadAuthority(
            Uri cloudInstanceUri,
            Guid tenantId,
            bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAuthorityUri($"{cloudInstanceUri}/{tenantId:N}/", validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its domain name. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Uri to the Azure Cloud instance (for instance
        /// <c>https://login.microsoftonline.com)</c></param>
        /// <param name="tenant">domain name associated with the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority.
        /// <paramref name="tenant"/> can also contain the string representation of a GUID (tenantId),
        /// or even <c>common</c>, <c>organizations</c> or <c>consumers</c> but in this case
        /// it's recommended to use another override (<see cref="WithAadAuthority(AzureCloudInstance, Guid, bool)"/>
        /// and <see cref="WithAadAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/></remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAadAuthority(
            Uri cloudInstanceUri,
            string tenant,
            bool validateAuthority = true)
        {
            if (Guid.TryParse(tenant, out Guid tenantId))
            {
                return WithAadAuthority(cloudInstanceUri, tenantId);
            }

            Config.AuthorityInfo = AuthorityInfo.FromAuthorityUri($"{cloudInstanceUri}/{tenant}/", validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its tenant ID.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...)</param>
        /// <param name="tenantId">Tenant Id of the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>You can add several authorities, but only one can be the default authority.</remarks>
        public T WithAadAuthority(
            AzureCloudInstance azureCloudInstance,
            Guid tenantId,
            bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAadAuthority(azureCloudInstance, tenantId, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its domain
        /// name or tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...)</param>
        /// <param name="tenant">Domain name associated with the Azure AD tenant from which
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// to sign-in users. This can also be a guid</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>You can add several authorities, but only one can be the default authority.</remarks>
        public T WithAadAuthority(
            AzureCloudInstance azureCloudInstance,
            string tenant,
            bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAadAuthority(azureCloudInstance, tenant, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the cloud instance and the sign-in audience. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure Cloud (for instance Azure
        /// worldwide cloud, Azure German Cloud, US government ...)</param>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority.</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAadAuthority(AzureCloudInstance azureCloudInstance, AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAadAuthority(azureCloudInstance, authorityAudience, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the sign-in audience (the cloud being the Azure public cloud). See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority.</remarks>
        /// <returns>The builder to chain the .With methods</returns>

        public T WithAadAuthority(AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAadAuthority(authorityAudience, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the full authority Uri. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="authorityUri">URL of the security token service (STS) from which MSAL.NET will acquire the tokens.
        ///  Usual authorities endpoints for the Azure public Cloud are:
        ///  <list type="bullet">
        ///  <item><description><c>https://login.microsoftonline.com/tenant/</c> where <c>tenant</c> is the tenant ID of the Azure AD tenant
        ///  or a domain associated with this Azure AD tenant, in order to sign-in users of a specific organization only</description></item>
        ///  <item><description><c>https://login.microsoftonline.com/common/</c> to sign-in users with any work and school accounts or Microsoft personal account</description></item>
        ///  <item><description><c>https://login.microsoftonline.com/organizations/</c> to sign-in users with any work and school accounts</description></item>
        ///  <item><description><c>https://login.microsoftonline.com/consumers/</c> to sign-in users with only personal Microsoft accounts (live)</description></item>
        ///  </list>
        ///  Note that this setting needs to be consistent with what is declared in the application registration portal</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority.</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAadAuthority(string authorityUri, bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAadAuthority(authorityUri, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known Authority corresponding to an ADFS server. See https://aka.ms/msal-net-adfs
        /// </summary>
        /// <param name="authorityUri">Authority URL for an ADFS server</param>
        /// <param name="validateAuthority">TODO(migration): documentation</param>
        /// <remarks>You can add several authorities, but only one can be the default authority.
        /// MSAL.NET will only support ADFS 2019 or later.</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAdfsAuthority(string authorityUri, bool validateAuthority = true)
        {
            Config.AuthorityInfo = AuthorityInfo.FromAdfsAuthority(authorityUri, validateAuthority);
            return (T)this;
        }

        /// <summary>
        /// Adds a known authority corresponding to an Azure AD B2C policy.
        /// See https://aka.ms/msal-net-b2c-specificities
        /// </summary>
        /// <param name="authorityUri">Azure AD B2C authority, including the B2C policy (for instance
        /// <c>"https://fabrikamb2c.b2clogin.com/tfp/{Tenant}/{policy}</c></param>)
        /// <remarks>You can add several authorities, but only one can be the default authority.</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithB2CAuthority(string authorityUri)
        {
            Config.AuthorityInfo = AuthorityInfo.FromB2CAuthority(authorityUri);
            return (T)this;
        }

        private static string GetValueIfNotEmpty(string original, string value)
        {
            return string.IsNullOrWhiteSpace(value) ? original : value;
        }
    }
}