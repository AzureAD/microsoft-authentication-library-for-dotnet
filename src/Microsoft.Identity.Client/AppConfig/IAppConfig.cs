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
    /// </summary>
    public interface IAppConfig
    {
        /// <summary>
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// </summary>
        bool EnablePiiLogging { get; }

        /// <summary>
        /// </summary>
        IMsalHttpClientFactory HttpClientFactory { get; }

        /// <summary>
        /// </summary>
        LogLevel LogLevel { get; }

        /// <summary>
        /// </summary>
        bool IsDefaultPlatformLoggingEnabled { get; }

        /// <summary>
        /// </summary>
        string RedirectUri { get; }

        /// <summary>
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// </summary>
        LogCallback LoggingCallback { get; }

        /// <summary>
        /// </summary>
        TelemetryCallback TelemetryCallback { get; }

        /// <summary>
        /// </summary>
        string Component { get; }

        /// <summary>
        /// </summary>
        IDictionary<string, string> ExtraQueryParameters { get; }

        /// <summary>
        /// </summary>
        string Claims { get; }

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