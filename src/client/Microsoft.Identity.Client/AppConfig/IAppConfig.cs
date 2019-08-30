// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Configuration properties used to build a public or confidential client application
    /// </summary>
    public interface IAppConfig
    {
        /// <summary>
        /// Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Flag telling if logging of Personally Identifiable Information (PII) is enabled/disabled for
        /// the application. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <seealso cref="IsDefaultPlatformLoggingEnabled"/>
        bool EnablePiiLogging { get; }

        /// <summary>
        /// <see cref="IMsalHttpClientFactory"/> used to get HttpClient instances to communicate
        /// with the identity provider.
        /// </summary>
        IMsalHttpClientFactory HttpClientFactory { get; }

        /// <summary>
        /// Level of logging requested for the app.
        /// See https://aka.ms/msal-net-logging
        /// </summary>
        LogLevel LogLevel { get; }

        /// <summary>
        /// Flag telling if logging to platform defaults is enabled/disabled for the app.
        /// In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In Android, logcat is used. See https://aka.ms/msal-net-logging
        /// </summary>
        bool IsDefaultPlatformLoggingEnabled { get; }

        /// <summary>
        /// Redirect URI for the application. See <see cref="ApplicationOptions.RedirectUri"/>
        /// </summary>
        string RedirectUri { get; }

        /// <summary>
        /// Audience for the application. See <see cref="ApplicationOptions.TenantId"/>
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Callback used for logging. It was set with <see cref="AbstractApplicationBuilder{T}.WithLogging(LogCallback, LogLevel?, bool?, bool?)"/>
        /// See https://aka.ms/msal-net-logging
        /// </summary>
        LogCallback LoggingCallback { get; }

        /// <summary>
        /// Extra query parameters that will be applied to every acquire token operation.
        /// See <see cref="AbstractApplicationBuilder{T}.WithExtraQueryParameters(IDictionary{string, string})"/>
        /// </summary>
        IDictionary<string, string> ExtraQueryParameters { get; }

        /// <summary>
        /// </summary>
        bool IsBrokerEnabled { get; }

        /// <summary>
        /// The name of the calling application for telemetry purposes.
        /// </summary>
        string ClientName { get; }

        /// <summary>
        /// The version of the calling application for telemetry purposes.
        /// </summary>
        string ClientVersion { get; }

        /// <summary>
        /// </summary>
        ITelemetryConfig TelemetryConfig { get; }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        /// <summary>
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// </summary>
        X509Certificate2 ClientCredentialCertificate { get; }
#endif

        /// <summary>
        /// </summary>
        Func<object> ParentActivityOrWindowFunc { get; }

#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently logged-in in Windows.
        /// When set to true, the application will try to connect to the corporate network using windows integrated authentication.
        /// </summary>
        bool UseCorporateNetwork { get; }
#endif // WINDOWS_APP

#if iOS
        /// <summary>
        /// </summary>
        string IosKeychainSecurityGroup { get; }
#endif // iOS
    }
}
