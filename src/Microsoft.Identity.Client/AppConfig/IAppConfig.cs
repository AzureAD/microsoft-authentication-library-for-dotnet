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

using System.Collections.Generic;

namespace Microsoft.Identity.Client.AppConfig
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
        /// <see cref="IMsalHttpClientFactory"/> used to get HttpClient instances to commmunicate
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
        /// Callback used for sending telemetry about MSAL.NET out of your app. It was set by a call
        /// to <see cref="AbstractApplicationBuilder{T}.WithTelemetry(TelemetryCallback)"/>
        /// </summary>
        TelemetryCallback TelemetryCallback { get; }

        /// <summary>
        /// Name of the component using MSAL.NET. See <see cref="ApplicationOptions.Component"/>
        /// and <see cref="AbstractApplicationBuilder{T}.WithComponent(string)"/>
        /// </summary>
        string Component { get; }

        /// <summary>
        /// Extra query parameters that will be applied to every acquire token operation.
        /// See <see cref="AbstractApplicationBuilder{T}.WithExtraQueryParameters(IDictionary{string, string})"/>
        /// </summary>
        IDictionary<string, string> ExtraQueryParameters { get; }

        /// <summary>
        /// </summary>
        bool IsBrokerEnabled { get; }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        /// <summary>
        /// </summary>
        ClientCredential ClientCredential { get; }
#endif

#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently logeed-in in Windows.
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
