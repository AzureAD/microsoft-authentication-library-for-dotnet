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
using System.Threading.Tasks;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Mac
{
    /// <summary>
    /// Platform / OS specific logic.
    /// </summary>
    internal class MacPlatformProxy : IPlatformProxy
    {
        internal const string IosDefaultRedirectUriTemplate = "msal{0}://auth";
        private readonly Lazy<IPlatformLogger> _platformLogger = 
            new Lazy<IPlatformLogger>(() => new ConsolePlatformLogger());
        private IWebUIFactory _overloadWebUiFactory;

        /// <summary>
        ///     Get the user logged
        /// </summary>
        public Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);
        }

        public Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            return Task.FromResult(false);
        }

        public bool IsDomainJoined()
        {
            return false;
        }

        public string GetEnvironmentVariable(string variable)
        {
            if (String.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentNullException(nameof(variable));
            }

            return Environment.GetEnvironmentVariable(variable);
        }

        public string GetProcessorArchitecture()
        {
            return null;
        }

        public string GetOperatingSystem()
        {
            return Environment.OSVersion.ToString();
        }

        public string GetDeviceModel()
        {
            return null;
        }


        /// <inheritdoc />
        public string GetBrokerOrRedirectUri(Uri redirectUri)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public string GetDefaultRedirectUri(string clientId)
        {
            return Constants.DefaultRedirectUri;
        }

        public string GetProductName()
        {
            return "MSAL.Xamarin.Mac";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Name of the calling application</returns>
        public string GetCallingApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name;
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Version of the calling application</returns>
        public string GetCallingApplicationVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }


        private static readonly Lazy<string> DeviceIdLazy = new Lazy<string>(
           () => NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                                 .Select(nic => nic.GetPhysicalAddress()?.ToString()).FirstOrDefault());

        /// <summary>
        /// Considered PII. Please ensure that it is hashed. 
        /// </summary>
        /// <returns>Device identifier</returns>
        public string GetDeviceId()
        {
            return DeviceIdLazy.Value;
        }

        public ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            // There is no ADAL for MAC 
            return new NullLegacyCachePersistence();
        }

        /// <remarks>
        /// Currently we do not store a token cache in the key chain for Mac. Instead, 
        /// we allow users provide custom token cache serialization.
        /// </remarks>
        public ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new TokenCacheAccessor(); 
        }

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager { get; } 
            = new MacCryptographyManager();

        /// <inheritdoc />
        public IPlatformLogger PlatformLogger => _platformLogger.Value;

        /// <inheritdoc />
        public IWebUIFactory GetWebUiFactory()
        {
            return _overloadWebUiFactory ?? new MacUIFactory();
        }

        /// <inheritdoc />
        public void SetWebUiFactory(IWebUIFactory webUiFactory)
        {
            _overloadWebUiFactory = webUiFactory;
        }
    }
}
