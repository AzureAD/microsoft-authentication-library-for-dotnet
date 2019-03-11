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
using Foundation;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using UIKit;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    /// <summary>
    ///     Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class iOSPlatformProxy : AbstractPlatformProxy
    {
        internal const string IosDefaultRedirectUriTemplate = "msal{0}://auth";

        public iOSPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged
        /// </summary>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);
        }

        public override Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            return Task.FromResult(false);
        }

        public override bool IsDomainJoined()
        {
            return false;
        }        

        public override string GetEnvironmentVariable(string variable)
        {
            return null;
        }

        protected override  string InternalGetProcessorArchitecture()
        {
            return null;
        }

        protected override  string InternalGetOperatingSystem()
        {
            return UIDevice.CurrentDevice.SystemVersion;
        }

        protected override  string InternalGetDeviceModel()
        {
            return UIDevice.CurrentDevice.Model;
        }

       
        /// <inheritdoc />
        public override string GetBrokerOrRedirectUri(Uri redirectUri)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string clientId)
        {
            return string.Format(CultureInfo.InvariantCulture, IosDefaultRedirectUriTemplate, clientId);
        }

        protected override  string InternalGetProductName()
        {
            return "MSAL.Xamarin.iOS";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override  string InternalGetCallingApplicationName()
        {
            return (NSString)NSBundle.MainBundle?.InfoDictionary?["CFBundleName"];
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override  string InternalGetCallingApplicationVersion()
        {
            return (NSString)NSBundle.MainBundle?.InfoDictionary?["CFBundleVersion"];
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed. 
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override  string InternalGetDeviceId()
        {
            return UIDevice.CurrentDevice?.IdentifierForVendor?.AsString();
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new iOSLegacyCachePersistence(Logger);
        }

        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new iOSTokenCacheAccessor();
        }

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new IosWebUIFactory();
        }

        protected override ICryptographyManager InternalGetCryptographyManager() => new iOSCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new ConsolePlatformLogger();

        protected override IFeatureFlags CreateFeatureFlags() => new iOSFeatureFlags();
    }
}
