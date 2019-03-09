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
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Mats.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here. 
    /// </summary>
    internal class NetCorePlatformProxy : AbstractPlatformProxy
    {
        public NetCorePlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in 
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
            if (string.IsNullOrWhiteSpace(variable))
            {
                throw new ArgumentNullException(nameof(variable));
            }

            return Environment.GetEnvironmentVariable(variable);
        }

        protected override string InternalGetProcessorArchitecture()
        {
            return null;
        }

        protected override string InternalGetOperatingSystem()
        {
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        }

        protected override string InternalGetDeviceModel()
        {
            return null;
        }

        /// <inheritdoc />
        public override string GetBrokerOrRedirectUri(Uri redirectUri)
        {
            return redirectUri.OriginalString;
        }

        /// <inheritdoc />
        public override string GetDefaultRedirectUri(string clientId)
        {
            return Constants.DefaultRedirectUri;
        }

        protected override string InternalGetProductName()
        {
            return "MSAL.NetCore";
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Name of the calling application</returns>
        protected override string InternalGetCallingApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name?.ToString();
        }

        /// <summary>
        /// Considered PII, ensure that it is hashed. 
        /// </summary>
        /// <returns>Version of the calling application</returns>
        protected override string InternalGetCallingApplicationVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        /// <summary>
        /// Considered PII. Please ensure that it is hashed. 
        /// </summary>
        /// <returns>Device identifier</returns>
        protected override string InternalGetDeviceId()
        {
            return Environment.MachineName;
        }

        public override ILegacyCachePersistence CreateLegacyCachePersistence()
        {
            return new InMemoryLegacyCachePersistance();
        }

        public override ITokenCacheAccessor CreateTokenCacheAccessor()
        {
            return new InMemoryTokenCacheAccessor();
        }

        protected override IWebUIFactory CreateWebUiFactory() => new WebUIFactory();
        protected override ICryptographyManager InternalGetCryptographyManager() => new NetCoreCryptographyManager();
        protected override IPlatformLogger InternalGetPlatformLogger() => new EventSourcePlatformLogger();

        public override string GetDeviceNetworkState()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetDpti()
        {
            // TODO(mats):
            return string.Empty;
        }

        public override string GetMatsOsPlatform()
        {
            // TODO(mats): need to detect operating system and switch on it to determine proper enum
            return MatsConverter.AsString(OsPlatform.Win32);
        }

        public override int GetMatsOsPlatformCode()
        {
            // TODO(mats): need to detect operating system and switch on it to determine proper enum
            return MatsConverter.AsInt(OsPlatform.Win32);
        }
    }
}
