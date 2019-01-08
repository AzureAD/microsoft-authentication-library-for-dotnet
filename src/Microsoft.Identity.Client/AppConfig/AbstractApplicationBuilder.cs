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
using System.Diagnostics;
using System.Linq;
using Microsoft.Identity.Client.Http;

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
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <returns></returns>
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
        /// </summary>
        /// <returns></returns>
        public T WithLoggingCallback(LogCallback loggingCallback)
        {
            if (Config.LoggingCallback != null)
            {
                throw new InvalidOperationException("LoggingCallback has already been set");
            }

            Config.LoggingCallback = loggingCallback;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T WithDebugLoggingCallback()
        {
            if (Config.LoggingCallback != null)
            {
                throw new InvalidOperationException("LoggingCallback has already been set");
            }

            Config.LoggingCallback = (level, message, pii) => { Debug.WriteLine($"{level}: {message}"); };
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T WithTelemetryCallback(TelemetryCallback telemetryCallback)
        {
            if (Config.TelemetryCallback  != null)
            {
                throw new InvalidOperationException("TelemetryCallback has already been set");
            }

            Config.TelemetryCallback = telemetryCallback;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public T WithClientId(string clientId)
        {
            Config.ClientId = clientId;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        public T WithRedirectUri(string redirectUri)
        {
            Config.RedirectUri = GetValueIfNotEmpty(Config.RedirectUri, redirectUri);
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        public T WithTenantId(string tenantId)
        {
            Config.TenantId = GetValueIfNotEmpty(Config.TenantId, tenantId);
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="enablePiiLogging"></param>
        /// <returns></returns>
        public T WithEnablePiiLogging(bool enablePiiLogging)
        {
            Config.EnablePiiLogging = enablePiiLogging;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public T WithLoggingLevel(LogLevel logLevel)
        {
            Config.LogLevel = logLevel;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public T WithDefaultPlatformLoggingEnabled(bool enabled)
        {
            Config.IsDefaultPlatformLoggingEnabled = enabled;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="applicationOptions"></param>
        /// <returns></returns>
        protected T WithOptions(ApplicationOptions applicationOptions)
        {
            WithClientId(applicationOptions.ClientId);
            WithRedirectUri(applicationOptions.RedirectUri);
            WithTenantId(applicationOptions.TenantId);
            WithLoggingLevel(applicationOptions.LogLevel);
            WithComponent(applicationOptions.Component);
            WithEnablePiiLogging(applicationOptions.EnablePiiLogging);
            WithDefaultPlatformLoggingEnabled(applicationOptions.IsDefaultPlatformLoggingEnabled);
            WithSliceParameters(applicationOptions.SliceParameters);

            Config.Instance = applicationOptions.Instance;
            Config.AadAuthorityAudience = applicationOptions.AadAuthorityAudience;
            Config.AzureCloudInstance = applicationOptions.AzureCloudInstance;

            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public T WithComponent(string component)
        {
            Config.Component = GetValueIfNotEmpty(Config.Component, component);
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="sliceParameters"></param>
        /// <returns></returns>
        public T WithSliceParameters(string sliceParameters)
        {
            Config.SliceParameters = GetValueIfNotEmpty(Config.SliceParameters, sliceParameters);
            return (T)this;
        }

        internal virtual ApplicationConfiguration BuildConfiguration()
        {
            // Validate that we have a client id
            if (string.IsNullOrWhiteSpace(Config.ClientId))
            {
                throw new InvalidOperationException("No ClientId was specified.");
            }

            TryAddDefaultAuthority();

            // validate that we only have ONE default authority
            if (Config.Authorities.Where(x => x.IsDefault).ToList().Count != 1)
            {
                throw new InvalidOperationException("More than one default authority was configured.");
            }

            return Config;
        }

        private void TryAddDefaultAuthority()
        {
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

                Config.AddAuthorityInfo(
                    new AuthorityInfo(
                        defaultAuthorityType.Value,
                        new Uri($"{defaultAuthorityInstance}/{defaultAuthorityAudience}").ToString(),
                        true));
            }

            if (!Config.Authorities.Any())
            {
                // Add the default.
                AddKnownAadAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, true);
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
                Config.AadAuthorityAudience != AadAuthorityAudience.AzureAdSpecificDirectoryOnly)
            {
                // Conflict, user has specified a string tenantId and the enum audience value for AAD, which is also the tenant.
                throw new InvalidOperationException("TenantId and AadAuthorityAudience are both set, but they're mutually exclusive.");
            }

            if (Config.AadAuthorityAudience != AadAuthorityAudience.None)
            {
                return GetAadAuthorityAudienceValue(Config.AadAuthorityAudience, Config.TenantId);
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
                throw new InvalidOperationException("Instance and AzureCloudInstance are both set but they're mutually exclusive.");
            }

            if (!string.IsNullOrWhiteSpace(Config.Instance))
            {
                return Config.Instance;
            }

            if (Config.AzureCloudInstance != AzureCloudInstance.None)
            {
                return GetCloudUrl(Config.AzureCloudInstance);
            }

            return string.Empty;
        }

        /// <summary>
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAuthority(Uri authorityUri, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(AuthorityInfo.FromAuthorityUri(authorityUri.ToString(), isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cloudInstanceUri"></param>
        /// <param name="tenantId"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(
            Uri cloudInstanceUri,
            Guid tenantId,
            bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(AuthorityInfo.FromAuthorityUri($"{cloudInstanceUri}/{tenantId:N}/", isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cloudInstanceUri"></param>
        /// <param name="tenant"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(
            Uri cloudInstanceUri,
            string tenant,
            bool isDefaultAuthority = false)
        {
            if (Guid.TryParse(tenant, out Guid tenantId))
            {
                return AddKnownAadAuthority(cloudInstanceUri, tenantId, isDefaultAuthority);
            }
            
            Config.AddAuthorityInfo(AuthorityInfo.FromAuthorityUri($"{cloudInstanceUri}/{tenant}/", isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="azureCloudInstance"></param>
        /// <param name="tenantId"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(
            AzureCloudInstance azureCloudInstance,
            Guid tenantId,
            bool isDefaultAuthority = false)
        {
            string authorityUri = GetAuthorityUri(azureCloudInstance, AadAuthorityAudience.AzureAdSpecificDirectoryOnly, $"{tenantId:N}");
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="azureCloudInstance"></param>
        /// <param name="tenant">Domain name or guid</param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(
            AzureCloudInstance azureCloudInstance,
            string tenant,
            bool isDefaultAuthority = false)
        {
            if (Guid.TryParse(tenant, out Guid tenantIdGuid))
            {
                return AddKnownAadAuthority(azureCloudInstance, tenantIdGuid, isDefaultAuthority);
            }

            string authorityUri = GetAuthorityUri(azureCloudInstance, AadAuthorityAudience.AzureAdSpecificDirectoryOnly, tenant);
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="azureCloudInstance"></param>
        /// <param name="authorityAudience"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(AzureCloudInstance azureCloudInstance, AadAuthorityAudience authorityAudience, bool isDefaultAuthority = false)
        {
            string authorityUri = GetAuthorityUri(azureCloudInstance, authorityAudience);
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorityAudience"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(AadAuthorityAudience authorityAudience, bool isDefaultAuthority = false)
        {
            string authorityUri = GetAuthorityUri(AzureCloudInstance.AzurePublic, authorityAudience);
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAadAuthority(string authorityUri, bool isDefaultAuthority = false)
        {
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        internal static string GetCloudUrl(AzureCloudInstance azureCloudInstance)
        {
            switch (azureCloudInstance)
            {
            case AzureCloudInstance.AzurePublic:
                return "https://login.microsoftonline.com";
            case AzureCloudInstance.AzureChina:
                return "https://login.chinacloudapi.cn";
            case AzureCloudInstance.AzureGermany:
                return "https://login.microsoftonline.de";
            case AzureCloudInstance.AzureUsGovernment:
                return "https://login.microsoftonline.us";
            default:
                throw new ArgumentException(nameof(azureCloudInstance));
            }
        }

        internal static string GetAuthorityUri(
            AzureCloudInstance azureCloudInstance,
            AadAuthorityAudience authorityAudience,
            string tenantId = null)
        {
            string cloudUrl = GetCloudUrl(azureCloudInstance);
            string tenantValue = GetAadAuthorityAudienceValue(authorityAudience, tenantId);

            return $"{cloudUrl}/{tenantValue}";
        }

        private static string GetAadAuthorityAudienceValue(AadAuthorityAudience authorityAudience, string tenantId)
        {
            if (authorityAudience == AadAuthorityAudience.Default)
            {
                authorityAudience = AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount;
            }

            switch (authorityAudience)
            {
            case AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount:
                return "common";
            case AadAuthorityAudience.AzureAdOnly:
                return "organizations";
            case AadAuthorityAudience.MicrosoftAccountOnly:
                return "consumers";
            case AadAuthorityAudience.AzureAdSpecificDirectoryOnly:
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    throw new ArgumentNullException(nameof(tenantId));
                }

                return tenantId;
            default:
                throw new ArgumentException(nameof(authorityAudience));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownAdfsAuthority(string authorityUri, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Adfs, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T AddKnownB2CAuthority(string authorityUri, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.B2C, authorityUri, isDefaultAuthority));
            return (T)this;
        }

        private static string GetValueIfNotEmpty(string original, string value)
        {
            return string.IsNullOrWhiteSpace(value) ? original : value;
        }
    }
}