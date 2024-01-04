// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;
using Microsoft.IdentityModel.Abstractions;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractApplicationBuilder<T> : BaseAbstractApplicationBuilder<T>
        where T : BaseAbstractApplicationBuilder<T>
    {
        internal AbstractApplicationBuilder(ApplicationConfiguration configuration) : base(configuration) { }

        /// <summary>
        /// Allows developers to configure their own valid authorities. A json string similar to https://aka.ms/aad-instance-discovery should be provided.
        /// MSAL uses this information to: 
        /// <list type="bullet">
        /// <item><description>Call REST APIs on the environment specified in the preferred_network</description></item>
        /// <item><description>Identify an environment under which to save tokens and accounts in the cache</description></item>
        /// <item><description>Use the environment aliases to match tokens issued to other authorities</description></item>
        /// </list>
        /// For more details see https://aka.ms/msal-net-custom-instance-metadata
        /// </summary>
        /// <remarks>
        /// Developers take responsibility for authority validation if they use this method. Should not be used when the authority is not know in advance. 
        /// Has no effect on ADFS or B2C authorities, only for AAD authorities</remarks>
        /// <param name="instanceDiscoveryJson"></param>
        /// <returns></returns>
        [Obsolete("This method name has a typo, please use WithInstanceDiscoveryMetadata instead", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T WithInstanceDicoveryMetadata(string instanceDiscoveryJson)
        {
            if (string.IsNullOrEmpty(instanceDiscoveryJson))
            {
                throw new ArgumentNullException(instanceDiscoveryJson);
            }

            try
            {
                InstanceDiscoveryResponse instanceDiscovery = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(instanceDiscoveryJson);
                Config.CustomInstanceDiscoveryMetadata = instanceDiscovery;
                return this as T;
            }
            catch (JsonException ex)
            {
                throw new MsalClientException(
                    MsalError.InvalidUserInstanceMetadata,
                    MsalErrorMessage.InvalidUserInstanceMetadata,
                    ex);
            }
        }

        /// <summary>
        /// Allows developers to configure their own valid authorities. A json string similar to https://aka.ms/aad-instance-discovery should be provided.
        /// MSAL uses this information to: 
        /// <list type="bullet">
        /// <item><description>Call REST APIs on the environment specified in the preferred_network</description></item>
        /// <item><description>Identify an environment under which to save tokens and accounts in the cache</description></item>
        /// <item><description>Use the environment aliases to match tokens issued to other authorities</description></item>
        /// </list>
        /// For more details see https://aka.ms/msal-net-custom-instance-metadata
        /// </summary>
        /// <remarks>
        /// Developers take responsibility for authority validation if they use this method. Should not be used when the authority is not known in advance. 
        /// Has no effect on ADFS or B2C authorities, only for AAD authorities</remarks>
        /// <param name="instanceDiscoveryJson"></param>
        /// <returns></returns>
        public T WithInstanceDiscoveryMetadata(string instanceDiscoveryJson)
        {
            if (string.IsNullOrEmpty(instanceDiscoveryJson))
            {
                throw new ArgumentNullException(instanceDiscoveryJson);
            }

            try
            {
                InstanceDiscoveryResponse instanceDiscovery = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(instanceDiscoveryJson);
                Config.CustomInstanceDiscoveryMetadata = instanceDiscovery;
                return this as T;
            }
            catch (JsonException ex)
            {
                throw new MsalClientException(
                    MsalError.InvalidUserInstanceMetadata,
                    MsalErrorMessage.InvalidUserInstanceMetadata,
                    ex);
            }
        }

        /// <summary>
        /// Lets an organization setup their own service to handle instance discovery, which enables better caching for microservice/service environments.
        /// A Uri that returns a response similar to https://aka.ms/aad-instance-discovery should be provided. MSAL uses this information to: 
        /// <list type="bullet">
        /// <item><description>Call REST APIs on the environment specified in the preferred_network</description></item>
        /// <item><description>Identify an environment under which to save tokens and accounts in the cache</description></item>
        /// <item><description>Use the environment aliases to match tokens issued to other authorities</description></item>
        /// </list>
        /// For more details see https://aka.ms/msal-net-custom-instance-metadata
        /// </summary>
        /// <remarks>
        /// Developers take responsibility for authority validation if they use this method. Should not be used when the authority is not know in advance. 
        /// Has no effect on ADFS or B2C authorities, only for AAD authorities</remarks>
        /// <param name="instanceDiscoveryUri"></param>
        /// <returns></returns>
        [Obsolete("This method name has a typo, please use WithInstanceDiscoveryMetadata instead", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T WithInstanceDicoveryMetadata(Uri instanceDiscoveryUri)
        {
            Config.CustomInstanceDiscoveryMetadataUri = instanceDiscoveryUri ??
                throw new ArgumentNullException(nameof(instanceDiscoveryUri));

            return this as T;
        }

        /// <summary>
        /// Lets an organization setup their own service to handle instance discovery, which enables better caching for microservice/service environments.
        /// A Uri that returns a response similar to https://aka.ms/aad-instance-discovery should be provided. MSAL uses this information to: 
        /// <list type="bullet">
        /// <item><description>Call REST APIs on the environment specified in the preferred_network</description></item>
        /// <item><description>Identify an environment under which to save tokens and accounts in the cache</description></item>
        /// <item><description>Use the environment aliases to match tokens issued to other authorities</description></item>
        /// </list>
        /// For more details see https://aka.ms/msal-net-custom-instance-metadata
        /// </summary>
        /// <remarks>
        /// Developers take responsibility for authority validation if they use this method. Should not be used when the authority is not known in advance. 
        /// Has no effect on ADFS or B2C authorities, only for AAD authorities</remarks>
        /// <param name="instanceDiscoveryUri"></param>
        /// <returns></returns>
        public T WithInstanceDiscoveryMetadata(Uri instanceDiscoveryUri)
        {
            Config.CustomInstanceDiscoveryMetadataUri = instanceDiscoveryUri ??
                throw new ArgumentNullException(nameof(instanceDiscoveryUri));

            return this as T;
        }

        internal T WithPlatformProxy(IPlatformProxy platformProxy)
        {
            Config.PlatformProxy = platformProxy;
            return this as T;
        }

        /// <summary>
        /// Options for MSAL token caches. 
        /// 
        /// MSAL maintains a token cache internally in memory. By default, this cache object is part of each instance of <see cref="PublicClientApplication"/> or <see cref="ConfidentialClientApplication"/>.
        /// This method allows customization of the in-memory token cache of MSAL. 
        /// 
        /// MSAL's memory cache is different than token cache serialization. Cache serialization pulls the tokens from a cache (e.g. Redis, Cosmos, or a file on disk), 
        /// where they are stored in JSON format, into MSAL's internal memory cache. Memory cache operations do not involve JSON operations. 
        /// 
        /// External cache serialization remains the recommended way to handle desktop apps, web site and web APIs, as it provides persistence. These options
        /// do not currently control external cache serialization.
        /// 
        /// Detailed guidance for each application type and platform:
        /// https://aka.ms/msal-net-token-cache-serialization
        /// </summary>
        /// <param name="options">Options for the internal MSAL token caches. </param>
#if !SUPPORTS_CUSTOM_CACHE || WINDOWS_APP
    [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public T WithCacheOptions(CacheOptions options)
        {
#if !SUPPORTS_CUSTOM_CACHE || WINDOWS_APP
            throw new PlatformNotSupportedException("WithCacheOptions is supported only on platforms where MSAL stores tokens in memory and not on mobile platforms or UWP.");
#else

            Config.AccessorOptions = options;
            return this as T;
#endif
        }

        internal T WithUserTokenCacheInternalForTest(ITokenCacheInternal tokenCacheInternal)
        {
            Config.UserTokenCacheInternalForTest = tokenCacheInternal;
            return this as T;
        }

        /// <summary>
        /// Enables legacy ADAL cache serialization and deserialization.
        /// </summary>
        /// <param name="enableLegacyCacheCompatibility">Enable legacy ADAL cache compatibility.</param>
        /// <returns>The builder to chain the .With methods.</returns>
        /// <remarks>
        /// ADAL is a previous legacy generation of MSAL.NET authentication library. 
        /// If you don't use <c>.WithLegacyCacheCompatibility(false)</c>, then by default, the ADAL cache is used
        /// (along with MSAL cache). <c>true</c> flag is only needed for specific migration scenarios 
        /// from ADAL.NET to MSAL.NET when both library versions are running side-by-side.
        /// To improve performance add <c>.WithLegacyCacheCompatibility(false)</c> unless you care about migration scenarios.
        /// </remarks>
        public T WithLegacyCacheCompatibility(bool enableLegacyCacheCompatibility = true)
        {
            Config.LegacyCacheCompatibilityEnabled = enableLegacyCacheCompatibility;
            return this as T;
        }

        /// <summary>
        /// Sets the telemetry callback. For details see https://aka.ms/msal-net-telemetry
        /// </summary>
        /// <param name="telemetryCallback">Delegate to the callback sending the telemetry
        /// elaborated by the library to the telemetry endpoint of choice</param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="InvalidOperationException"/> is thrown if the method was already
        /// called on the application builder.
        [Obsolete("Telemetry is sent automatically by MSAL.NET. See https://aka.ms/msal-net-telemetry.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal T WithTelemetry(TelemetryCallback telemetryCallback)
        {
            return this as T;
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
            return this as T;
        }

        /// <summary>
        /// Sets the redirect URI of the application. The URI must also be registered in the application portal. 
        /// See https://aka.ms/msal-net-application-configuration
        /// </summary>
        /// <param name="redirectUri">URL where the STS will call back the application with the security token.
        /// Public Client Applications - desktop, mobile, console apps - use different browsers (system browser, embedded browses) and brokers
        /// and each has its own rules.
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithRedirectUri(string redirectUri)
        {
            Config.RedirectUri = GetValueIfNotEmpty(Config.RedirectUri, redirectUri);
            return this as T;
        }

        /// <summary>
        /// Sets the tenant ID of the organization from which the application will let
        /// users sign-in. This is classically a GUID or a domain name. See https://aka.ms/msal-net-application-configuration.
        /// Although it is also possible to set <paramref name="tenantId"/> to <c>common</c>,
        /// <c>organizations</c>, and <c>consumers</c>, it's recommended to use one of the
        /// overrides of <see cref="WithAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>.
        /// </summary>
        /// <param name="tenantId">tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in a user of a specific organization only</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithTenantId(string tenantId)
        {
            Config.TenantId = GetValueIfNotEmpty(Config.TenantId, tenantId);
            return this as T;
        }

        /// <summary>
        /// Sets the name of the calling application for telemetry purposes.
        /// </summary>
        /// <param name="clientName">The name of the application for telemetry purposes.</param>
        /// <returns></returns>
        public T WithClientName(string clientName)
        {
            Config.ClientName = GetValueIfNotEmpty(Config.ClientName, clientName);
            return this as T;
        }

        /// <summary>
        /// Sets the version of the calling application for telemetry purposes.
        /// </summary>
        /// <param name="clientVersion">The version of the calling application for telemetry purposes.</param>
        /// <returns></returns>
        public T WithClientVersion(string clientVersion)
        {
            Config.ClientVersion = GetValueIfNotEmpty(Config.ClientVersion, clientVersion);
            return this as T;
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
            WithClientName(applicationOptions.ClientName);
            WithClientVersion(applicationOptions.ClientVersion);
            WithClientCapabilities(applicationOptions.ClientCapabilities);
            WithLegacyCacheCompatibility(applicationOptions.LegacyCacheCompatibilityEnabled);

            WithLogging(
                null,
                applicationOptions.LogLevel,
                applicationOptions.EnablePiiLogging,
                applicationOptions.IsDefaultPlatformLoggingEnabled);

            Config.Instance = applicationOptions.Instance;
            Config.AadAuthorityAudience = applicationOptions.AadAuthorityAudience;
            Config.AzureCloudInstance = applicationOptions.AzureCloudInstance;

            return this as T;
        }

        /// <summary>
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority
        /// as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithExtraQueryParameters(IDictionary<string, string> extraQueryParameters)
        {
            Config.ExtraQueryParameters = extraQueryParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return this as T;
        }

        /// <summary>
        /// Sets Extra Query Parameters for the query string in the HTTP authentication request
        /// </summary>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// The string needs to be properly URL-encoded and ready to send as a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// </param>
        /// <returns></returns>
        public T WithExtraQueryParameters(string extraQueryParameters)
        {
            if (!string.IsNullOrWhiteSpace(extraQueryParameters))
            {
                return WithExtraQueryParameters(CoreHelpers.ParseKeyValueList(extraQueryParameters, '&', true, null));
            }
            return this as T;
        }

        /// <summary>
        /// Microsoft Identity specific OIDC extension that allows resource challenges to be resolved without interaction. 
        /// Allows configuration of one or more client capabilities, e.g. "llt"
        /// </summary>
        /// <remarks>
        /// MSAL will transform these into special claims request. See https://openid.net/specs/openid-connect-core-1_0-final.html#ClaimsParameter for
        /// details on claim requests.
        /// For more details see https://aka.ms/msal-net-claims-request
        /// </remarks>
        public T WithClientCapabilities(IEnumerable<string> clientCapabilities)
        {
            if (clientCapabilities != null && clientCapabilities.Any())
            {
                Config.ClientCapabilities = clientCapabilities;
            }

            return this as T;
        }

        /// <summary>
        /// Determines whether or not instance discovery is performed when attempting to authenticate. Setting this to false will completely disable
        /// instance discovery and authority validation. This will not affect the behavior of application configured with regional endpoints however.
        /// </summary>
        /// <remarks>If instance discovery is disabled and no user metadata is provided, MSAL will use the provided authority without any checks.
        /// <see cref="WithInstanceDiscoveryMetadata(string)"/> takes priority over <paramref name="enableInstanceDiscovery"/>
        /// so instance metadata can be provided regardless of this configuration.
        /// </remarks>
        /// <param name="enableInstanceDiscovery">Determines if instance discovery/Authority validation is performed</param>
        /// <returns></returns>
        public T WithInstanceDiscovery(bool enableInstanceDiscovery)
        {
            Config.IsInstanceDiscoveryEnabled = enableInstanceDiscovery;

            return this as T;
        }

        /// <summary>
        /// Generate telemetry aggregation events.
        /// </summary>
        /// <param name="telemetryConfig"></param>
        /// <returns></returns>
        [Obsolete("Telemetry is sent automatically by MSAL.NET. See https://aka.ms/msal-net-telemetry.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T WithTelemetry(ITelemetryConfig telemetryConfig)
        {
            return this as T;
        }

        internal virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(Config.ClientId))
            {
                throw new MsalClientException(MsalError.NoClientId, MsalErrorMessage.NoClientIdWasSpecified);
            }

            if (Config.CustomInstanceDiscoveryMetadata != null && Config.CustomInstanceDiscoveryMetadataUri != null)
            {
                throw new MsalClientException(
                    MsalError.CustomMetadataInstanceOrUri,
                    MsalErrorMessage.CustomMetadataInstanceOrUri);
            }

            if (Config.Authority.AuthorityInfo.ValidateAuthority &&
                (Config.CustomInstanceDiscoveryMetadata != null || Config.CustomInstanceDiscoveryMetadataUri != null))
            {
                throw new MsalClientException(MsalError.ValidateAuthorityOrCustomMetadata, MsalErrorMessage.ValidateAuthorityOrCustomMetadata);
            }
        }

        internal override ApplicationConfiguration BuildConfiguration()
        {
            ResolveAuthority();
            Validate();
            return Config;
        }

        #region Authority

        /// <summary>
        /// Adds a known authority to the application. See <see href="https://aka.ms/msal-net-application-configuration">Application configuration options</see>.
        /// This constructor is mainly used for scenarios where the authority is not a standard Azure AD authority,
        /// nor an ADFS authority, nor an Azure AD B2C authority. For Azure AD, even in sovereign clouds, prefer
        /// using other overrides such as <see cref="WithAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>
        /// </summary>
        /// <param name="authorityUri">URI of the authority</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="authorityUri"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="authorityUri"/> is not well-formatted (for example, has spaces).</exception>
        /// <exception cref="MsalClientException">Thrown in general exception scenarios (for example if the application was configured with multiple different authority hosts).</exception>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(Uri authorityUri, bool validateAuthority = true)
        {
            if (authorityUri == null)
            {
                throw new ArgumentNullException(nameof(authorityUri));
            }

            return WithAuthority(authorityUri.ToString(), validateAuthority);
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the full authority URI. See <see href="https://aka.ms/msal-net-application-configuration">Application configuration options</see>.
        /// </summary>
        /// <param name="authorityUri">URI of the authority from which MSAL.NET will acquire the tokens.
        ///  Authority endpoints for the Azure public Cloud are:
        ///  <list type="bullet">
        ///  <item><description><c>https://login.microsoftonline.com/tenant/</c> where <c>tenant</c> is the tenant ID of the Azure AD tenant
        ///  or a domain associated with this Azure AD tenant, in order to sign-in users of a specific organization only</description></item>
        ///  <item><description><c>https://login.microsoftonline.com/common/</c> to sign-in users with any work and school accounts or personal Microsoft accounts</description></item>
        ///  <item><description><c>https://login.microsoftonline.com/organizations/</c> to sign-in users with any work and school accounts</description></item>
        ///  <item><description><c>https://login.microsoftonline.com/consumers/</c> to sign-in users with only personal Microsoft accounts (live)</description></item>
        ///  </list>
        ///  Note that this setting needs to be consistent with what is declared in the application registration portal</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="authorityUri"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="authorityUri"/> is not well-formatted (for example, has spaces).</exception>
        /// <exception cref="MsalClientException">Thrown in general exception scenarios (for example if the application was configured with multiple different authority hosts).</exception>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(string authorityUri, bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(authorityUri))
            {
                throw new ArgumentNullException(authorityUri);
            }

            Config.Authority = Authority.CreateAuthority(authorityUri, validateAuthority);

            return this as T;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single-tenant application) specified by its tenant ID. See <see href="https://aka.ms/msal-net-application-configuration">Application configuration options</see>.
        /// </summary>
        /// <param name="cloudInstanceUri">Azure cloud instance.</param>
        /// <param name="tenantId">GUID of the tenant from which to sign-in users.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cloudInstanceUri"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="cloudInstanceUri"/> is not well-formatted (for example, has spaces).</exception>
        /// <exception cref="MsalClientException">Thrown in more general exception scenarios (for example if the application was configured with multiple different authority hosts).</exception>
        /// <returns>The builder to chain the .With methods.</returns>
        public T WithAuthority(
            string cloudInstanceUri,
            Guid tenantId,
            bool validateAuthority = true)
        {
            WithAuthority(cloudInstanceUri, tenantId.ToString("D", CultureInfo.InvariantCulture), validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single-tenant application) described by its domain name. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="cloudInstanceUri">Uri to the Azure cloud instance (for instance
        /// <c>https://login.microsoftonline.com)</c></param>
        /// <param name="tenant">Domain name associated with the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <remarks>
        /// <paramref name="tenant"/> can also contain the string representation of a GUID (tenantId),
        /// or even <c>common</c>, <c>organizations</c> or <c>consumers</c> but in this case
        /// it's recommended to use another override (<see cref="WithAuthority(AzureCloudInstance, Guid, bool)"/>
        /// and <see cref="WithAuthority(AzureCloudInstance, AadAuthorityAudience, bool)"/>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cloudInstanceUri"/> or <paramref name="tenant"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="cloudInstanceUri"/> or <paramref name="tenant"/> is not well-formatted (for example, has spaces).</exception>
        /// <exception cref="MsalClientException">Thrown in more general exception scenarios (for example if the application was configured with multiple different authority hosts).</exception>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(
            string cloudInstanceUri,
            string tenant,
            bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(cloudInstanceUri))
            {
                throw new ArgumentNullException(nameof(cloudInstanceUri));
            }
            if (string.IsNullOrWhiteSpace(tenant))
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            var authorityInfo = AuthorityInfo.FromAadAuthority(
                cloudInstanceUri,
                tenant,
                validateAuthority);
            Config.Authority = new AadAuthority(authorityInfo);

            return this as T;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single tenant application) described by its cloud instance and its tenant ID.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure cloud (for example, Azure
        /// public cloud, Azure China, or Azure Government).</param>
        /// <param name="tenantId">Tenant Id of the tenant from which to sign-in users</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(
            AzureCloudInstance azureCloudInstance,
            Guid tenantId,
            bool validateAuthority = true)
        {
            WithAuthority(azureCloudInstance, tenantId.ToString("D", CultureInfo.InvariantCulture), validateAuthority);
            return this as T;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users from a single
        /// organization (single-tenant application) described by its cloud instance and its domain
        /// name or tenant ID. See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="azureCloudInstance">Instance of Azure cloud (for example, Azure
        /// public cloud, Azure China, or Azure Government).</param>
        /// <param name="tenant">Domain name associated with the Azure AD tenant from which
        /// to sign-in users. This can also be a GUID.</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tenant"/> or <paramref name="tenant"/> is null or empty.</exception>
        /// <returns>The builder to chain the .With methods.</returns>
        public T WithAuthority(
            AzureCloudInstance azureCloudInstance,
            string tenant,
            bool validateAuthority = true)
        {
            if (string.IsNullOrWhiteSpace(tenant))
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            Config.AzureCloudInstance = azureCloudInstance;
            Config.TenantId = tenant;
            Config.ValidateAuthority = validateAuthority;

            return this as T;
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
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(AzureCloudInstance azureCloudInstance, AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            Config.AzureCloudInstance = azureCloudInstance;
            Config.AadAuthorityAudience = authorityAudience;
            Config.ValidateAuthority = validateAuthority;

            return this as T;
        }

        /// <summary>
        /// Adds a known Azure AD authority to the application to sign-in users specifying
        /// the sign-in audience (the cloud being the Azure public cloud). See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="authorityAudience">Sign-in audience (one AAD organization,
        /// any work and school accounts, or any work and school accounts and Microsoft personal
        /// accounts</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAuthority(AadAuthorityAudience authorityAudience, bool validateAuthority = true)
        {
            Config.AadAuthorityAudience = authorityAudience;
            Config.ValidateAuthority = validateAuthority;
            return this as T;
        }

        /// <summary>
        /// Adds a known Authority corresponding to an ADFS server. See https://aka.ms/msal-net-adfs
        /// </summary>
        /// <param name="authorityUri">Authority URL for an ADFS server</param>
        /// <param name="validateAuthority">Whether the authority should be validated against the server metadata.</param>
        /// <remarks>MSAL.NET will only support ADFS 2019 or later.</remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithAdfsAuthority(string authorityUri, bool validateAuthority = true)
        {

            var authorityInfo = AuthorityInfo.FromAdfsAuthority(authorityUri, validateAuthority);
            Config.Authority = AdfsAuthority.CreateAuthority(authorityInfo);
            return this as T;
        }

        /// <summary>
        /// Adds a known authority corresponding to an Azure AD B2C policy.
        /// See https://aka.ms/msal-net-b2c-specificities
        /// </summary>
        /// <param name="authorityUri">Azure AD B2C authority, including the B2C policy (for instance
        /// <c>"https://fabrikamb2c.b2clogin.com/tfp/{Tenant}/{policy}</c></param>)
        /// <returns>The builder to chain the .With methods</returns>
        public T WithB2CAuthority(string authorityUri)
        {
            var authorityInfo = AuthorityInfo.FromB2CAuthority(authorityUri);
            Config.Authority = Authority.CreateAuthority(authorityInfo);

            return this as T;
        }        
        
        #endregion

        private static string GetValueIfNotEmpty(string original, string value)
        {
            return string.IsNullOrWhiteSpace(value) ? original : value;
        }
    }
}
