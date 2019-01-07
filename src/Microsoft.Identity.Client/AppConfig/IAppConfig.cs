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

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    ///     TODO: resolve interface naming for public API...
    ///     Have this in the public api for developer debugging...
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
        string Tenant { get; }

        /// <summary>
        /// </summary>
        string SliceParameters { get; }

        /// <summary>
        /// </summary>
        LogCallback LoggingCallback { get; }

        /// <summary>
        /// </summary>
        ITelemetryHandler TelemetryHandler { get; }

        /// <summary>
        /// </summary>
        string Component { get; }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME // Hide confidential client on mobile platforms
        /// <summary>
        /// </summary>
        ClientCredential ClientCredential { get; }
#endif
    }
}