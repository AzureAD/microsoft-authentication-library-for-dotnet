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
using Microsoft.Identity.Client.Internal;
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
    public abstract class BaseAbstractApplicationBuilder<T>
        where T : BaseAbstractApplicationBuilder<T>
    {
        internal BaseAbstractApplicationBuilder(ApplicationConfiguration configuration)
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
        /// <remarks>MSAL does not guarantee that it will not modify the HttpClient, for example by adding new headers.
        /// Prior to the changes needed in order to make MSAL's httpClients thread safe (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2046/files),
        /// the httpClient had the possibility of throwing an exception stating "Properties can only be modified before sending the first request".
        /// MSAL's httpClient will no longer throw this exception after 4.19.0 (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.19.0)
        /// see (https://aka.ms/msal-httpclient-info) for more information.
        /// </remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithHttpClientFactory(IMsalHttpClientFactory httpClientFactory)
        {
            Config.HttpClientFactory = httpClientFactory;
            return (T)this;
        }

        /// <summary>
        /// Uses a specific <see cref="IMsalHttpClientFactory"/> to communicate
        /// with the IdP. This enables advanced scenarios such as setting a proxy,
        /// or setting the Agent.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <param name="retryOnceOn5xx">Configures MSAL to retry on 5xx server errors. When enabled (on by default), MSAL will wait 1 second after receiving
        /// a 5xx error and then retry the http request again.</param>
        /// <remarks>MSAL does not guarantee that it will not modify the HttpClient, for example by adding new headers.
        /// Prior to the changes needed in order to make MSAL's httpClients thread safe (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/pull/2046/files),
        /// the httpClient had the possibility of throwing an exception stating "Properties can only be modified before sending the first request".
        /// MSAL's httpClient will no longer throw this exception after 4.19.0 (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.19.0)
        /// see (https://aka.ms/msal-httpclient-info) for more information.
        /// If you only want to configure the retryOnceOn5xx parameter, set httpClientFactory to null and MSAL will use the default http client.
        /// </remarks>
        /// <returns>The builder to chain the .With methods</returns>
        public T WithHttpClientFactory(IMsalHttpClientFactory httpClientFactory, bool retryOnceOn5xx)
        {
            Config.HttpClientFactory = httpClientFactory;
            Config.RetryOnServerErrors = retryOnceOn5xx;
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
        /// <param name="loggingCallback"></param>
        /// <param name="logLevel">Desired level of logging.  The default is LogLevel.Info</param>
        /// <param name="enablePiiLogging">Boolean used to enable/disable logging of
        /// Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR.
        /// You can set it to <c>true</c> for advanced debugging requiring PII
        /// If both WithLogging apis are set, the other one will override the this one
        /// </param>
        /// <param name="enableDefaultPlatformLogging">Flag to enable/disable logging to platform defaults.
        /// In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In android, Logcat is used. The default value is <c>false</c>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="InvalidOperationException"/> is thrown if the loggingCallback
        /// was already set on the application builder        
        public T WithLogging(
            LogCallback loggingCallback,
            LogLevel? logLevel = null,
            bool? enablePiiLogging = null,
            bool? enableDefaultPlatformLogging = null)
        {
            if (Config.LoggingCallback != null)
            {
                throw new InvalidOperationException(MsalErrorMessage.LoggingCallbackAlreadySet);
            }

            Config.LoggingCallback = loggingCallback;
            Config.LogLevel = logLevel ?? Config.LogLevel;
            Config.EnablePiiLogging = enablePiiLogging ?? Config.EnablePiiLogging;
            Config.IsDefaultPlatformLoggingEnabled = enableDefaultPlatformLogging ?? Config.IsDefaultPlatformLoggingEnabled;
            return (T)this;
        }

        /// <summary>
        /// Sets the Identity Logger. For details see https://aka.ms/msal-net-logging
        /// </summary>
        /// <param name="identityLogger">IdentityLogger</param>
        /// <param name="enablePiiLogging">Boolean used to enable/disable logging of
        /// Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR.
        /// You can set it to <c>true</c> for advanced debugging requiring PII
        /// If both WithLogging apis are set, this one will override the other
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <remarks>This is an experimental API. The method signature may change in the future without involving a major version upgrade.</remarks>
        public T WithLogging(
            IIdentityLogger identityLogger,
            bool enablePiiLogging = false)
        {
            Config.IdentityLogger = identityLogger;
            Config.EnablePiiLogging = enablePiiLogging;
            return (T)this;
        }

        /// <summary>
        /// Sets the Debug logging callback to a default debug method which displays
        /// the level of the message and the message itself. For details see https://aka.ms/msal-net-logging
        /// </summary>
        /// <param name="logLevel">Desired level of logging.  The default is LogLevel.Info</param>
        /// <param name="enablePiiLogging">Boolean used to enable/disable logging of
        /// Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR.
        /// You can set it to <c>true</c> for advanced debugging requiring PII
        /// </param>
        /// <param name="withDefaultPlatformLoggingEnabled">Flag to enable/disable logging to platform defaults.
        /// In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In android, logcat is used. The default value is <c>false</c>
        /// </param>
        /// <returns>The builder to chain the .With methods</returns>
        /// <exception cref="InvalidOperationException"/> is thrown if the loggingCallback
        /// was already set on the application builder by calling <see cref="WithLogging(LogCallback, LogLevel?, bool?, bool?)"/>
        /// <seealso cref="WithLogging(LogCallback, LogLevel?, bool?, bool?)"/>
        public T WithDebugLoggingCallback(
            LogLevel logLevel = LogLevel.Info,
            bool enablePiiLogging = false,
            bool withDefaultPlatformLoggingEnabled = false)
        {
            WithLogging(
                (level, message, _) => { Debug.WriteLine($"{level}: {message}"); },
                logLevel,
                enablePiiLogging,
                withDefaultPlatformLoggingEnabled);
            return (T)this;
        }

        /// <summary>
        /// Sets application options, which can, for instance have been read from configuration files.
        /// See https://aka.ms/msal-net-application-configuration.
        /// </summary>
        /// <param name="applicationOptions">Application options</param>
        /// <returns>The builder to chain the .With methods</returns>
        protected T WithOptions(BaseApplicationOptions applicationOptions)
        {
            WithLogging(
                null,
                applicationOptions.LogLevel,
                applicationOptions.EnablePiiLogging,
                applicationOptions.IsDefaultPlatformLoggingEnabled);

            return (T)this;
        }

        /// <summary>
        /// Allows usage of experimental features and APIs. If this flag is not set, experimental features 
        /// will throw an exception. For details see https://aka.ms/msal-net-experimental-features
        /// </summary>
        /// <remarks>
        /// Changes in the public API of experimental features will not result in an increment of the major version of this library.
        /// For these reason we advise against using these features in production.
        /// </remarks>
        public T WithExperimentalFeatures(bool enableExperimentalFeatures = true)
        {
            Config.ExperimentalFeaturesEnabled = enableExperimentalFeatures;
            return (T)this;
        }

        internal virtual ApplicationConfiguration BuildConfiguration()
        {
            ResolveAuthority();
            return Config;
        }

        internal void ResolveAuthority()
        {
            if (Config.Authority?.AuthorityInfo != null)
            {
                // Both WithAuthority and WithTenant were used at app config level
                if (!string.IsNullOrEmpty(Config.TenantId))
                {
                    if (!Config.Authority.AuthorityInfo.CanBeTenanted)
                    {
                        throw new MsalClientException(
                            MsalError.TenantOverrideNonAad,
                            $"Cannot use WithTenantId(tenantId) in the application builder, because the authority {Config.Authority.AuthorityInfo.AuthorityType} doesn't support it.");
                    }

                    string tenantedAuthority = Config.Authority.GetTenantedAuthority(
                        Config.TenantId,
                        forceSpecifiedTenant: true);

                    Config.Authority = Authority.CreateAuthority(
                        tenantedAuthority,
                        Config.Authority.AuthorityInfo.ValidateAuthority);
                }
            }
            else
            {
                string authorityInstance = GetAuthorityInstance();
                string authorityAudience = GetAuthorityAudience();

                var authorityInfo = new AuthorityInfo(
                        AuthorityType.Aad,
                        new Uri($"{authorityInstance}/{authorityAudience}").ToString(),
                        Config.ValidateAuthority);

                Config.Authority = new AadAuthority(authorityInfo);
            }
        }

        private string GetAuthorityAudience()
        {
            if (!string.IsNullOrWhiteSpace(Config.TenantId) &&
                Config.AadAuthorityAudience != AadAuthorityAudience.None &&
                Config.AadAuthorityAudience != AadAuthorityAudience.AzureAdMyOrg)
            {
                // Conflict, user has specified a string tenantId and the enum audience value for AAD, which is also the tenant.
                throw new InvalidOperationException(MsalErrorMessage.TenantIdAndAadAuthorityInstanceAreMutuallyExclusive);
            }

            if (Config.AadAuthorityAudience != AadAuthorityAudience.None)
            {
                return AuthorityInfo.GetAadAuthorityAudienceValue(Config.AadAuthorityAudience, Config.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(Config.TenantId))
            {
                return Config.TenantId;
            }

            return AuthorityInfo.GetAadAuthorityAudienceValue(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount, string.Empty);
        }

        private string GetAuthorityInstance()
        {
            // Check if there's enough information in the existing config to build up a default authority.
            if (!string.IsNullOrWhiteSpace(Config.Instance) && Config.AzureCloudInstance != AzureCloudInstance.None)
            {
                // Conflict, user has specified a string instance and the enum instance value.
                throw new InvalidOperationException(MsalErrorMessage.InstanceAndAzureCloudInstanceAreMutuallyExclusive);
            }

            if (!string.IsNullOrWhiteSpace(Config.Instance))
            {
                Config.Instance = Config.Instance.TrimEnd(' ', '/');
                return Config.Instance;
            }

            if (Config.AzureCloudInstance != AzureCloudInstance.None)
            {
                return AuthorityInfo.GetCloudUrl(Config.AzureCloudInstance);
            }

            return AuthorityInfo.GetCloudUrl(AzureCloudInstance.AzurePublic);
        }

        internal void ValidateUseOfExperimentalFeature([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (!Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(memberName));
            }
        }
    }
}
