//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Platforms.Android
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here. 
    /// </summary>
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AndroidPlatformProxy : IPlatformProxy
    {
        internal const string AndroidDefaultRedirectUriTemplate = "msal{0}://auth";

        private readonly bool _isMsal;

        public AndroidPlatformProxy(bool isMsal)
        {
            _isMsal = isMsal;
        }

        /// <summary>
        /// Get the user logged in 
        /// </summary>
        /// <returns>The username or throws</returns>
        public async Task<string> GetUserPrincipalNameAsync()
        {
            return await Task.Factory.StartNew(() => string.Empty).ConfigureAwait(false);

        }
        public async Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            return await Task.Factory.StartNew(() => false).ConfigureAwait(false);
        }

        public bool IsDomainJoined()
        {
            return false;
        }

        public string GetEnvironmentVariable(string variable)
        {
            return null;
        }

        public string GetProcessorArchitecture()
        {
            if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Lollipop)
            {
                return global::Android.OS.Build.CpuAbi;
            }

            IList<string> supportedABIs = global::Android.OS.Build.SupportedAbis;
            if (supportedABIs != null && supportedABIs.Count > 0)
            {
                return supportedABIs[0];
            }

            return null;
        }

        public string GetOperatingSystem()
        {
            return global::Android.OS.Build.VERSION.Sdk;
        }

        public string GetDeviceModel()
        {
            return global::Android.OS.Build.Model;
        }

        /// <inheritdoc />
        public string GetBrokerOrRedirectUri(Uri redirectUri)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public string GetDefaultRedirectUri(string clientId)
        {
            return _isMsal ?
                string.Format(CultureInfo.InvariantCulture, AndroidDefaultRedirectUriTemplate, clientId) :
                null; // Adal does not specify a default
        }

        public string GetProductName()
        {
            return _isMsal ? "MSAL.Xamarin.Android" : "PCL.Android";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Name of the calling application</returns>
        public string GetCallingApplicationName()
        {
            return global::Android.App.Application.Context.ApplicationInfo?.LoadLabel(global::Android.App.Application.Context.PackageManager);
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Version of the calling application</returns>
        public string GetCallingApplicationVersion()
        {
            return global::Android.App.Application.Context.PackageManager.GetPackageInfo(global::Android.App.Application.Context.PackageName, 0)?.VersionName;
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed. 
        /// </summary>
        /// <returns>Device identifier</returns>
        public string GetDeviceId()
        {
            return global::Android.Provider.Settings.Secure.GetString(
                global::Android.App.Application.Context.ContentResolver,
                global::Android.Provider.Settings.Secure.AndroidId);
        }

        /// <inheritdoc />
        public ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new AndroidLegacyCachePersistence();
        }

        /// <inheritdoc />
        public ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new AndroidTokenCacheAccessor();
        }

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager { get; } = new AndroidCryptographyManager();
    }
}
