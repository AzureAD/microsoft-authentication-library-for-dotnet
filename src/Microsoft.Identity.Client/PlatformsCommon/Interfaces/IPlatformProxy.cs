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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Common operations for extracting platform / operating system specifics
    /// </summary>
    internal interface IPlatformProxy
    {
        /// <summary>
        /// Gets the device model. On some TFMs this is not returned for security reasonons.
        /// </summary>
        /// <returns>device model or null</returns>
        string GetDeviceModel();

        string GetEnvironmentVariable(string variable);

        string GetOperatingSystem();

        string GetProcessorArchitecture();

        /// <summary>
        /// Gets the upn of the user currently logged into the OS
        /// </summary>
        /// <returns></returns>
        Task<string> GetUserPrincipalNameAsync();

        /// <summary>
        /// Returns true if the current OS logged in user is AD or AAD joined.
        /// </summary>
        /// <returns></returns>
        bool IsDomainJoined();

        Task<bool> IsUserLocalAsync(RequestContext requestContext);

        /// <summary>
        /// Returns the name of the calling assembly
        /// </summary>
        /// <returns></returns>
        string GetCallingApplicationName();

        /// <summary>
        /// Returns the version of the calling assembly
        /// </summary>
        /// <returns></returns>
        string GetCallingApplicationVersion();

        /// <summary>
        /// Returns a device identifier. Varies by platform.
        /// </summary>
        /// <returns></returns>
        string GetDeviceId();

        /// <summary>
        /// Get the redirect Uri as string, or the a broker specified value
        /// </summary>
        string GetBrokerOrRedirectUri(Uri redirectUri);

        /// <summary>
        /// Gets the default redirect uri for the platform, which sometimes includes the clientId
        /// </summary>
        string GetDefaultRedirectUri(string clientId);

        string GetProductName();

        ILegacyCachePersistence CreateLegacyCachePersistence();

        ITokenCacheAccessor CreateTokenCacheAccessor();

        ITokenCacheBlobStorage CreateTokenCacheBlobStorage();

        ICryptographyManager CryptographyManager { get; }

        IPlatformLogger PlatformLogger { get; }

        IWebUIFactory GetWebUiFactory();

        // MATS related data
        string GetDpti();
        string GetDeviceNetworkState();
        int GetMatsOsPlatformCode();
        string GetMatsOsPlatform();
        void /* for test */ SetWebUiFactory(IWebUIFactory webUiFactory);

        IFeatureFlags GetFeatureFlags();

        void /* for test */ SetFeatureFlags(IFeatureFlags featureFlags);
    }
}
