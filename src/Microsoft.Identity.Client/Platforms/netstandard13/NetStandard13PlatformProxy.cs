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
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here. 
    /// </summary>
    internal class Netstandard13PlatformProxy : IPlatformProxy
    {
        private readonly Lazy<IPlatformLogger> _platformLogger =
            new Lazy<IPlatformLogger>(() => new EventSourcePlatformLogger());
        private IWebUIFactory _overloadWebUiFactory;

        /// <summary>
        /// Get the user logged in
        /// </summary>
        public Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult(string.Empty);
        }

        public Task<bool> IsUserLocalAsync(RequestContext requestContext)
        {
            return Task.FromResult(false);
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

        /// <inheritdoc />
        public string GetProductName()
        {
            return "MSAL.CoreCLR";
        }

        public bool IsDomainJoined()
        {
            return false;
        }

        public string GetEnvironmentVariable(string variable)
        {
            if (string.IsNullOrWhiteSpace(variable))
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
            return null;
        }

        public string GetDeviceModel()
        {
            return null;
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Name of the calling application</returns>
        public string GetCallingApplicationName()
        {
            return null;
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Version of the calling application</returns>
        public string GetCallingApplicationVersion()
        {
            return null;
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed. 
        /// </summary>
        /// <returns>Device identifier</returns>
        public string GetDeviceId()
        {
            return null;
        }

        public ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new InMemoryLegacyCachePersistance();
        }

        public ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new TokenCacheAccessor();
        }

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager { get; } =
            new NetStandard13CryptographyManager();

        /// <inheritdoc />
        public IPlatformLogger PlatformLogger => _platformLogger.Value;

        /// <inheritdoc />
        public IWebUIFactory GetWebUiFactory()
        {
            return _overloadWebUiFactory ?? new WebUIFactory();
        }

        /// <inheritdoc />
        public void SetWebUiFactory(IWebUIFactory webUiFactory)
        {
            _overloadWebUiFactory = webUiFactory;
        }
    }
}
