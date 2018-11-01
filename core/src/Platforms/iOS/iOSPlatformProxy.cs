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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Cache;
using UIKit;

namespace Microsoft.Identity.Core
{
    /// <summary>
    ///     Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class iOSPlatformProxy : IPlatformProxy
    {
        internal const string IosDefaultRedirectUriTemplate = "msal{0}://auth";
        private readonly bool _isMsal;

        public iOSPlatformProxy(bool isMsal)
        {
            _isMsal = isMsal;
        }

        /// <summary>
        ///     Get the user logged
        /// </summary>
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
            return null;
        }

        public string GetOperatingSystem()
        {
            return UIDevice.CurrentDevice.SystemVersion;
        }

        public string GetDeviceModel()
        {
            return UIDevice.CurrentDevice.Model;
        }

        /// <inheritdoc />
        public void ValidateRedirectUri(Uri redirectUri, RequestContext requestContext)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }

            if (_isMsal)
            {
                if (Constants.DefaultRedirectUri.Equals(redirectUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Need to use CoreExceptionFactory here...?
                    //throw new MsalException(MsalError.RedirectUriValidationFailed, "Default redirect URI - " + Constants.DefaultRedirectUri +
                    //                                                               " cannot be used on iOS platform");
                    throw new InvalidOperationException(
                        $"Default redirect URI - {Constants.DefaultRedirectUri} cannot be used on iOS platform");
                }
            }
        }

        /// <inheritdoc />
        public string GetRedirectUriAsString(Uri redirectUri, RequestContext requestContext)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public string GetDefaultRedirectUri(string correlationId)
        {
            return _isMsal ?
                string.Format(
                    CultureInfo.InvariantCulture,
                    IosDefaultRedirectUriTemplate,
                    correlationId)
                : Constants.DefaultRedirectUri;
        }

        public string GetProductName()
        {
            return _isMsal ? "MSAL.Xamarin.iOS" : "PCL.iOS";
        }

        public ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new iOSLegacyCachePersistence();
        }

        public ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new iOSTokenCacheAccessor();
        }

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager { get; } = new iOSCryptographyManager();
    }
}